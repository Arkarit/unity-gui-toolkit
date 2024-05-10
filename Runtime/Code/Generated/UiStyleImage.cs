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
		[SerializeField] private ApplicableValue<UnityEngine.Material> m_material = new();
		[SerializeField] private ApplicableValue<UnityEngine.Color> m_color = new();

		public ApplicableValue<UnityEngine.Sprite> Sprite => m_sprite;
		public ApplicableValue<UnityEngine.Sprite> OverrideSprite => m_overrideSprite;
		public ApplicableValue<UnityEngine.Material> Material => m_material;
		public ApplicableValue<UnityEngine.Color> Color => m_color;
	}
}
