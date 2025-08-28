using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

// Rebuilds runtime Texture/Sprite from serialized PNG bytes.
// Works in play mode, edit mode, and Prefab Stage (ExecuteAlways).
namespace GuiToolkit
{
	[ExecuteAlways]
	public class UiSpriteHolder : MonoBehaviour, ISerializationCallbackReceiver
	{
		// Persisted data (goes into scene/prefab asset)
		[SerializeField, HideInInspector] private byte[] m_pngBytes;
		[SerializeField, HideInInspector] private int m_width;
		[SerializeField, HideInInspector] private int m_height;
		[SerializeField, HideInInspector] private float m_pixelsPerUnit = 100f;
		[SerializeField, HideInInspector] private Vector2 m_pivot = new Vector2(0.5f, 0.5f);

		// Runtime only (never serialized)
		[NonSerialized] private Texture2D m_texture;
		[NonSerialized] private Sprite m_sprite;

		// Public accessors
		public Texture2D Texture => m_texture;
		public Sprite Sprite => m_sprite;
		public bool HasData => m_pngBytes != null && m_pngBytes.Length > 0;

		// Optional: mark to (re)encode on next serialize if Texture changed via API
		private bool m_dirty;

		// Create fresh empty texture & sprite (not persisted yet)
		public void Create( string name = null, int width = -1, int height = -1 )
		{
			DestroyRuntimeObjects();

			if (width <= 0 || height <= 0)
			{
#if UNITY_EDITOR
				var gv = Handles.GetMainGameViewSize();
				width = Mathf.Max(64, Mathf.RoundToInt(gv.x));
				height = Mathf.Max(64, Mathf.RoundToInt(gv.y));
#else
            width = 512; height = 512;
#endif
			}

			m_width = width;
			m_height = height;
			m_pixelsPerUnit = 100f;
			m_pivot = new Vector2(0.5f, 0.5f);

			m_texture = new Texture2D(m_width, m_height, TextureFormat.RGBA32, false, false);
			m_texture.name = name ?? $"__sprite_{GetInstanceID()}__";
			m_texture.wrapMode = TextureWrapMode.Clamp;
			m_texture.filterMode = FilterMode.Bilinear;

			m_sprite = Sprite.Create(m_texture, new Rect(0, 0, m_width, m_height), m_pivot, m_pixelsPerUnit, 0, SpriteMeshType.FullRect);
			m_sprite.name = (name ?? "Sprite") + "_sprite";

			// No PNG data yet; caller should call CaptureIntoTexture(...) or SetFromTexture(...)
			m_dirty = true;
		}

		// Copy pixels from active RenderTexture into our Texture and mark for persistence
		public void ReadFromActiveRT()
		{
			if (m_texture == null) Create();
			var rt = RenderTexture.active;
			if (rt == null || rt.width != m_width || rt.height != m_height)
			{
				// Resize to match RT
				Create(m_texture ? m_texture.name : null, rt ? rt.width : m_width, rt ? rt.height : m_height);
			}

			var prev = RenderTexture.active;
			try
			{
				// Assume caller already set RenderTexture.active
				m_texture.ReadPixels(new Rect(0, 0, m_width, m_height), 0, 0, false);
				m_texture.Apply(false, false);
			}
			finally
			{
				RenderTexture.active = prev;
			}
			m_dirty = true;
		}

		// Set from an existing Texture2D (copies pixels)
		public void SetFromTexture( Texture2D source, bool encodeNow = true )
		{
			if (source == null) return;
			Create(source.name, source.width, source.height);
			Graphics.CopyTexture(source, m_texture);
			m_dirty = true;
			if (encodeNow) EncodeToPngBytes();
		}

		// Destroys runtime-only objects (safe across domain reloads)
		public void DestroyRuntimeObjects()
		{
			if (m_sprite) DestroyImmediate(m_sprite);
			m_sprite = null;
			if (m_texture) DestroyImmediate(m_texture);
			m_texture = null;
		}

		// ---- Persistence ----

		// Encode current Texture into PNG bytes
		private void EncodeToPngBytes()
		{
			if (m_texture == null) return;
#if UNITY_EDITOR
			// Make sure texture is readable (it is, we created it)
#endif
			m_pngBytes = m_texture.EncodeToPNG();
			m_width = m_texture.width;
			m_height = m_texture.height;
			m_dirty = false;
		}

		// Rebuild Texture/Sprite from PNG bytes (used after reload / in Prefab Stage)
		private void RebuildFromBytesIfNeeded()
		{
			if (!HasData) return;

			if (m_texture == null || m_texture.width != m_width || m_texture.height != m_height)
			{
				// Create new runtime texture & sprite
				DestroyRuntimeObjects();
				m_texture = new Texture2D(m_width, m_height, TextureFormat.RGBA32, false, false);
				m_texture.wrapMode = TextureWrapMode.Clamp;
				m_texture.filterMode = FilterMode.Bilinear;
				m_sprite = Sprite.Create(m_texture, new Rect(0, 0, m_width, m_height), m_pivot, m_pixelsPerUnit, 0, SpriteMeshType.FullRect);
			}

			// Decode PNG into texture
			if (m_pngBytes != null && m_pngBytes.Length > 0)
			{
				// ImageConversion.LoadImage expands PNG; ensure it targets our instance
				m_texture.LoadImage(m_pngBytes, markNonReadable: false);
				// Ensure size matches metadata (LoadImage can resize; update fields just in case)
				m_width = m_texture.width;
				m_height = m_texture.height;
			}
		}

		// ISerializationCallbackReceiver
		public void OnBeforeSerialize()
		{
			// When saving prefab/scene, store PNG bytes so the visual can be reconstructed later.
			if ((m_pngBytes == null || m_pngBytes.Length == 0 || m_dirty) && m_texture != null)
				EncodeToPngBytes();
		}

		public void OnAfterDeserialize()
		{
			// Cannot create UnityEngine.Objects here; do it in OnEnable / OnValidate.
			// We intentionally no-op here.
		}

		private void OnEnable()
		{
			// Rebuild runtime objects when component becomes active (also in Prefab Stage)
			RebuildFromBytesIfNeeded();

			// Auto-attach to sibling Image if present
			var img = GetComponent<UnityEngine.UI.Image>();
			if (img && m_sprite) img.sprite = m_sprite;
		}

		private void OnDisable()
		{
			// Keep runtime objects to keep preview visible while disabled in editor
			// (Destroying here would blank the overlay if hierarchy is toggled)
		}

		private void OnDestroy()
		{
			DestroyRuntimeObjects();
		}

#if UNITY_EDITOR
		private void OnValidate()
		{
			// Editor-time changes: ensure sprite reflects texture size
			if (m_sprite && m_texture && (m_sprite.rect.width != m_texture.width || m_sprite.rect.height != m_texture.height))
			{
				DestroyRuntimeObjects();
				RebuildFromBytesIfNeeded();
				var img = GetComponent<UnityEngine.UI.Image>();
				if (img && m_sprite) img.sprite = m_sprite;
			}
		}
#endif
	}
}