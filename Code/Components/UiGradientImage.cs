using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit
{
	/// \brief Generated image of a unity gradient
	/// 
	/// This component is useful to generate an Unity gradient as a Unity image, which can be used in other components,
	/// similar to a Image component. This can be a colorful gradient; however, that makes not much sense in terms of performance;
	/// better use UiGradient or UiGradientSimple instead in that case.
	/// The more interesting usage is that of a mask; instead of creating gradient masks, you can simply create a "procedural" mask here.
	/// It can be either horizontal or vertical.
//	[ExecuteAlways]
	public class UiGradientImage : MaskableGraphic //, ICanvasRaycastFilter
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

//		protected override void Start()
//		{
//			base.Start();
//			CreateTexture();
//		}

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


		private void CreateTexture()
		{
			if (m_texture)
				m_texture.Destroy();

			bool horz = m_type == Type.Horizontal;

			m_texture = new Texture2D(horz ? m_textureSize : 1, horz ? 1 : m_textureSize, TextureFormat.RGBA32, false);
			Color[] colors = new Color[m_textureSize];

			for (int i = 0; i < m_textureSize; i++)
			{
				float norm = (float)i / (float)(m_textureSize - 1);
				Color color = m_gradient.Evaluate(norm);
				colors[i] = color;
			}

			m_texture.SetPixels(colors);
			m_texture.Apply(false, true);

			StartCoroutine(DelayedSetVerticesDirty());
		}

		private IEnumerator DelayedSetVerticesDirty()
		{
			yield return 0;
			SetVerticesDirty();
		}

		//		public virtual bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera) => true;

//		protected override void OnValidate()
//		{
//			base.OnValidate();
//			CreateTexture();
//		}

	}
}