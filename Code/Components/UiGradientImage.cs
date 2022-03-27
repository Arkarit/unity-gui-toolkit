using System;
using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit
{
	[ExecuteAlways]
	public class UiGradientImage : MaskableGraphic, ICanvasRaycastFilter
	{
		protected enum Type
		{
			Horizontal,
			Vertical,
		}

		[SerializeField]
		protected Gradient m_gradient = new Gradient();

		[SerializeField]
		protected Type m_type;

		[SerializeField]
		protected int m_textureSize = 256;

		private Texture2D m_texture;

		protected override void Awake()
		{
			base.Awake();
			CreateTexture();
		}

		public override Texture mainTexture
		{
			get
			{
				if (m_texture == null)
					CreateTexture();
				return m_texture;
			}
		}

		private void CreateTexture()
		{
			if (m_texture)
				m_texture.Destroy();

			bool horz = m_type == Type.Horizontal;

			m_texture = new Texture2D(horz ? m_textureSize : 1, horz ? 1 : m_textureSize, TextureFormat.RGBA32, false);
			Color[] colors = new Color[m_textureSize];

			for (int i=0; i<m_textureSize; i++)
			{
				float norm = (float) i / (float) (m_textureSize-1);
				Color color = m_gradient.Evaluate(norm);
				colors[i] = color;
			}

			m_texture.SetPixels(colors);
			m_texture.Apply(false, true);
		}

		public virtual bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera) => true;

		protected override void OnValidate()
		{
			base.OnValidate();
			CreateTexture();
		}
	}
}