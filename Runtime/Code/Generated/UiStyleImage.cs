// Auto-generated, please do not change!
using System;
using UnityEngine;
using GuiToolkit.Style;

namespace GuiToolkit.Style
{
	[Serializable]
	public class UiStyleImage : UiAbstractStyle<UnityEngine.UI.Image>
	{
		[SerializeField] private ApplicableValue<UnityEngine.Sprite> m_sprite = new();
		[SerializeField] private ApplicableValue<UnityEngine.Sprite> m_overrideSprite = new();
		[SerializeField] private ApplicableValue<UnityEngine.UI.Image.Type> m_type = new();
		[SerializeField] private ApplicableValue<System.Boolean> m_preserveAspect = new();
		[SerializeField] private ApplicableValue<System.Boolean> m_fillCenter = new();
		[SerializeField] private ApplicableValue<UnityEngine.UI.Image.FillMethod> m_fillMethod = new();
		[SerializeField] private ApplicableValue<System.Single> m_fillAmount = new();
		[SerializeField] private ApplicableValue<System.Boolean> m_fillClockwise = new();
		[SerializeField] private ApplicableValue<System.Int32> m_fillOrigin = new();
		[SerializeField] private ApplicableValue<System.Boolean> m_useSpriteMesh = new();
		[SerializeField] private ApplicableValue<UnityEngine.Material> m_material = new();
		[SerializeField] private ApplicableValue<System.Boolean> m_maskable = new();
		[SerializeField] private ApplicableValue<System.Boolean> m_isMaskingGraphic = new();
		[SerializeField] private ApplicableValue<UnityEngine.Color> m_color = new();

		public UnityEngine.Sprite Sprite
		{
			get => m_sprite.Value;
			set => m_sprite.Value = value;
		}

		public UnityEngine.Sprite OverrideSprite
		{
			get => m_overrideSprite.Value;
			set => m_overrideSprite.Value = value;
		}

		public UnityEngine.UI.Image.Type Type
		{
			get => m_type.Value;
			set => m_type.Value = value;
		}

		public System.Boolean PreserveAspect
		{
			get => m_preserveAspect.Value;
			set => m_preserveAspect.Value = value;
		}

		public System.Boolean FillCenter
		{
			get => m_fillCenter.Value;
			set => m_fillCenter.Value = value;
		}

		public UnityEngine.UI.Image.FillMethod FillMethod
		{
			get => m_fillMethod.Value;
			set => m_fillMethod.Value = value;
		}

		public System.Single FillAmount
		{
			get => m_fillAmount.Value;
			set => m_fillAmount.Value = value;
		}

		public System.Boolean FillClockwise
		{
			get => m_fillClockwise.Value;
			set => m_fillClockwise.Value = value;
		}

		public System.Int32 FillOrigin
		{
			get => m_fillOrigin.Value;
			set => m_fillOrigin.Value = value;
		}

		public System.Boolean UseSpriteMesh
		{
			get => m_useSpriteMesh.Value;
			set => m_useSpriteMesh.Value = value;
		}

		public UnityEngine.Material Material
		{
			get => m_material.Value;
			set => m_material.Value = value;
		}

		public System.Boolean Maskable
		{
			get => m_maskable.Value;
			set => m_maskable.Value = value;
		}

		public System.Boolean IsMaskingGraphic
		{
			get => m_isMaskingGraphic.Value;
			set => m_isMaskingGraphic.Value = value;
		}

		public UnityEngine.Color Color
		{
			get => m_color.Value;
			set => m_color.Value = value;
		}

	}
}
