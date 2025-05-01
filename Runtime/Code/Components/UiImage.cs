using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit
{
	public class UiImage : UiThing
	{
		[SerializeField] protected Image m_image;
		[Tooltip("Simple Gradient. Mandatory if you want to use the 'SimpleGradientColors' getters+setters.")]
		[SerializeField] protected UiGradientSimple m_gradientSimple;
		[SerializeField] protected bool m_supportDisabledMaterial = true;
		[SerializeField] protected Material m_normalMaterial;
		[SerializeField] protected Material m_disabledMaterial;
		
		public override bool IsEnableableInHierarchy => m_supportDisabledMaterial;

		public Image Image => m_image;

		public Color Color
		{
			get
			{
				if (m_image == null)
					return Color.white;

				return m_image.color;
			}
			set
			{
				if (m_image == null)
				{
					Debug.LogError("Attempt to set button color, but background image was not set");
					return;
				}

				m_image.color = value;
			}
		}

		public void SetSimpleGradientColors(Color _leftOrTop, Color _rightOrBottom)
		{
			if (m_gradientSimple == null)
			{
				Debug.LogError("Attempt to set simple gradient colors, but simple gradient was not set");
				return;
			}
			m_gradientSimple.SetColors(_leftOrTop, _rightOrBottom);
		}

		public (Color leftOrTop, Color rightOrBottom) GetSimpleGradientColors()
		{
			if (m_gradientSimple == null)
				return (leftOrTop:Color.white, rightOrBottom:Color.white);
			return m_gradientSimple.GetColors();
		}

		protected override void OnEnable()
		{
			base.OnEnable();
			SetMaterialByEnabled();
		}

		protected virtual void Init() { }
		protected override void OnEnabledInHierarchyChanged(bool _enabled)
		{
			SetMaterialByEnabled();
		}

#if UNITY_EDITOR
		private void OnValidate()
		{
			SetMaterialByEnabled();
			OnEnabledInHierarchyChanged(EnabledInHierarchy);
		}
#endif

		private void SetMaterialByEnabled()
		{
			if (!m_supportDisabledMaterial)
				return;

			if (m_image && m_normalMaterial && m_disabledMaterial)
			{
				// Note: Image.material matches Renderer.sharedMaterial: setting Image.material does NOT create a material clone
				m_image.material = EnabledInHierarchy ? m_normalMaterial : m_disabledMaterial;
			}
		}
	}
}