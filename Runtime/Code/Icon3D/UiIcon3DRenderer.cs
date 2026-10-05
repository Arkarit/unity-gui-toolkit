using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GuiToolkit
{
	/// <summary>
	/// Renders 3D objects into textures for UI icons, each with its own lighting, isolated from each other
	/// and from the scene. See .Dev-App/Planning/3D-Icons.md for the reasoning.
	///
	/// <b>Isolation by time.</b> There is one hidden stage with one camera on a reserved layer. Icons are rendered
	/// one after another; during a single render exactly one object instance and one preset are active, the
	/// environment is replaced (<see cref="IIcon3DRenderBackend.BeginEnvironment"/>) and scene lights are masked
	/// out of the icon layer. Everything is restored right after. Outside its own render an icon object does not
	/// exist (static icons) or is invisible, so scene cameras never see it, whatever their culling mask.
	///
	/// <b>Static icons</b> are instantiated, rendered once and destroyed; the result stays in a lean render texture
	/// (no depth, no MSAA) from <see cref="RenderTextureManager"/>. Identical requests share one render.
	///
	/// <b>Animated icons</b> keep a persistent instance on the stage, so its Animator, particles and scripts run; it is
	/// rendered every frame (or every n-th) after the static icons and is invisible outside its own render
	/// (<c>forceRenderingOff</c>, own lights off). Framing is computed once, so the image does not pump with the
	/// animation. In edit mode Animators and particles are advanced manually (30 fps); scripts do not run there.
	///
	/// Rendering happens once per frame after the canvas layout and before the cameras render, limited to
	/// <see cref="RendersPerFrame"/>. <see cref="Flush"/> renders everything pending immediately.
	/// </summary>
	[EditorAware]
	public static class UiIcon3DRenderer
	{
		/// <summary>Where the stage lives. Far away from typical level content; float precision is still fine here.</summary>
		public static readonly Vector3 StagePosition = new(0, -5000, 0);

		private const int FallbackLayer = 31;

		/// <summary>Resources path of the library's neutral preset, used when neither icon nor configuration names one.</summary>
		public const string DefaultPresetResourcePath = "Icon3D/Icon3DPreset_Neutral";

		private static readonly Dictionary<string, Icon3DRequest> s_requests = new();
		private static readonly List<Icon3DRequest> s_pending = new();
		private static readonly Dictionary<UiIcon3DPreset, UiIcon3DPreset> s_presetInstances = new();
		private static readonly List<(Light light, int cullingMask)> s_maskedLights = new();
		private static readonly List<Animator> s_animators = new();
		private static readonly List<ParticleSystem> s_particleSystems = new();
		private static readonly List<Icon3DRequest> s_animated = new();
		private static readonly List<Icon3DRequest> s_animatedDue = new();
		private static int s_animatedCounter;

		/// <summary>In edit mode animated icons advance and render at most this often (the editor ticks far more often).</summary>
		private const double EditModeAnimationInterval = 1.0 / 30.0;

		private static GameObject s_stage;
		private static Transform s_parking;
		private static Camera s_camera;
		private static UiIcon3DPreset s_fallbackPreset;
		private static Cubemap s_fallbackReflection;
		private static Material s_alphaCombine;
		private static bool s_alphaCombineWarned;
		private static IIcon3DRenderBackend s_backend;

		private static int s_layerOverride = -1;
		private static int s_resolvedLayer = -1;
		private static int s_msaaOverride;
		private static int s_rendersPerFrameOverride;
		private static float s_renderMillisecondsOverride = -1;
		private static bool s_playerLoopInstalled;
		private static bool s_warnedMissingLayer;
		private static bool s_triedResourcesPreset;
		private static UiIcon3DPreset s_resourcesPreset;
		private static Action s_beforeRender;
		private static Action s_afterRender;
		private static bool s_repaintRequested;

		/// <summary>
		/// Raised on every renderer tick, right before pending icons are rendered: once per frame in play mode
		/// (after the canvas layout), on every editor update in edit mode. Icons use it to follow size changes,
		/// so a request made here is rendered in the same tick.
		/// </summary>
		public static event Action EvBeforeRender
		{
			add
			{
				s_beforeRender += value;
				EnsureUpdateHook();
			}
			remove => s_beforeRender -= value;
		}

		/// <summary>
		/// Raised on every renderer tick right after pending icons were rendered. Icons swap to a finished image
		/// here, so the image is shown in the same tick it was rendered in.
		/// </summary>
		public static event Action EvAfterRender
		{
			add
			{
				s_afterRender += value;
				EnsureUpdateHook();
			}
			remove => s_afterRender -= value;
		}

		/// <summary>
		/// Ask for a repaint of all editor views at the end of this tick, e.g. after an icon switched to an image
		/// that needed no render (shared with another icon). Only has an effect in edit mode, where nothing else
		/// would repaint the views until the user interacts.
		/// </summary>
		public static void RequestEditorRepaint() => s_repaintRequested = true;

		/// <summary>Number of renders since start; for tests and diagnostics.</summary>
		public static int RenderCount { get; private set; }

		/// <summary>Number of distinct icons currently requested.</summary>
		public static int RequestCount => s_requests.Count;

		/// <summary>
		/// Layer reserved for icons. Taken from UiToolkitConfiguration (layer name), unless set explicitly.
		/// No scene object may use this layer.
		/// </summary>
		public static int Layer
		{
			get
			{
				if (s_layerOverride >= 0)
					return s_layerOverride;

				if (s_resolvedLayer < 0)
					s_resolvedLayer = ResolveLayerFromConfig();

				return s_resolvedLayer;
			}
			set
			{
				int newValue = value >= 0 && value < 32 ? value : -1;
				if (newValue == s_layerOverride)
					return;

				s_layerOverride = newValue;
				TearDownStage();   // instances and presets carry the old layer
				SetAllDirty();
			}
		}

		/// <summary>MSAA samples of the scratch render target. From UiToolkitConfiguration unless set explicitly (0 = config).</summary>
		public static int MsaaSamples
		{
			get
			{
				int samples = s_msaaOverride > 0 ? s_msaaOverride : Config?.Icon3DMsaaSamples ?? 4;
				return samples >= 8 ? 8 : samples >= 4 ? 4 : samples >= 2 ? 2 : 1;
			}
			set => s_msaaOverride = Mathf.Max(0, value);
		}

		/// <summary>Static renders per frame. From UiToolkitConfiguration unless set explicitly (0 = config).</summary>
		public static int RendersPerFrame
		{
			get => s_rendersPerFrameOverride > 0 ? s_rendersPerFrameOverride : Mathf.Max(1, Config?.Icon3DRendersPerFrame ?? 8);
			set => s_rendersPerFrameOverride = Mathf.Max(0, value);
		}

		/// <summary>
		/// Time budget for the static renders of one frame, in milliseconds of CPU time (0 = none). Works together
		/// with <see cref="RendersPerFrame"/>: whichever is reached first ends the frame's batch. At least one icon
		/// is always rendered per tick, so a single expensive object can not starve itself. It measures what the CPU
		/// spends on instantiating, culling and submitting - the GPU works asynchronously and is not included.
		/// From UiToolkitConfiguration unless set explicitly (negative = config).
		/// </summary>
		public static float RenderMilliseconds
		{
			get => s_renderMillisecondsOverride >= 0 ? s_renderMillisecondsOverride : Mathf.Max(0, Config?.Icon3DRenderMilliseconds ?? 4f);
			set => s_renderMillisecondsOverride = value < 0 ? -1 : value;
		}

		/// <summary>Static icons that were still pending after the last tick because the time budget ran out.</summary>
		public static int DeferredByTimeBudget { get; private set; }

		/// <summary>CPU milliseconds the static renders of the last tick took.</summary>
		public static float LastStaticRenderMilliseconds { get; private set; }

		/// <summary>Render pipeline specific part. Built-in by default.</summary>
		public static IIcon3DRenderBackend Backend
		{
			get => s_backend ??= new BuiltinIcon3DBackend();
			set
			{
				if (s_backend == value)
					return;

				TearDownStage();
				s_backend = value;
				SetAllDirty();
			}
		}

		/// <summary>
		/// Request a static icon of _prefab. The texture is available via the returned handle (or by its keyword from
		/// <see cref="RenderTextureManager"/>) and holds the icon once <see cref="Icon3DHandle.IsRendered"/>.
		/// Release the handle when the icon is no longer needed.
		/// </summary>
		/// <param name="_prefab">Object to show; a prefab asset or any GameObject to copy.</param>
		/// <param name="_preset">Lighting preset prefab; null = configured default preset, or a neutral built-in one.</param>
		/// <param name="_size">Texture size in pixels.</param>
		/// <param name="_viewRotation">Overrides the preset's view rotation.</param>
		public static Icon3DHandle RenderStatic( GameObject _prefab, UiIcon3DPreset _preset, Vector2Int _size, Quaternion? _viewRotation = null )
		{
			if (_prefab == null)
				throw new ArgumentNullException(nameof(_prefab));

			_size = Vector2Int.Max(_size, Vector2Int.one);
			string key = BuildKey(_prefab, _preset, _size, _viewRotation);

			if (!s_requests.TryGetValue(key, out var request))
			{
				request = new Icon3DRequest(key, _prefab, _preset, _size, _viewRotation);
				s_requests.Add(key, request);
				RenderTextureManager.RegisterProducer(key, request);
				EnsureUpdateHook();
			}

			request.RefCount++;
			return new Icon3DHandle(request);
		}

		/// <summary>
		/// Request an animated icon of _prefab: a persistent instance whose own animation (Animator, particles,
		/// scripts) plays, rendered every <paramref name="_frameDivider"/>-th frame. Animated icons are never shared.
		/// Release the handle when the icon is no longer needed; <see cref="Icon3DHandle.IsPlaying"/> = false freezes it.
		/// </summary>
		public static Icon3DHandle RenderAnimated( GameObject _prefab, UiIcon3DPreset _preset, Vector2Int _size,
			Quaternion? _viewRotation = null, int _frameDivider = 1 )
		{
			if (_prefab == null)
				throw new ArgumentNullException(nameof(_prefab));

			_size = Vector2Int.Max(_size, Vector2Int.one);
			string key = $"icon3d-animated/{++s_animatedCounter}/{_prefab.GetInstanceID()}";
			var request = new Icon3DRequest(key, _prefab, _preset, _size, _viewRotation, true)
			{
				FrameDivider = Mathf.Max(1, _frameDivider)
			};

			s_requests.Add(key, request);
			s_animated.Add(request);
			RenderTextureManager.RegisterProducer(key, request);
			EnsureUpdateHook();

			request.RefCount++;
			// Rendered unless the caller says it is not visible (UiIcon3D does, every frame)
			return new Icon3DHandle(request) { IsVisible = true };
		}

		/// <summary>Render all pending icons now, regardless of the per-frame budget.</summary>
		public static void Flush() => Process(int.MaxValue);

		/// <summary>Stop an animated icon on its current frame: the texture stays, the instance goes.</summary>
		internal static void Freeze( Icon3DRequest _request )
		{
			_request.IsPlaying = false;
			DestroyAnimatedInstance(_request);
		}

		/// <summary>Release all icons and tear down the stage. Handles still held become empty.</summary>
		public static void Shutdown()
		{
			foreach (var request in s_requests.Values)
				RenderTextureManager.UnregisterProducer(request.Key, request);

			s_requests.Clear();
			TearDownStage();
			s_animated.Clear();
		}

		internal static void Release( Icon3DRequest _request )
		{
			if (--_request.RefCount > 0)
				return;

			if (s_requests.TryGetValue(_request.Key, out var registered) && registered == _request)
			{
				s_requests.Remove(_request.Key);
				RenderTextureManager.UnregisterProducer(_request.Key, _request);
			}

			if (_request.IsAnimated)
			{
				s_animated.Remove(_request);
				DestroyAnimatedInstance(_request);
			}
		}

		private static string BuildKey( GameObject _prefab, UiIcon3DPreset _preset, Vector2Int _size, Quaternion? _viewRotation )
		{
			string rotation = _viewRotation.HasValue
				? $"{_viewRotation.Value.x:F4},{_viewRotation.Value.y:F4},{_viewRotation.Value.z:F4},{_viewRotation.Value.w:F4}"
				: "preset";
			int presetId = _preset != null ? _preset.GetInstanceID() : 0;
			return $"icon3d/{_prefab.GetInstanceID()}/{presetId}/{_size.x}x{_size.y}/{rotation}";
		}

		/// <summary>
		/// Forget cached preset instances and render every icon again, e.g. after a preset or object prefab
		/// was edited. Called by the editor whenever prefabs, materials or textures are reimported.
		/// </summary>
		public static void Invalidate()
		{
			s_triedResourcesPreset = false;
			TearDownStage();
			SetAllDirty();
		}

		private static void SetAllDirty()
		{
			foreach (var request in s_requests.Values)
				request.SetDirty();
		}

		#region Rendering

		/// <summary>
		/// One renderer tick: icons update their requests, pending icons render (within _budget renders and
		/// _milliseconds of CPU time, 0 = no time limit), icons swap.
		/// </summary>
		internal static void Process( int _budget, float _milliseconds = 0 )
		{
			Icon3DAssetCache.Update();
			Raise(s_beforeRender);
			int rendered = RenderPending(_budget, _milliseconds);
			Raise(s_afterRender);

#if UNITY_EDITOR
			// In edit mode nothing repaints the views on its own; a changed RawImage would stay invisible until
			// the user clicks somewhere
			if (!Application.isPlaying && (rendered > 0 || s_repaintRequested))
				UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
#endif
			s_repaintRequested = false;
		}

		private static int RenderPending( int _budget, float _milliseconds )
		{
			DeferredByTimeBudget = 0;
			LastStaticRenderMilliseconds = 0;

			if (s_requests.Count == 0)
				return 0;

			// Lets new requests get their textures and resized ones re-render in this pass
			RenderTextureManager.FlushAll();

			s_pending.Clear();
			foreach (var request in s_requests.Values)
			{
				if (request.IsAnimated)
					continue;

				// Render textures lose their content e.g. on a graphics device reset
				if (request.IsRendered && request.Texture != null && !request.Texture.IsCreated())
					request.SetDirty();

				if (request.IsDirty && !request.HasFailed && request.Texture != null)
					s_pending.Add(request);
			}

			// Visible first, then oldest first - a list that just opened fills in from what the user looks at
			s_pending.Sort(s_renderOrder);
			int staticCount = Mathf.Clamp(_budget, 0, s_pending.Count);

			CollectDueAnimated();

			if (staticCount == 0 && s_animatedDue.Count == 0)
			{
				s_pending.Clear();
				return 0;
			}

			EnsureStage();
			MaskForeignLights();
			int renderedStatic = 0;
			try
			{
				var clock = System.Diagnostics.Stopwatch.StartNew();
				for (int i = 0; i < staticCount; i++)
				{
					// Always at least one: whatever the budget, the queue must make progress
					if (i > 0 && _milliseconds > 0 && clock.Elapsed.TotalMilliseconds >= _milliseconds)
						break;

					RenderStaticRequest(s_pending[i]);
					renderedStatic++;
				}

				LastStaticRenderMilliseconds = (float)clock.Elapsed.TotalMilliseconds;
				DeferredByTimeBudget = staticCount - renderedStatic;

				// After the static ones: they must not wait for animation, which renders every frame anyway
				foreach (var request in s_animatedDue)
					RenderAnimatedRequest(request);
			}
			finally
			{
				RestoreForeignLights();
				s_pending.Clear();
			}

			int count = renderedStatic + s_animatedDue.Count;
			s_animatedDue.Clear();
			return count;
		}

		/// <summary>Animated icons that render in this tick: playing, visible, and their frame has come.</summary>
		private static void CollectDueAnimated()
		{
			s_animatedDue.Clear();
			if (s_animated.Count == 0)
				return;

			double now = Time.realtimeSinceStartupAsDouble;
			foreach (var request in s_animated)
			{
				if (!request.IsPlaying || request.HasFailed || request.Texture == null)
					continue;

				// Not visible: keep animating, but do not spend a render on it - unless it has no image at all yet
				if (request.VisibleHandles == 0 && request.IsRendered && !request.IsDirty)
					continue;

				if (!Application.isPlaying && request.IsRendered && !request.IsDirty
				    && now - request.LastAnimationTime < EditModeAnimationInterval)
					continue;

				if (Application.isPlaying && request.IsRendered && !request.IsDirty
				    && ++request.FrameCounter % request.FrameDivider != 0)
					continue;

				s_animatedDue.Add(request);
			}
		}

		private static void RenderStaticRequest( Icon3DRequest _request )
		{
			_request.IsDirty = false;
			GameObject instance = null;

			try
			{
				var preset = GetPresetInstance(_request.Preset);
				instance = CreateInstance(_request.Prefab);
				instance.transform.SetParent(s_stage.transform, false);
				if (!instance.activeSelf)
					instance.SetActive(true);

				// Bring animated characters into their entry pose instead of the stored bind pose
				UpdateAnimators(instance, 0);
				_request.ExactAlpha = NeedsExactAlpha(instance, preset);

				GetFraming(instance, _request, preset, out var bounds, out var rotation);
				RenderInstance(_request, preset, bounds, rotation);
			}
			catch (Exception e)
			{
				OnRenderFailed(_request, e);
			}
			finally
			{
				if (instance != null)
				{
					// Destroy() is deferred to the end of the frame - the instance must vanish now
					instance.SetActive(false);
					DestroyObject(instance);
				}
			}
		}

		private static void RenderAnimatedRequest( Icon3DRequest _request )
		{
			try
			{
				var preset = GetPresetInstance(_request.Preset);
				if (_request.IsDirty || _request.Instance == null)
				{
					DestroyAnimatedInstance(_request);
					CreateAnimatedInstance(_request, preset);
					_request.IsDirty = false;
				}

				double now = Time.realtimeSinceStartupAsDouble;
				if (!Application.isPlaying)
				{
					// Nothing animates in edit mode on its own
					float deltaTime = Mathf.Clamp((float)(now - _request.LastAnimationTime), 0, 0.1f);
					UpdateAnimators(_request.Instance, deltaTime);
					SimulateParticles(_request.Instance, deltaTime);
				}

				_request.LastAnimationTime = now;

				SetAnimatedVisible(_request, true);
				try
				{
					RenderInstance(_request, preset, _request.FitBounds, _request.FitRotation);
				}
				finally
				{
					SetAnimatedVisible(_request, false);
				}
			}
			catch (Exception e)
			{
				OnRenderFailed(_request, e);
				DestroyAnimatedInstance(_request);
			}
		}

		/// <summary>The render itself, shared by static and animated icons: frame, light, render, resolve.</summary>
		private static void RenderInstance( Icon3DRequest _request, UiIcon3DPreset _preset, Bounds _bounds, Quaternion _rotation )
		{
			var target = _request.Texture;
			RenderTexture scratch = null;
			RenderTexture scratchOverWhite = null;
			var previousActive = RenderTexture.active;

			try
			{
				// Lights are camera relative: the preset turns with the view
				_preset.transform.SetParent(s_stage.transform, false);
				_preset.transform.SetPositionAndRotation(_bounds.center, _rotation);

				Icon3DFitter.Fit(s_camera, _bounds, _rotation, _preset.Projection, _preset.FieldOfView, _preset.FitMode,
					_preset.Padding, (float)target.width / target.height);
				s_camera.cullingMask = 1 << Layer;

				// A second render over white only makes sense for a background that is transparent
				var combine = _request.ExactAlpha && _preset.BackgroundColor.a <= 0f ? GetAlphaCombineMaterial() : null;
				s_camera.backgroundColor = combine != null ? new Color(0, 0, 0, 0) : _preset.BackgroundColor;

				scratch = RenderTexture.GetTemporary(target.width, target.height, 24, RenderTextureFormat.ARGB32,
					RenderTextureReadWrite.Default, MsaaSamples);
				RenderStage(_preset, scratch);

				if (combine != null)
				{
					s_camera.backgroundColor = new Color(1, 1, 1, 0);
					scratchOverWhite = RenderTexture.GetTemporary(target.width, target.height, 24, RenderTextureFormat.ARGB32,
						RenderTextureReadWrite.Default, MsaaSamples);
					RenderStage(_preset, scratchOverWhite);

					// Resolves MSAA, drops the depth buffer and derives the alpha from the difference of both
					combine.SetTexture("_WhiteTex", scratchOverWhite);
					Graphics.Blit(scratch, target, combine);
				}
				else
				{
					// Resolves MSAA and drops the depth buffer
					Graphics.Blit(scratch, target);
				}

				_request.IsRendered = true;
				RenderCount++;
			}
			finally
			{
				RenderTexture.active = previousActive;

				if (s_camera != null)
					s_camera.targetTexture = null;

				if (scratch != null)
					RenderTexture.ReleaseTemporary(scratch);

				if (scratchOverWhite != null)
					RenderTexture.ReleaseTemporary(scratchOverWhite);

				if (_preset != null && s_parking != null)
					_preset.transform.SetParent(s_parking, false);
			}
		}

		/// <summary>One camera render of the stage into _target, with the preset's environment and nothing else.</summary>
		private static void RenderStage( UiIcon3DPreset _preset, RenderTexture _target )
		{
			s_camera.targetTexture = _target;
			Backend.BeginEnvironment(_preset);
			try
			{
				Backend.Render(s_camera);
			}
			finally
			{
				Backend.EndEnvironment();
				s_camera.targetTexture = null;
			}
		}

		/// <summary>
		/// Whether the alpha of this object needs the two render path: the preset says so, or (Auto) the object has a
		/// material in the transparent render queue - glass, particles, anything blended.
		/// </summary>
		private static bool NeedsExactAlpha( GameObject _instance, UiIcon3DPreset _preset )
		{
			switch (_preset.AlphaMode)
			{
				case UiIcon3DPreset.EAlphaMode.Fast:
					return false;
				case UiIcon3DPreset.EAlphaMode.Exact:
					return true;
			}

			foreach (var rend in _instance.GetComponentsInChildren<Renderer>(true))
			{
				foreach (var material in rend.sharedMaterials)
				{
					if (material != null && material.renderQueue >= (int)UnityEngine.Rendering.RenderQueue.Transparent)
						return true;
				}
			}

			return false;
		}

		private static Material GetAlphaCombineMaterial()
		{
			if (s_alphaCombine != null)
				return s_alphaCombine;

			var shader = Resources.Load<Shader>("Icon3D/Icon3DAlphaCombine");
			if (shader == null || !shader.isSupported)
			{
				if (!s_alphaCombineWarned)
				{
					s_alphaCombineWarned = true;
					UiLog.LogWarning("3D icons: the alpha combine shader (Resources/Icon3D/Icon3DAlphaCombine) is missing or unsupported - transparent materials keep their wrong alpha.");
				}

				return null;
			}

			s_alphaCombine = new Material(shader) { name = "Icon3D Alpha Combine", hideFlags = HideFlags.HideAndDontSave };
			return s_alphaCombine;
		}

		/// <summary>Instantiate below the inactive parking slot (no Awake yet) and make it safe for the stage.</summary>
		private static GameObject CreateInstance( GameObject _prefab )
		{
			var instance = Object.Instantiate(_prefab, s_parking, false);
			instance.name = _prefab.name;
			PrepareForStage(instance, Layer);
			return instance;
		}

		/// <summary>Priority of the view: the icon's override, the object's own hint, the preset.</summary>
		private static void GetFraming( GameObject _instance, Icon3DRequest _request, UiIcon3DPreset _preset, out Bounds _bounds, out Quaternion _rotation )
		{
			var hint = _instance.GetComponentInChildren<UiIcon3DBoundsHint>();
			bool useHint = hint != null && hint.enabled;

			if (useHint)
				_bounds = hint.WorldBounds;
			else if (!Icon3DFitter.TryGetBounds(_instance, out _bounds))
				_bounds = new Bounds(_instance.transform.position, Vector3.one);

			_rotation = _request.ViewRotation
				?? (useHint && hint.OverrideViewRotation ? hint.WorldViewRotation : _preset.ViewRotation);
		}

		private static void CreateAnimatedInstance( Icon3DRequest _request, UiIcon3DPreset _preset )
		{
			var instance = CreateInstance(_request.Prefab);

			// Nothing may stop animating because no real camera looks at it, and nothing may walk out of the frame
			instance.GetComponentsInChildren(true, s_animators);
			foreach (var animator in s_animators)
			{
				animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
				animator.applyRootMotion = false;
			}
			s_animators.Clear();

			foreach (var skinned in instance.GetComponentsInChildren<SkinnedMeshRenderer>(true))
				skinned.updateWhenOffscreen = true;

			instance.GetComponentsInChildren(true, s_particleSystems);
			foreach (var particles in s_particleSystems)
			{
				var main = particles.main;
				main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
			}
			s_particleSystems.Clear();

			// Invisible and dark until its own render
			instance.GetComponentsInChildren(true, _request.Renderers);
			foreach (var light in instance.GetComponentsInChildren<Light>(true))
				if (light.enabled)
					_request.Lights.Add(light);

			_request.ExactAlpha = NeedsExactAlpha(instance, _preset);
			_request.Instance = instance;
			SetAnimatedVisible(_request, false);

			instance.transform.SetParent(s_stage.transform, false);
			if (!instance.activeSelf)
				instance.SetActive(true);

			UpdateAnimators(instance, 0);

			// Framing once, from the entry pose - per frame it would pump with the animation
			GetFraming(instance, _request, _preset, out _request.FitBounds, out _request.FitRotation);
			_request.LastAnimationTime = Time.realtimeSinceStartupAsDouble;
			_request.FrameCounter = 0;
		}

		private static void DestroyAnimatedInstance( Icon3DRequest _request )
		{
			if (_request.Instance != null)
			{
				_request.Instance.SetActive(false);
				DestroyObject(_request.Instance);
			}

			_request.Instance = null;
			_request.Renderers.Clear();
			_request.Lights.Clear();
		}

		private static void SetAnimatedVisible( Icon3DRequest _request, bool _visible )
		{
			foreach (var rend in _request.Renderers)
				if (rend != null)
					rend.forceRenderingOff = !_visible;

			foreach (var light in _request.Lights)
				if (light != null)
					light.enabled = _visible;
		}

		private static void UpdateAnimators( GameObject _root, float _deltaTime )
		{
			_root.GetComponentsInChildren(false, s_animators);
			foreach (var animator in s_animators)
				if (animator.isActiveAndEnabled && animator.runtimeAnimatorController != null)
					animator.Update(_deltaTime);
			s_animators.Clear();
		}

		private static void SimulateParticles( GameObject _root, float _deltaTime )
		{
			if (_deltaTime <= 0)
				return;

			_root.GetComponentsInChildren(false, s_particleSystems);
			foreach (var particles in s_particleSystems)
			{
				// Only roots: Simulate() handles the children
				if (particles.transform.parent != null && particles.transform.parent.GetComponentInParent<ParticleSystem>() != null)
					continue;

				particles.Simulate(_deltaTime, true, false, false);
			}
			s_particleSystems.Clear();
		}

		private static void OnRenderFailed( Icon3DRequest _request, Exception _e )
		{
			_request.HasFailed = true;
			UiLog.LogError($"3D icon '{_request.Key}' failed to render, it will not be retried until SetDirty(): {_e.Message}", _request.Prefab);
			Debug.LogException(_e, _request.Prefab);
		}

		private static readonly Comparison<Icon3DRequest> s_renderOrder = ( _a, _b ) =>
		{
			bool aVisible = _a.VisibleHandles > 0;
			bool bVisible = _b.VisibleHandles > 0;
			if (aVisible != bVisible)
				return aVisible ? -1 : 1;

			return _a.Sequence.CompareTo(_b.Sequence);
		};

		private static void Raise( Action _event )
		{
			if (_event == null)
				return;

			try
			{
				_event.Invoke();
			}
			catch (Exception e)
			{
				Debug.LogException(e);
			}
		}

		/// <summary>
		/// Make an instance (object or preset) safe for the stage: icon layer, hidden, not saved, no light/reflection
		/// probes from the scene, own lights restricted to the icon layer, no audio, no cameras.
		/// </summary>
		private static void PrepareForStage( GameObject _root, int _layer )
		{
			foreach (var t in _root.GetComponentsInChildren<Transform>(true))
			{
				t.gameObject.layer = _layer;
				t.gameObject.hideFlags = HideFlags.HideAndDontSave;
			}

			foreach (var rend in _root.GetComponentsInChildren<Renderer>(true))
			{
				// Otherwise the scene's light probes / reflection probes at the stage position light the object
				rend.lightProbeUsage = UnityEngine.Rendering.LightProbeUsage.Off;
				rend.reflectionProbeUsage = UnityEngine.Rendering.ReflectionProbeUsage.Off;
			}

			foreach (var light in _root.GetComponentsInChildren<Light>(true))
				light.cullingMask = 1 << _layer;

			foreach (var audioSource in _root.GetComponentsInChildren<AudioSource>(true))
				audioSource.enabled = false;

			foreach (var listener in _root.GetComponentsInChildren<AudioListener>(true))
				listener.enabled = false;

			foreach (var cam in _root.GetComponentsInChildren<Camera>(true))
				cam.enabled = false;

			// An animated instance stays active: it must not collide with anything, nor fall
#if UITK_PHYSICS
			foreach (var col in _root.GetComponentsInChildren<Collider>(true))
				col.enabled = false;

			foreach (var body in _root.GetComponentsInChildren<Rigidbody>(true))
			{
				body.isKinematic = true;
				body.detectCollisions = false;
			}
#endif
#if UITK_PHYSICS2D
			foreach (var col in _root.GetComponentsInChildren<Collider2D>(true))
				col.enabled = false;

			foreach (var body in _root.GetComponentsInChildren<Rigidbody2D>(true))
				body.simulated = false;
#endif
		}

		private static UiIcon3DPreset GetPresetInstance( UiIcon3DPreset _prefab )
		{
			if (_prefab == null)
				_prefab = Config?.Icon3DDefaultPreset;

			if (_prefab == null)
				_prefab = GetResourcesPreset();

			if (_prefab == null)
				return GetFallbackPreset();

			if (s_presetInstances.TryGetValue(_prefab, out var instance) && instance != null)
				return instance;

			instance = Object.Instantiate(_prefab, s_parking, false);
			instance.name = _prefab.name;
			PreparePreset(instance);

			// Active in itself, inactive in hierarchy while parked; becomes active when moved onto the stage
			if (!instance.gameObject.activeSelf)
				instance.gameObject.SetActive(true);
			s_presetInstances[_prefab] = instance;
			return instance;
		}

		private static UiIcon3DPreset GetResourcesPreset()
		{
			if (!s_triedResourcesPreset)
			{
				s_triedResourcesPreset = true;
				var go = Resources.Load<GameObject>(DefaultPresetResourcePath);
				s_resourcesPreset = go != null ? go.GetComponent<UiIcon3DPreset>() : null;
			}

			return s_resourcesPreset;
		}

		/// <summary>Last resort when even the library preset is missing: a neutral preset built in code.</summary>
		private static UiIcon3DPreset GetFallbackPreset()
		{
			if (s_fallbackPreset != null)
				return s_fallbackPreset;

			var go = new GameObject("Icon3D Fallback Preset");
			go.transform.SetParent(s_parking, false);
			s_fallbackPreset = go.AddComponent<UiIcon3DPreset>();

			var settings = go.AddComponent<UiCameraRenderSettings>();
			settings.AmbientMode.Value = UnityEngine.Rendering.AmbientMode.Flat;
			settings.AmbientMode.Enabled = true;
			settings.AmbientLight.Value = new Color(0.4f, 0.4f, 0.4f);
			settings.AmbientLight.Enabled = true;

			// Without a reflection of its own, metal renders black (the baseline reflection is black)
			s_fallbackReflection = Icon3DEnvironmentUtility.CreateGradientCubemap(
				new Color(0.85f, 0.85f, 0.85f), new Color(0.45f, 0.45f, 0.45f), new Color(0.12f, 0.12f, 0.12f));
			s_fallbackReflection.hideFlags = HideFlags.HideAndDontSave;
			settings.DefaultReflectionMode.Value = UnityEngine.Rendering.DefaultReflectionMode.Custom;
			settings.DefaultReflectionMode.Enabled = true;
			settings.CustomReflection.Value = s_fallbackReflection;
			settings.CustomReflection.Enabled = true;

			var lightGo = new GameObject("Key Light");
			lightGo.transform.SetParent(go.transform, false);
			lightGo.transform.localRotation = Quaternion.Euler(35, 30, 0);   // from top left, relative to the camera
			var light = lightGo.AddComponent<Light>();
			light.type = LightType.Directional;
			light.intensity = 1.1f;
			light.shadows = LightShadows.None;

			PreparePreset(s_fallbackPreset);
			return s_fallbackPreset;
		}

		private static void PreparePreset( UiIcon3DPreset _preset )
		{
			PrepareForStage(_preset.gameObject, Layer);

			// Presets are few and must look exactly as authored: no per-vertex fallback for their lights
			foreach (var light in _preset.GetComponentsInChildren<Light>(true))
				light.renderMode = LightRenderMode.ForcePixel;
		}

		private static void MaskForeignLights()
		{
			int bit = 1 << Layer;
			var lights = Object.FindObjectsByType<Light>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
			foreach (var light in lights)
			{
				if ((light.cullingMask & bit) == 0 || light.transform.IsChildOf(s_stage.transform))
					continue;

				s_maskedLights.Add((light, light.cullingMask));
				light.cullingMask &= ~bit;
			}
		}

		private static void RestoreForeignLights()
		{
			foreach (var (light, cullingMask) in s_maskedLights)
				if (light != null)
					light.cullingMask = cullingMask;

			s_maskedLights.Clear();
		}

		#endregion

		#region Stage

		private static void EnsureStage()
		{
			if (s_stage != null)
				return;

			int layer = Layer;
			s_stage = new GameObject("UiIcon3D Stage") { hideFlags = HideFlags.HideAndDontSave, layer = layer };
			s_stage.transform.position = StagePosition;
			if (Application.isPlaying)
				Object.DontDestroyOnLoad(s_stage);

			var parkingGo = new GameObject("Parking") { hideFlags = HideFlags.HideAndDontSave, layer = layer };
			parkingGo.SetActive(false);
			s_parking = parkingGo.transform;
			s_parking.SetParent(s_stage.transform, false);

			var cameraGo = new GameObject("Camera") { hideFlags = HideFlags.HideAndDontSave, layer = layer };
			cameraGo.transform.SetParent(s_stage.transform, false);
			s_camera = cameraGo.AddComponent<Camera>();
			s_camera.cullingMask = 1 << layer;
			Backend.SetupCamera(s_camera);
		}

		private static void TearDownStage()
		{
			s_backend?.Dispose();

			// Animated instances live on the stage; they are recreated on their next render
			foreach (var request in s_animated)
			{
				DestroyAnimatedInstance(request);
				if (request.IsPlaying)
					request.IsDirty = true;
			}

			foreach (var preset in s_presetInstances.Values)
				if (preset != null)
					DestroyObject(preset.gameObject);

			s_presetInstances.Clear();
			s_fallbackPreset = null;

			if (s_fallbackReflection != null)
				DestroyObject(s_fallbackReflection);
			s_fallbackReflection = null;

			if (s_alphaCombine != null)
				DestroyObject(s_alphaCombine);
			s_alphaCombine = null;

			if (s_stage != null)
				DestroyObject(s_stage);

			s_stage = null;
			s_parking = null;
			s_camera = null;
		}

		private static void DestroyObject( Object _obj )
		{
			if (Application.isPlaying)
				Object.Destroy(_obj);
			else
				Object.DestroyImmediate(_obj);
		}

		#endregion

		#region Configuration and update hooks

		private static UiToolkitConfiguration Config
		{
			get
			{
				try
				{
					return UiToolkitConfiguration.Instance;
				}
				catch
				{
					// Not initialized yet (early startup, some test setups): use defaults
					return null;
				}
			}
		}

		private static int ResolveLayerFromConfig()
		{
			string layerName = Config?.Icon3DLayerName;
			if (string.IsNullOrEmpty(layerName))
				layerName = "Icon3D";

			int layer = LayerMask.NameToLayer(layerName);
			if (layer >= 0)
				return layer;

			if (!s_warnedMissingLayer)
			{
				s_warnedMissingLayer = true;
				UiLog.LogWarning($"3D icons: layer '{layerName}' does not exist, using layer {FallbackLayer}. " +
					"Create the layer (Gui Toolkit configuration window, 3D Icons) and make sure no scene object uses it.");
			}

			return FallbackLayer;
		}

		// Marker type for the player loop system
		private struct UiIcon3DRendererUpdate { }

		private static void EnsureUpdateHook()
		{
			if (s_playerLoopInstalled || !Application.isPlaying)
				return;

			s_playerLoopInstalled = PlayerLoopUtility.InsertBeforeFrameRendering(typeof(UiIcon3DRendererUpdate), OnFrame);
		}

		private static void OnFrame() => Process(RendersPerFrame, RenderMilliseconds);

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetStatics()
		{
			// Domain reload may be disabled: nothing from a previous session must survive
			TearDownStage();
			s_requests.Clear();
			s_animated.Clear();
			s_animatedCounter = 0;
			s_playerLoopInstalled = false;
			s_resolvedLayer = -1;
			s_warnedMissingLayer = false;
			s_triedResourcesPreset = false;
			s_resourcesPreset = null;
			RenderCount = 0;
		}

#if UNITY_EDITOR
		[UnityEditor.InitializeOnLoadMethod]
		private static void InitEditor()
		{
			// Edit mode preview; editor update ticks regardless of focus (see CLAUDE.md)
			UnityEditor.EditorApplication.update += () =>
			{
				if (!Application.isPlaying)
					Process(RendersPerFrame, RenderMilliseconds);
			};

			UnityEditor.AssemblyReloadEvents.beforeAssemblyReload += TearDownStage;
			UnityEditor.EditorApplication.playModeStateChanged += _state =>
			{
				if (_state == UnityEditor.PlayModeStateChange.ExitingEditMode || _state == UnityEditor.PlayModeStateChange.ExitingPlayMode)
					TearDownStage();
			};
		}
#endif

		#endregion
	}
}
