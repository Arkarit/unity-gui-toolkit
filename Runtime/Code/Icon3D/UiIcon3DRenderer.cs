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
	/// Rendering happens once per frame after the canvas layout and before the cameras render, limited to
	/// <see cref="RendersPerFrame"/>. <see cref="Flush"/> renders everything pending immediately.
	/// </summary>
	[EditorAware]
	public static class UiIcon3DRenderer
	{
		/// <summary>Where the stage lives. Far away from typical level content; float precision is still fine here.</summary>
		public static readonly Vector3 StagePosition = new(0, -5000, 0);

		private const int FallbackLayer = 31;

		private static readonly Dictionary<string, Icon3DRequest> s_requests = new();
		private static readonly List<Icon3DRequest> s_pending = new();
		private static readonly Dictionary<UiIcon3DPreset, UiIcon3DPreset> s_presetInstances = new();
		private static readonly List<(Light light, int cullingMask)> s_maskedLights = new();
		private static readonly List<Animator> s_animators = new();

		private static GameObject s_stage;
		private static Transform s_parking;
		private static Camera s_camera;
		private static UiIcon3DPreset s_fallbackPreset;
		private static Cubemap s_fallbackReflection;
		private static IIcon3DRenderBackend s_backend;

		private static int s_layerOverride = -1;
		private static int s_resolvedLayer = -1;
		private static int s_msaaOverride;
		private static int s_rendersPerFrameOverride;
		private static bool s_playerLoopInstalled;
		private static bool s_warnedMissingLayer;

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

		/// <summary>Render all pending icons now, regardless of the per-frame budget.</summary>
		public static void Flush() => Process(int.MaxValue);

		/// <summary>Release all icons and tear down the stage. Handles still held become empty.</summary>
		public static void Shutdown()
		{
			foreach (var request in s_requests.Values)
				RenderTextureManager.UnregisterProducer(request.Key, request);

			s_requests.Clear();
			TearDownStage();
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
		}

		private static string BuildKey( GameObject _prefab, UiIcon3DPreset _preset, Vector2Int _size, Quaternion? _viewRotation )
		{
			string rotation = _viewRotation.HasValue
				? $"{_viewRotation.Value.x:F4},{_viewRotation.Value.y:F4},{_viewRotation.Value.z:F4},{_viewRotation.Value.w:F4}"
				: "preset";
			int presetId = _preset != null ? _preset.GetInstanceID() : 0;
			return $"icon3d/{_prefab.GetInstanceID()}/{presetId}/{_size.x}x{_size.y}/{rotation}";
		}

		private static void SetAllDirty()
		{
			foreach (var request in s_requests.Values)
				request.SetDirty();
		}

		#region Rendering

		private static void Process( int _budget )
		{
			if (s_requests.Count == 0)
				return;

			// Lets new requests get their textures and resized ones re-render in this pass
			RenderTextureManager.FlushAll();

			s_pending.Clear();
			foreach (var request in s_requests.Values)
			{
				// Render textures lose their content e.g. on a graphics device reset
				if (request.IsRendered && request.Texture != null && !request.Texture.IsCreated())
					request.SetDirty();

				if (request.IsDirty && !request.HasFailed && request.Texture != null)
					s_pending.Add(request);
			}

			if (s_pending.Count == 0)
				return;

			EnsureStage();
			MaskForeignLights();
			try
			{
				int count = Mathf.Min(_budget, s_pending.Count);
				for (int i = 0; i < count; i++)
					RenderRequest(s_pending[i]);
			}
			finally
			{
				RestoreForeignLights();
				s_pending.Clear();
			}

#if UNITY_EDITOR
			if (!Application.isPlaying)
				UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
#endif
		}

		private static void RenderRequest( Icon3DRequest _request )
		{
			_request.IsDirty = false;

			var target = _request.Texture;
			UiIcon3DPreset preset = null;
			GameObject instance = null;
			RenderTexture scratch = null;
			var previousActive = RenderTexture.active;

			try
			{
				preset = GetPresetInstance(_request.Preset);
				int layer = Layer;

				// Instantiate below the inactive parking slot: no Awake before it is prepared
				instance = Object.Instantiate(_request.Prefab, s_parking, false);
				instance.name = _request.Prefab.name;
				PrepareForStage(instance, layer);
				instance.transform.SetParent(s_stage.transform, false);
				if (!instance.activeSelf)
					instance.SetActive(true);

				// Bring animated characters into their entry pose instead of the stored bind pose
				instance.GetComponentsInChildren(false, s_animators);
				foreach (var animator in s_animators)
					if (animator.isActiveAndEnabled && animator.runtimeAnimatorController != null)
						animator.Update(0);
				s_animators.Clear();

				if (!Icon3DFitter.TryGetBounds(instance, out var bounds))
					bounds = new Bounds(instance.transform.position, Vector3.one);

				var rotation = _request.ViewRotation ?? preset.ViewRotation;

				// Lights are camera relative: the preset turns with the view
				preset.transform.SetParent(s_stage.transform, false);
				preset.transform.SetPositionAndRotation(bounds.center, rotation);

				Icon3DFitter.Fit(s_camera, bounds, rotation, preset.Projection, preset.FieldOfView, preset.FitMode,
					preset.Padding, (float)target.width / target.height);
				s_camera.backgroundColor = preset.BackgroundColor;
				s_camera.cullingMask = 1 << layer;

				scratch = RenderTexture.GetTemporary(target.width, target.height, 24, RenderTextureFormat.ARGB32,
					RenderTextureReadWrite.Default, MsaaSamples);
				s_camera.targetTexture = scratch;

				Backend.BeginEnvironment(preset);
				try
				{
					Backend.Render(s_camera);
				}
				finally
				{
					Backend.EndEnvironment();
				}

				s_camera.targetTexture = null;

				// Resolves MSAA and drops the depth buffer
				Graphics.Blit(scratch, target);
				_request.IsRendered = true;
				RenderCount++;
			}
			catch (Exception e)
			{
				_request.HasFailed = true;
				UiLog.LogError($"3D icon '{_request.Key}' failed to render, it will not be retried until SetDirty(): {e.Message}", _request.Prefab);
				Debug.LogException(e, _request.Prefab);
			}
			finally
			{
				RenderTexture.active = previousActive;

				if (s_camera != null)
					s_camera.targetTexture = null;

				if (scratch != null)
					RenderTexture.ReleaseTemporary(scratch);

				if (preset != null && s_parking != null)
					preset.transform.SetParent(s_parking, false);

				if (instance != null)
				{
					// Destroy() is deferred to the end of the frame - the instance must vanish now
					instance.SetActive(false);
					DestroyObject(instance);
				}
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
		}

		private static UiIcon3DPreset GetPresetInstance( UiIcon3DPreset _prefab )
		{
			if (_prefab == null)
				_prefab = Config?.Icon3DDefaultPreset;

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

			foreach (var preset in s_presetInstances.Values)
				if (preset != null)
					DestroyObject(preset.gameObject);

			s_presetInstances.Clear();
			s_fallbackPreset = null;

			if (s_fallbackReflection != null)
				DestroyObject(s_fallbackReflection);
			s_fallbackReflection = null;

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

		private static void OnFrame() => Process(RendersPerFrame);

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void ResetStatics()
		{
			// Domain reload may be disabled: nothing from a previous session must survive
			TearDownStage();
			s_requests.Clear();
			s_playerLoopInstalled = false;
			s_resolvedLayer = -1;
			s_warnedMissingLayer = false;
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
					Process(RendersPerFrame);
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
