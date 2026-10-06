using GuiToolkit.AssetHandling;
using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit
{
	/// <summary>
	/// Shows a 3D object as a UI icon, rendered by <see cref="UiIcon3DRenderer"/> with its own lighting preset,
	/// isolated from the scene and from other icons. Works in edit mode, in the Prefab Stage and in play mode.
	///
	/// The icon only says what to show; the stage (camera, lights, environment) belongs to the renderer, and any
	/// animation belongs to the object. The texture matches the RawImage's size in screen pixels; identical icons
	/// (same object, preset, view and size) share one render and one texture, so a list of 100 items with 20
	/// distinct objects renders 20 times.
	///
	/// While a new image is being rendered (first show, resize, property change) the previous one stays visible,
	/// so there is no flicker; before the very first image a transparent placeholder is shown. The switch to the new
	/// image happens in the same renderer tick that rendered it.
	///
	/// The object is either referenced directly or loaded through <see cref="AssetManager"/> by canonical id
	/// (Resources, Addressables, ...); loads are shared between icons. When the object changes - e.g. a pooled list
	/// item is reused for another entry - the old image is dropped at once, it would show the wrong thing.
	///
	/// Visible icons are rendered first: off screen, or culled by a RectMask2D (scroll views), they wait.
	///
	/// The RawImage needs the UI_Icon3D material (premultiplied alpha) - the library prefab has it.
	/// </summary>
	[ExecuteAlways]
	[RequireComponent(typeof(RawImage))]
	public class UiIcon3D : UiThing, IPoolable
	{
		public enum EMode
		{
			/// <summary>Rendered once; re-rendered only when something changes.</summary>
			Static,
			/// <summary>Re-rendered every <see cref="RefreshInterval"/> seconds, e.g. for time dependent presets.</summary>
			Periodic,
			/// <summary>
			/// The object's own animation plays (Animator, particles, scripts) and is rendered every
			/// <see cref="FrameDivider"/>-th frame. Switching to <see cref="Static"/> freezes the current frame.
			/// </summary>
			Animated,
			/// <summary>
			/// A running animation is played backwards, faster by <see cref="RewindSpeed"/>, to the first frame of its loop, and
			/// the icon stands still there - the way back for <see cref="Animated"/> when the pointer leaves. Switching to
			/// <see cref="Animated"/> again meanwhile carries on forward from where it is. Without a running animation this is
			/// <see cref="Static"/>.
			/// </summary>
			Rewinding,
		}

		[Tooltip("Object to show; a prefab or any GameObject. It is copied for every render, never modified.")]
		[SerializeField] private GameObject m_prefab;
		[Tooltip("Object to load through the AssetManager (Resources, Addressables, ...) - used when no prefab is set directly")]
		[SerializeField][CanonicalAssetRef(new[] { typeof(GameObject) })] private CanonicalAssetRef m_prefabRef = new();
		[Tooltip("Shown while the object loads and before its first image; empty = transparent. Should be premultiplied or opaque (UI_Icon3D material).")]
		[SerializeField] private Texture m_loadingTexture;
		[Tooltip("Lighting preset prefab; empty = the configured default preset")]
		[SerializeField] private UiIcon3DPreset m_preset;
		[SerializeField] private bool m_overrideViewRotation;
		[Tooltip("Camera rotation (euler angles); (0,180,0) looks at an object's front")]
		[SerializeField] private Vector3 m_viewRotation = new(15, 150, 0);
		[SerializeField] private EMode m_mode = EMode.Static;
		[Tooltip("Rewinding mode: how much faster than normal the animation runs back to its first frame")]
		[SerializeField][Min(0.1f)] private float m_rewindSpeed = 2f;
		[SerializeField][Min(0.05f)] private float m_refreshInterval = 1;
		[Tooltip("Animated mode: render every n-th frame (1 = every frame)")]
		[SerializeField][Min(1)] private int m_frameDivider = 1;
		[Tooltip("Multiplier for the texture size, e.g. 2 for supersampling or 0.5 to save memory")]
		[SerializeField][Min(0.05f)] private float m_resolutionScale = 1;
		[Tooltip("Pixels per canvas unit for world space canvases, where the canvas scale factor is meaningless")]
		[SerializeField][Min(0.01f)] private float m_worldSpacePixelsPerUnit = 1;

		private RawImage m_rawImage;
		private Icon3DHandle m_shown;
		private Icon3DHandle m_pending;
		private Vector2Int m_requestedSize;
		private GameObject m_requestedPrefab;
		private Icon3DAssetLease m_lease;
		private bool m_isDirty = true;
		private bool m_isSubscribed;
		private float m_nextRefreshTime;
		private string m_shownSignature;
		private string m_pendingSignature;

		public GameObject Prefab
		{
			get => m_prefab;
			set
			{
				if (m_prefab == value)
					return;

				m_prefab = value;
				SetDirty();
			}
		}

		/// <summary>
		/// Canonical id of the object to load (e.g. "res:Items/Sword" or an Addressables key with the provider's prefix).
		/// Only used while <see cref="Prefab"/> is not set.
		/// </summary>
		public string PrefabId
		{
			get => m_prefabRef?.Id ?? string.Empty;
			set
			{
				m_prefabRef ??= new CanonicalAssetRef();
				if (m_prefabRef.Id == value)
					return;

				m_prefabRef.Id = value;
				SetDirty();
			}
		}

		public Texture LoadingTexture
		{
			get => m_loadingTexture;
			set => m_loadingTexture = value;
		}

		/// <summary>True while the object is being loaded.</summary>
		public bool IsLoading => m_prefab == null && m_lease != null && !m_lease.IsLoaded && !m_lease.HasFailed;

		/// <summary>True if loading the object failed; the icon stays empty until the id changes.</summary>
		public bool HasLoadFailed => m_prefab == null && m_lease != null && m_lease.HasFailed;

		public UiIcon3DPreset Preset
		{
			get => m_preset;
			set
			{
				if (m_preset == value)
					return;

				m_preset = value;
				SetDirty();
			}
		}

		public bool OverrideViewRotation
		{
			get => m_overrideViewRotation;
			set
			{
				if (m_overrideViewRotation == value)
					return;

				m_overrideViewRotation = value;
				SetDirty();
			}
		}

		public Quaternion ViewRotation
		{
			get => Quaternion.Euler(m_viewRotation);
			set
			{
				m_viewRotation = value.eulerAngles;
				if (m_overrideViewRotation)
					SetDirty();
			}
		}

		/// <summary>
		/// Static, periodic or animated. Animated to static freezes the current frame (no new render, which would show
		/// the entry pose); static to animated starts the animation, the static image stays until the first frame.
		/// </summary>
		public EMode Mode
		{
			get => m_mode;
			set
			{
				if (m_mode == value)
					return;

				m_mode = value;
				SetDirty();
			}
		}

		/// <summary>Rewinding mode: speed of the way back, as multiple of normal.</summary>
		public float RewindSpeed
		{
			get => m_rewindSpeed;
			set
			{
				m_rewindSpeed = Mathf.Max(0.1f, value);
				if (m_mode == EMode.Rewinding && m_shown != null)
					m_shown.Rewind(m_rewindSpeed);
			}
		}

		/// <summary>Animated mode: render every n-th frame (1 = every frame).</summary>
		public int FrameDivider
		{
			get => m_frameDivider;
			set
			{
				m_frameDivider = Mathf.Max(1, value);
				if (m_shown != null)
					m_shown.FrameDivider = m_frameDivider;
				if (m_pending != null)
					m_pending.FrameDivider = m_frameDivider;
			}
		}

		public float RefreshInterval
		{
			get => m_refreshInterval;
			set => m_refreshInterval = Mathf.Max(0.05f, value);
		}

		public float ResolutionScale
		{
			get => m_resolutionScale;
			set
			{
				value = Mathf.Max(0.05f, value);
				if (Mathf.Approximately(m_resolutionScale, value))
					return;

				m_resolutionScale = value;
				SetDirty();
			}
		}

		/// <summary>True while the RawImage shows a finished image of the current settings.</summary>
		public bool IsRendered => m_pending == null && m_shown != null && m_shown.IsRendered;

		/// <summary>Render texture keyword of the image currently shown; empty if none.</summary>
		public string Key => m_shown?.Key ?? string.Empty;

		public RawImage RawImage
		{
			get
			{
				if (m_rawImage == null)
					m_rawImage = GetComponent<RawImage>();
				return m_rawImage;
			}
		}

		/// <summary>Request the image again on the next renderer tick (e.g. after changing the object at runtime).</summary>
		public void SetDirty() => m_isDirty = true;

		/// <summary>Render again now-ish although nothing changed, e.g. after an animation state change on the object.</summary>
		public void Refresh()
		{
			m_shown?.SetDirty();
			m_pending?.SetDirty();
		}

		protected override void OnEnable()
		{
			base.OnEnable();
			m_isDirty = true;
			ShowTexture(LoadingTextureOrPlaceholder);

			if (!m_isSubscribed)
			{
				UiIcon3DRenderer.EvBeforeRender += OnBeforeRender;
				UiIcon3DRenderer.EvAfterRender += OnAfterRender;
				m_isSubscribed = true;
			}
		}

		protected override void OnDisable()
		{
			if (m_isSubscribed)
			{
				UiIcon3DRenderer.EvBeforeRender -= OnBeforeRender;
				UiIcon3DRenderer.EvAfterRender -= OnAfterRender;
				m_isSubscribed = false;
			}

			// Detach before releasing: the released texture is destroyed immediately in edit mode
			ShowTexture(null);
			ReleaseHandles();
			ReleaseLease();
			base.OnDisable();
		}

#if UNITY_EDITOR
		protected virtual void OnValidate() => m_isDirty = true;

		protected virtual void Reset()
		{
			// The premultiplied material; without it edges get dark fringes
			if (RawImage.material != RawImage.defaultMaterial)
				return;

			foreach (var guid in UnityEditor.AssetDatabase.FindAssets("UI_Icon3D t:Material"))
			{
				var material = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid));
				if (material != null)
				{
					RawImage.material = material;
					break;
				}
			}
		}
