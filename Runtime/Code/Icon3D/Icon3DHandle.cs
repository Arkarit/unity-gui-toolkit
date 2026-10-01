using System;
using UnityEngine;

namespace GuiToolkit
{
	/// <summary>
	/// A caller's share of a rendered 3D icon. Identical requests (same object, preset, size and view)
	/// share one render and one texture; the texture lives as long as at least one handle is not released.
	///
	/// The texture is managed by <see cref="RenderTextureManager"/> under <see cref="Key"/>, so any
	/// render texture consumer (e.g. <see cref="UiRawImageRenderTextureConsumer"/>) can show it by keyword.
	/// </summary>
	public sealed class Icon3DHandle : IDisposable
	{
		private Icon3DRequest m_request;

		internal Icon3DHandle( Icon3DRequest _request ) => m_request = _request;

		public bool IsReleased => m_request == null;

		/// <summary>Render texture keyword; empty after release.</summary>
		public string Key => m_request?.Key ?? string.Empty;

		/// <summary>The icon texture, or null while there is none. Content is valid once <see cref="IsRendered"/>.</summary>
		public RenderTexture Texture => m_request?.Texture;

		/// <summary>True once the current texture holds the rendered icon.</summary>
		public bool IsRendered => m_request != null && m_request.IsRendered;

		/// <summary>Request a new render, e.g. after the object changed in a way the icon can not detect.</summary>
		public void SetDirty() => m_request?.SetDirty();

		public void Release()
		{
			if (m_request == null)
				return;

			UiIcon3DRenderer.Release(m_request);
			m_request = null;
		}

		public void Dispose() => Release();
	}

	/// <summary>
	/// One distinct icon: what to render and where the result lives. Shared by all handles with the same key.
	/// Producer of its render texture keyword.
	/// </summary>
	internal sealed class Icon3DRequest : IRenderTextureProducer
	{
		public readonly string Key;
		public readonly GameObject Prefab;
		public readonly UiIcon3DPreset Preset;
		public readonly Vector2Int Size;
		public readonly Quaternion? ViewRotation;
		public int RefCount;
		public RenderTexture Texture;
		public bool IsDirty;
		public bool IsRendered;
		public bool HasFailed;

		private readonly RenderTextureSpec m_spec;

		public Icon3DRequest( string _key, GameObject _prefab, UiIcon3DPreset _preset, Vector2Int _size, Quaternion? _viewRotation )
		{
			Key = _key;
			Prefab = _prefab;
			Preset = _preset;
			Size = _size;
			ViewRotation = _viewRotation;

			// Storage only: the actual render goes into a shared scratch target with depth and MSAA
			m_spec = RenderTextureSpec.Default;
			m_spec.FixedSize = _size;
			m_spec.DepthBits = 0;
			m_spec.MsaaSamples = 1;
		}

		public RenderTextureSpec RenderTextureSpec => m_spec;

		public void OnRenderTextureChanged( RenderTexture _texture )
		{
			// A new or resized texture has no content
			Texture = _texture;
			IsRendered = false;
			IsDirty = _texture != null;
		}

		public void SetDirty()
		{
			IsDirty = Texture != null;
			HasFailed = false;
		}
	}
}
