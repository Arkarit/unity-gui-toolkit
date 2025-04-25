// Auto-generated, please do not change!
using System;
using System.Collections.Generic;
using UnityEngine;
using GuiToolkit;
using GuiToolkit.Style;

namespace GuiToolkit.Style
{
	[Serializable]
	public class UiStyleShadow : UiAbstractStyle<UnityEngine.UI.Shadow>
	{
		public UiStyleShadow(UiStyleConfig _styleConfig, string _name)
		{
			StyleConfig = _styleConfig;
			Name = _name;
		}

		private class ApplicableValueColor : ApplicableValue<UnityEngine.Color> {}
		private class ApplicableValueVector2 : ApplicableValue<UnityEngine.Vector2> {}
		private class ApplicableValueBoolean : ApplicableValue<System.Boolean> {}

		protected override ApplicableValueBase[] GetValueArray()
		{
			return new ApplicableValueBase[]
			{
				EffectColor,
				EffectDistance,
				Enabled,
			};
		}

#if UNITY_EDITOR
		public override ValueInfo[] GetValueInfoArray()
		{
			return new ValueInfo[]
			{
				new ValueInfo()
				{
					GetterName = "EffectColor",
					GetterType = typeof(ApplicableValueColor),
					Value = EffectColor,
				},
				new ValueInfo()
				{
					GetterName = "EffectDistance",
					GetterType = typeof(ApplicableValueVector2),
					Value = EffectDistance,
				},
				new ValueInfo()
				{
					GetterName = "Enabled",
					GetterType = typeof(ApplicableValueBoolean),
					Value = Enabled,
				},
			};
		}
#endif

		[SerializeReference] private ApplicableValueColor m_effectColor = new();
		[SerializeReference] private ApplicableValueVector2 m_effectDistance = new();
		[SerializeReference] private ApplicableValueBoolean m_enabled = new();

		public ApplicableValue<UnityEngine.Color> EffectColor
		{
			get
			{
				if (m_effectColor == null)
					m_effectColor = new ApplicableValueColor();
				return m_effectColor;
			}
		}

		public ApplicableValue<UnityEngine.Vector2> EffectDistance
		{
			get
			{
				if (m_effectDistance == null)
					m_effectDistance = new ApplicableValueVector2();
				return m_effectDistance;
			}
		}

		public ApplicableValue<System.Boolean> Enabled
		{
			get
			{
				if (m_enabled == null)
					m_enabled = new ApplicableValueBoolean();
				return m_enabled;
			}
		}

	}
}
