using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit
{
	/// <summary>
	/// Shows the managed render texture for a keyword in a RawImage.
	/// Requests a texture matching the RawImage's size in screen pixels.
	/// </summary>
	[RequireComponent(typeof(RawImage))]
	public class UiRawImageRenderTextureConsumer : UiRenderTextureConsumer
	{
		[Tooltip("Multiplier for the requested size, e.g. 2 for supersampling or 0.5 to save memory.")]
		[SerializeField][Min(0.01f)] private float m_resolutionScale = 1;
		[Tooltip("Pixels per canvas unit for world space canvases, where the canvas scale factor is meaningless.")]
		[SerializeField][Min(0.01f)] private float m_worldSpacePixelsPerUnit = 1;

		private RawImage m_rawImage;

		public RawImage RawImage
		{
			get
			{
				if (m_rawImage == null)
					m_rawImage = GetComponent<RawImage>();
				return m_rawImage;
			}
		}

		public float ResolutionScale
		{
			get => m_resolutionScale;
			set => m_resolutionScale = Mathf.Max(0.01f, value);
		}

		public override Vector2Int RequestedSize
		{
			get
			{
				var rawImage = RawImage;
				if (rawImage == null)
					return Vector2Int.zero;

				var size = ((RectTransform)transform).rect.size;
				var canvas = rawImage.canvas;
				float scale = m_resolutionScale;
				if (canvas != null)
				{
					var rootCanvas = canvas.rootCanvas;
					scale *= rootCanvas.renderMode == RenderMode.WorldSpace ? m_worldSpacePixelsPerUnit : rootCanvas.scaleFactor;
				}

				return new Vector2Int(Mathf.CeilToInt(size.x * scale), Mathf.CeilToInt(size.y * scale));
			}
		}

		protected override void ApplyTexture( Texture _texture )
		{
			var rawImage = RawImage;
			if (rawImage == null)
				return;

			// Same texture object after a resize still needs a material refresh
			if (rawImage.texture == _texture)
				rawImage.SetMaterialDirty();
			else
				rawImage.texture = _texture;
		}
	}
}