#endif

		public void OnPoolCreated() => m_isDirty = true;

		public void OnPoolReleased()
		{
			ShowTexture(LoadingTextureOrPlaceholder);
			ReleaseHandles();
			ReleaseLease();
		}

		// Before the renderer renders: follow size and property changes, so the request renders in this tick
		private void OnBeforeRender()
		{
			if (this == null)
				return;

			var prefab = ResolvePrefab();
			var size = GetPixelSize();
			if (m_isDirty || size != m_requestedSize || !ReferenceEquals(prefab, m_requestedPrefab))
				Request(size, prefab);

			// Visible icons are rendered first
			bool visible = IsOnScreen();
			if (m_pending != null)
				m_pending.IsVisible = visible;
			if (m_shown != null)
				m_shown.IsVisible = visible;

			if (m_mode == EMode.Periodic && m_shown != null && Time.realtimeSinceStartup >= m_nextRefreshTime)
			{
				m_nextRefreshTime = Time.realtimeSinceStartup + m_refreshInterval;
				m_shown.SetDirty();
			}
		}

		// After the renderer rendered: show a finished image in the same tick
		private void OnAfterRender()
		{
			if (this == null || m_pending == null || !m_pending.IsRendered)
				return;

			var previous = m_shown;
			m_shown = m_pending;
			m_shownSignature = m_pendingSignature;
			m_pending = null;
			ShowTexture(m_shown.Texture);
			previous?.Release();
			m_nextRefreshTime = Time.realtimeSinceStartup + m_refreshInterval;

			// The image may have needed no render (shared with another icon), so nothing else repaints the editor
			UiIcon3DRenderer.RequestEditorRepaint();
		}

		/// <summary>The object to render: the direct reference, else the loaded asset; null while loading or without object.</summary>
		private GameObject ResolvePrefab()
		{
			if (m_prefab != null)
			{
				ReleaseLease();
				return m_prefab;
			}

			string id = PrefabId;
			if (string.IsNullOrEmpty(id))
			{
				ReleaseLease();
				return null;
			}

			if (m_lease == null || m_lease.Id != id)
			{
				ReleaseLease();
				m_lease = Icon3DAssetCache.Acquire(id);
			}

			return m_lease.Asset;
		}

		private void Request( Vector2Int _size, GameObject _prefab )
		{
			m_isDirty = false;
			bool objectChanged = !ReferenceEquals(_prefab, m_requestedPrefab);
			m_requestedSize = _size;
			m_requestedPrefab = _prefab;

			// Same content, only the mode changed (or something irrelevant): an animated image freezes / plays on,
			// instead of being replaced by a new render
			string signature = BuildSignature(_prefab, _size);
			if (m_shown != null && m_shown.IsAnimated && signature == m_shownSignature)
			{
				m_pending?.Release();
				m_pending = null;
				m_shown.FrameDivider = m_frameDivider;
				if (m_mode == EMode.Rewinding)
					m_shown.Rewind(m_rewindSpeed);
				else
					m_shown.IsPlaying = m_mode == EMode.Animated;
				return;
			}

			m_pending?.Release();
			m_pending = null;

			// Another object: the old image would show the wrong thing (think of a reused list item)
			if (objectChanged && m_shown != null)
			{
				ShowTexture(LoadingTextureOrPlaceholder);
				m_shown.Release();
				m_shown = null;
			}

			if (_prefab == null || _size.x <= 0 || _size.y <= 0)
			{
				ShowTexture(LoadingTextureOrPlaceholder);
				ReleaseHandles();
				return;
			}

			var rotation = m_overrideViewRotation ? ViewRotation : (Quaternion?)null;
			var handle = m_mode == EMode.Animated
				? UiIcon3DRenderer.RenderAnimated(_prefab, m_preset, _size, rotation, m_frameDivider)
				: UiIcon3DRenderer.RenderStatic(_prefab, m_preset, _size, rotation);

			// Nothing that matters changed (static icons share by key; animated ones are never equal)
			if (m_shown != null && handle.Key == m_shown.Key)
			{
				handle.Release();
				return;
			}

			m_pending = handle;
			m_pendingSignature = signature;
			if (m_shown == null)
				ShowTexture(LoadingTextureOrPlaceholder);
		}

		/// <summary>What the image shows - everything but the mode and the frame divider.</summary>
		private string BuildSignature( GameObject _prefab, Vector2Int _size )
		{
			if (_prefab == null)
				return string.Empty;

			string rotation = m_overrideViewRotation ? m_viewRotation.ToString("F3") : "preset";
			int presetId = m_preset != null ? m_preset.GetInstanceID() : 0;
			return $"{_prefab.GetInstanceID()}/{presetId}/{_size.x}x{_size.y}/{rotation}";
		}

		private Texture LoadingTextureOrPlaceholder => m_loadingTexture != null ? m_loadingTexture : RenderTextureManager.Placeholder;

		private static readonly Vector3[] s_corners = new Vector3[4];

		/// <summary>
		/// On screen and not culled by a RectMask2D (which is what a scroll view uses). Stencil masks are not
		/// considered; such icons count as visible.
		/// </summary>
		private bool IsOnScreen()
		{
			var rawImage = RawImage;
			if (rawImage == null || !rawImage.isActiveAndEnabled || rawImage.canvasRenderer.cull)
				return false;

			var canvas = rawImage.canvas;
			if (canvas == null)
				return false;

			var rootCanvas = canvas.rootCanvas;
			var cam = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera;
			RectTransform.GetWorldCorners(s_corners);

			float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
			foreach (var corner in s_corners)
			{
				var p = RectTransformUtility.WorldToScreenPoint(cam, corner);
				minX = Mathf.Min(minX, p.x);
				minY = Mathf.Min(minY, p.y);
				maxX = Mathf.Max(maxX, p.x);
				maxY = Mathf.Max(maxY, p.y);
			}

			return maxX >= 0 && maxY >= 0 && minX <= Screen.width && minY <= Screen.height;
		}

		private void ReleaseLease()
		{
			m_lease?.Release();
			m_lease = null;
		}

		private Vector2Int GetPixelSize()
		{
			var rawImage = RawImage;
			var canvas = rawImage != null ? rawImage.canvas : null;
			if (canvas == null)
				return Vector2Int.zero;

			var rootCanvas = canvas.rootCanvas;
			float scale = m_resolutionScale * (rootCanvas.renderMode == RenderMode.WorldSpace ? m_worldSpacePixelsPerUnit : rootCanvas.scaleFactor);
			var size = RectTransform.rect.size * scale;
			return new Vector2Int(Mathf.CeilToInt(size.x - 0.001f), Mathf.CeilToInt(size.y - 0.001f));
		}

		private void ShowTexture( Texture _texture )
		{
			var rawImage = RawImage;
			if (rawImage == null)
				return;

			// Reference comparison: Unity's == calls a destroyed texture equal to null,
			// which would keep the dead reference in the RawImage
			if (ReferenceEquals(rawImage.texture, _texture))
				rawImage.SetMaterialDirty();
			else
				rawImage.texture = _texture;
		}

		private void ReleaseHandles()
		{
			m_pending?.Release();
			m_pending = null;
			m_shown?.Release();
			m_shown = null;
			m_shownSignature = null;
			m_pendingSignature = null;
		}
	}
}
