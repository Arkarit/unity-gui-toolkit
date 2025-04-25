// Auto-generated, please do not change!
using System;
using System.Collections.Generic;
using UnityEngine;
using GuiToolkit;
using GuiToolkit.Style;

namespace GuiToolkit.Style
{
	[Serializable]
	public class UiStyleUiGradientEx : UiAbstractStyle<GuiToolkit.UiGradientEx>
	{
		public UiStyleUiGradientEx(UiStyleConfig _styleConfig, string _name)
		{
			StyleConfig = _styleConfig;
			Name = _name;
		}

		private class ApplicableValueBoolean : ApplicableValue<System.Boolean> {}
		private class ApplicableValueGradient : ApplicableValue<UnityEngine.Gradient> {}

		protected override ApplicableValueBase[] GetValueArray()
		{
			return new ApplicableValueBase[]
			{
				Enabled,
				Gradient,
			};
		}

#if UNITY_EDITOR
		public override ValueInfo[] GetValueInfoArray()
		{
			return new ValueInfo[]
			{
				new ValueInfo()
				{
					GetterName = "Enabled",
					GetterType = typeof(ApplicableValueBoolean),
					Value = Enabled,
				},
				new ValueInfo()
				{
					GetterName = "Gradient",
					GetterType = typeof(ApplicableValueGradient),
					Value = Gradient,
				},
			};
		}
#endif

		[SerializeReference] private ApplicableValueBoolean m_enabled = new();
		[SerializeReference] private ApplicableValueGradient m_Gradient = new();

		public ApplicableValue<System.Boolean> Enabled
		{
			get
			{
				if (m_enabled == null)
					m_enabled = new ApplicableValueBoolean();
				return m_enabled;
			}
		}

		public ApplicableValue<UnityEngine.Gradient> Gradient
		{
			get
			{
				if (m_Gradient == null)
					m_Gradient = new ApplicableValueGradient();
				return m_Gradient;
			}
		}

	}
}
