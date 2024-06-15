// Auto-generated, please do not change!
using System;
using System.Collections.Generic;
using UnityEngine;
using GuiToolkit.Style;

namespace GuiToolkit.Style
{
	[Serializable]
	public class UiStyleImage : UiAbstractStyle<UnityEngine.UI.Image>
	{
		private readonly List<ApplicableValueBase> m_values = new();
		private readonly List<object> m_defaultValues = new();

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

		public override List<ApplicableValueBase> Values
		{
			get
			{
				if (m_values.Count == 0)
				{
					m_values.Add(m_sprite);
					m_values.Add(m_overrideSprite);
					m_values.Add(m_material);
					m_values.Add(m_color);
				}

				return m_values;
			}
		}

		public override List<object> DefaultValues
		{
			get
			{
				if (m_defaultValues.Count == 0)
				{
					var defaultComponent = this.GetOrCreateComponent<UnityEngine.UI.Image>();

					m_defaultValues.Add(defaultComponent.sprite);
					m_defaultValues.Add(defaultComponent.overrideSprite);
					m_defaultValues.Add(defaultComponent.material);
					m_defaultValues.Add(defaultComponent.color);
				}

				return m_defaultValues;
			}
		}
	}
}
