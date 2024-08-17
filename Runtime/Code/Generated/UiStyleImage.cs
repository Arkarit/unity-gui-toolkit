// Auto-generated, please do not change!
using System;
using UnityEngine;
using GuiToolkit;
using GuiToolkit.Style;

namespace GuiToolkit.Style
{
	[Serializable]
	public class UiStyleImage : UiAbstractStyle<UnityEngine.UI.Image>
	{
		private class ApplicableValueSprite : ApplicableValue<UnityEngine.Sprite> {}
		private class ApplicableValueMaterial : ApplicableValue<UnityEngine.Material> {}
		private class ApplicableValueColor : ApplicableValue<UnityEngine.Color> {}

		protected override ApplicableValueBase[] GetValueList()
		{
			return new ApplicableValueBase[]
			{
				m_sprite,
				m_overrideSprite,
				m_material,
				m_color,
			};
		}

		[SerializeReference] private ApplicableValueSprite m_sprite = new();
		[SerializeReference] private ApplicableValueSprite m_overrideSprite = new();
		[SerializeReference] private ApplicableValueMaterial m_material = new();
		[SerializeReference] private ApplicableValueColor m_color = new();

		public ApplicableValue<UnityEngine.Sprite> Sprite => m_sprite;
		public ApplicableValue<UnityEngine.Sprite> OverrideSprite => m_overrideSprite;
		public ApplicableValue<UnityEngine.Material> Material => m_material;
		public ApplicableValue<UnityEngine.Color> Color => m_color;

		public override UiAbstractStyleBase Clone() => new UiStyleImage()
		{
			m_sprite = (ApplicableValueSprite) m_sprite.Clone(),
			m_overrideSprite = (ApplicableValueSprite) m_overrideSprite.Clone(),
			m_material = (ApplicableValueMaterial) m_material.Clone(),
			m_color = (ApplicableValueColor) m_color.Clone(),
		};
	}
}
