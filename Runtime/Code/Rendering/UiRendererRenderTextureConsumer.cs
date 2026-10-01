using UnityEngine;

namespace GuiToolkit
{
	/// <summary>
	/// Shows the managed render texture for a keyword on a Renderer, via a MaterialPropertyBlock,
	/// so the material asset is neither modified nor instantiated.
	/// A renderer has no pixel size, so the requested size is set explicitly.
	/// </summary>
	[RequireComponent(typeof(Renderer))]
	public class UiRendererRenderTextureConsumer : UiRenderTextureConsumer
	{
		[SerializeField] private string m_textureProperty = "_MainTex";
		[SerializeField] private Vector2Int m_requestedSize = new(256, 256);
		[Tooltip("Material index on the renderer; -1 applies to all materials.")]
		[SerializeField] private int m_materialIndex = -1;

		private Renderer m_renderer;
		private MaterialPropertyBlock m_propertyBlock;

		public Renderer Renderer
		{
			get
			{
				if (m_renderer == null)
					m_renderer = GetComponent<Renderer>();
				return m_renderer;
			}
		}

		public override Vector2Int RequestedSize => m_requestedSize;

		public Vector2Int Size
		{
			get => m_requestedSize;
			set => m_requestedSize = value;
		}

		protected override void ApplyTexture( Texture _texture )
		{
			var rend = Renderer;
			if (rend == null || string.IsNullOrEmpty(m_textureProperty))
				return;

			m_propertyBlock ??= new MaterialPropertyBlock();
			int propertyId = Shader.PropertyToID(m_textureProperty);

			if (m_materialIndex < 0)
			{
				rend.GetPropertyBlock(m_propertyBlock);
				m_propertyBlock.SetTexture(propertyId, _texture);
				rend.SetPropertyBlock(m_propertyBlock);
				return;
			}

			rend.GetPropertyBlock(m_propertyBlock, m_materialIndex);
			m_propertyBlock.SetTexture(propertyId, _texture);
			rend.SetPropertyBlock(m_propertyBlock, m_materialIndex);
		}

		protected override void OnDisable()
		{
			base.OnDisable();

			var rend = Renderer;
			if (rend == null)
				return;

			// Leave no texture reference behind. A property block has no way to remove a single entry,
			// so this clears the whole block - don't combine with other property block users on this renderer.
			if (m_materialIndex < 0)
				rend.SetPropertyBlock(null);
			else
				rend.SetPropertyBlock(null, m_materialIndex);
		}
	}
}
