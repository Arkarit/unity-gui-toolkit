// Auto-generated, please do not change!
using System;
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

		private class ApplicableValueGradient : ApplicableValue<UnityEngine.Gradient> {}
		private class ApplicableValueBoolean : ApplicableValue<System.Boolean> {}

		protected override ApplicableValueBase[] GetValueArray()
		{
			return new ApplicableValueBase[]
			{
				Gradient,
				Enabled,
			};
		}

		[SerializeReference] private ApplicableValueGradient m_Gradient = new();
		[SerializeReference] private ApplicableValueBoolean m_enabled = new();

		public ApplicableValue<UnityEngine.Gradient> Gradient
		{
			get
			{
				if (m_Gradient == null)
					m_Gradient = new ApplicableValueGradient();
				return m_Gradient;
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
