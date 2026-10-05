using System;
using System.Collections.Generic;
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
		private bool m_isVisible;

		internal Icon3DHandle( Icon3DRequest _request ) => m_request = _request;

		/// <summary>
		/// Whether the caller currently shows this icon on screen. Pending icons of visible handles are rendered
		/// before all others; set it every frame the visibility may change (UiIcon3D does).
		/// </summary>
		public bool IsVisible
		{
			get => m_isVisible;
			set
			{
				if (m_isVisible == value || m_request == null)
					return;

				m_isVisible = value;
				m_request.VisibleHandles += value ? 1 : -1;
			}
		}

		public bool IsReleased => m_request == null;

		/// <summary>Render texture keyword; empty after release.</summary>
		public string Key => m_request?.Key ?? string.Empty;

		/// <summary>The icon texture, or null while there is none. Content is valid once <see cref="IsRendered"/>.</summary>
		public RenderTexture Texture => m_request?.Texture;

		/// <summary>True once the current texture holds the rendered icon.</summary>
		public bool IsRendered => m_request != null && m_request.IsRendered;

		/// <summary>Request a new render, e.g. after the object changed in a way the icon can not detect.</summary>
		public void SetDirty() => m_request?.SetDirty();

		/// <summary>True for icons from <see cref="UiIcon3DRenderer.RenderAnimated"/>.</summary>
		public bool IsAnimated => m_request != null && m_request.IsAnimated;

		/// <summary>
		/// Animated icons only: false freezes the icon on its current frame and frees the object instance;
		/// true creates a new instance and plays again (from the start of its animation).
		/// </summary>
		public bool IsPlaying
		{
			get => m_request != null && m_request.IsAnimated && m_request.IsPlaying;
			set
			{
				if (m_request == null || !m_request.IsAnimated || m_request.IsPlaying == value)
					return;

				m_request.IsPlaying = value;
				if (value)
					m_request.SetDirty();
				else
					UiIcon3DRenderer.Freeze(m_request);
			}
		}

		/// <summary>Animated icons only: render every n-th frame (1 = every frame).</summary>
		public int FrameDivider
		{
			get => m_request?.FrameDivider ?? 1;
			set
			{
				if (m_request != null)
					m_request.FrameDivider = Mathf.Max(1, value);
			}
		}

		internal Icon3DRequest Request => m_request;

		public void Release()
		{
			if (m_request == null)
				return;

			IsVisible = false;
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
		public int VisibleHandles;
		public readonly long Sequence;
		public RenderTexture Texture;
		public bool IsDirty;
		public bool IsRendered;
		public bool HasFailed;
		/// <summary>Render twice (over black and over white) to get the alpha of transparent materials right; decided per instance.</summary>
		public bool ExactAlpha;

		// Animated icons: a persistent instance on the stage, invisible outside its own render
		public readonly bool IsAnimated;
		public bool IsPlaying;
		public int FrameDivider = 1;
		public int FrameCounter;
		public GameObject Instance;
		public Bounds FitBounds;
		public Quaternion FitRotation;
		public double LastAnimationTime;
		public readonly List<Renderer> Renderers = new();
		public readonly List<Light> Lights = new();

		private readonly RenderTextureSpec m_spec;
		private static long s_sequence;

		public Icon3DRequest( string _key, GameObject _prefab, UiIcon3DPreset _preset, Vector2Int _size, Quaternion? _viewRotation, bool _animated = false )
		{
			IsAnimated = _animated;
			IsPlaying = _animated;
			Key = _key;
			Prefab = _prefab;
			Preset = _preset;
			Size = _size;
			ViewRotation = _viewRotation;
			Sequence = ++s_sequence;

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
