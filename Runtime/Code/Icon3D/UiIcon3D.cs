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
		}

		[Tooltip("Object to show; a prefab or any GameObject. It is copied for every render, never modified.")]
		[SerializeField] private GameObject m_prefab;
		[Tooltip("Lighting preset prefab; empty = the configured default preset")]
		[SerializeField] private UiIcon3DPreset m_preset;
		[SerializeField] private bool m_overrideViewRotation;
		[Tooltip("Camera rotation (euler angles); (0,180,0) looks at an object's front")]
		[SerializeField] private Vector3 m_viewRotation = new(15, 150, 0);
		[SerializeField] private EMode m_mode = EMode.Static;
		[SerializeField][Min(0.05f)] private float m_refreshInterval = 1;
		[Tooltip("Multiplier for the texture size, e.g. 2 for supersampling or 0.5 to save memory")]
		[SerializeField][Min(0.05f)] private float m_resolutionScale = 1;
		[Tooltip("Pixels per canvas unit for world space canvases, where the canvas scale factor is meaningless")]
		[SerializeField][Min(0.01f)] private float m_worldSpacePixelsPerUnit = 1;

		private RawImage m_rawImage;
		private Icon3DHandle m_shown;
		private Icon3DHandle m_pending;
		private Vector2Int m_requestedSize;
		private bool m_isDirty = true;
		private bool m_isSubscribed;
		private float m_nextRefreshTime;

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

		public EMode Mode
		{
			get => m_mode;
			set => m_mode = value;
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
			ShowTexture(RenderTextureManager.Placeholder);

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
			ShowTexture(RenderTextureManager.Placeholder);
			ReleaseHandles();
		}

		// Before the renderer renders: follow size and property changes, so the request renders in this tick
		private void OnBeforeRender()
		{
			if (this == null)
				return;

			var size = GetPixelSize();
			if (m_isDirty || size != m_requestedSize)
				Request(size);

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
			m_pending = null;
			ShowTexture(m_shown.Texture);
			previous?.Release();
			m_nextRefreshTime = Time.realtimeSinceStartup + m_refreshInterval;

			// The image may have needed no render (shared with another icon), so nothing else repaints the editor
			UiIcon3DRenderer.RequestEditorRepaint();
		}

		private void Request( Vector2Int _size )
		{
			m_isDirty = false;
			m_requestedSize = _size;

			m_pending?.Release();
			m_pending = null;

			if (m_prefab == null || _size.x <= 0 || _size.y <= 0)
			{
				ShowTexture(RenderTextureManager.Placeholder);
				ReleaseHandles();
				return;
			}

			var handle = UiIcon3DRenderer.RenderStatic(m_prefab, m_preset, _size, m_overrideViewRotation ? ViewRotation : (Quaternion?)null);

			// Nothing that matters changed
			if (m_shown != null && handle.Key == m_shown.Key)
			{
				handle.Release();
				return;
			}

			m_pending = handle;
			if (m_shown == null)
				ShowTexture(RenderTextureManager.Placeholder);
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
			m_requestedSize = Vector2Int.zero;
		}
	}
}
