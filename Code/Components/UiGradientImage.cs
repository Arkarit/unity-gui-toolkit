using System.Collections;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GuiToolkit
{
	/// \brief Generated image of a unity gradient
	/// 
	/// This component is useful to generate an Unity gradient as a Unity image, which can be used in other components,
	/// similar to a Image component. This can be a colorful gradient; however, that makes not much sense in terms of performance;
	/// better use UiGradient or UiGradientSimple instead in that case.
	/// The more interesting usage is that of a mask; instead of creating gradient masks, you can simply create a "procedural" mask here.
	/// It can be either horizontal or vertical.
	[ExecuteAlways]
	public class UiGradientImage : Graphic //, ICanvasRaycastFilter
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
		private Type m_oldType;
		private int m_oldTextureSize;

		public override Texture mainTexture
		{
			get
			{
				if (m_texture == null)
					CreateTexture();
				return m_texture;
			}
		}

		public override Material material
		{
			get
			{
				if (m_Material != null)
					return m_Material;

				return defaultGraphicMaterial;
			}

			set
			{
				base.material = value;
			}
		}

		protected override void OnEnable()
		{
			base.OnEnable();
			CreateTexture();
		}

		protected override void OnDisable()
		{
			base.OnDisable();
			m_texture.Destroy();
			m_texture = null;
		}

		private void CreateTexture()
		{
			bool resetTexture = m_texture != null && (m_oldTextureSize != m_textureSize || m_oldType != m_type || !m_texture.isReadable);

			if (resetTexture)
			{
				m_texture.Destroy();
				m_texture = null;
			}

			bool horz = m_type == Type.Horizontal;

			if (m_textureSize <= 0)
				m_textureSize = 1;

			if (m_texture == null)
				m_texture = new Texture2D(horz ? m_textureSize : 1, horz ? 1 : m_textureSize, TextureFormat.RGBA32, false);

			Color[] colors = new Color[m_textureSize];

			for (int i = 0; i < m_textureSize; i++)
			{
				float norm = (float)i / (float)(m_textureSize - 1);
				Color color = m_gradient.Evaluate(norm) * base.color;
				colors[i] = color;
			}

			m_texture.SetPixels(colors);
			m_texture.Apply(false, Application.isPlaying);

			m_oldType = m_type;
			m_oldTextureSize = m_textureSize;

			SetMaterialDirty();
		}

		protected override void OnValidate()
		{
			base.OnValidate();

			if (gameObject.activeInHierarchy)
				CreateTexture();
		}
	}
}