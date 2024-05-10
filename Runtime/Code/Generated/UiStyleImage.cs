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

		public ApplicableValue<UnityEngine.Sprite> Sprite => m_sprite;
		public ApplicableValue<UnityEngine.Sprite> OverrideSprite => m_overrideSprite;
		public ApplicableValue<UnityEngine.UI.Image.Type> Type => m_type;
		public ApplicableValue<System.Boolean> PreserveAspect => m_preserveAspect;
		public ApplicableValue<System.Boolean> FillCenter => m_fillCenter;
		public ApplicableValue<UnityEngine.UI.Image.FillMethod> FillMethod => m_fillMethod;
		public ApplicableValue<System.Single> FillAmount => m_fillAmount;
		public ApplicableValue<System.Boolean> FillClockwise => m_fillClockwise;
		public ApplicableValue<System.Int32> FillOrigin => m_fillOrigin;
		public ApplicableValue<System.Boolean> UseSpriteMesh => m_useSpriteMesh;
		public ApplicableValue<UnityEngine.Material> Material => m_material;
		public ApplicableValue<System.Boolean> Maskable => m_maskable;
		public ApplicableValue<System.Boolean> IsMaskingGraphic => m_isMaskingGraphic;
		public ApplicableValue<UnityEngine.Color> Color => m_color;
	}
}
