// Auto-generated, please do not change!
using System;
using UnityEngine;
using GuiToolkit.Style;

namespace GuiToolkit.Style
{
	[Serializable]
	public class UiStyleImage : UiAbstractStyle<UnityEngine.UI.Image>
	{
		private class ApplicableValueSprite : ApplicableValue<UnityEngine.Sprite> {}
		private class ApplicableValueMaterial : ApplicableValue<UnityEngine.Material> {}
		private class ApplicableValueColor : ApplicableValue<UnityEngine.Color> {}

		[SerializeReference] private ApplicableValueSprite m_sprite = new();
		[SerializeReference] private ApplicableValueSprite m_overrideSprite = new();
		[SerializeReference] private ApplicableValueMaterial m_material = new();
		[SerializeReference] private ApplicableValueColor m_color = new();

		public ApplicableValue<UnityEngine.Sprite> Sprite => m_sprite;
		public ApplicableValue<UnityEngine.Sprite> OverrideSprite => m_overrideSprite;
		public ApplicableValue<UnityEngine.Material> Material => m_material;
		public ApplicableValue<UnityEngine.Color> Color => m_color;
	}
}
