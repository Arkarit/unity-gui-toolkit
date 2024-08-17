// Auto-generated, please do not change!
using System;
using UnityEngine;
using GuiToolkit;
using GuiToolkit.Style;
using System.Collections.Generic;
using TMPro;

namespace GuiToolkit.Style
{
	[Serializable]
	public class UiStyleUiGradient : UiAbstractStyle<GuiToolkit.UiGradient>
	{
		private class ApplicableValueGradient : ApplicableValue<UnityEngine.Gradient> {}
		private class ApplicableValueEAxis2D : ApplicableValue<GuiToolkit.EAxis2D> {}

		protected override ApplicableValueBase[] GetValueList()
		{
			return new ApplicableValueBase[]
			{
			};
		}

		[SerializeReference] private ApplicableValueGradient m_Gradient = new();
		[SerializeReference] private ApplicableValueEAxis2D m_Axis = new();

		public ApplicableValue<UnityEngine.Gradient> Gradient => m_Gradient;
		public ApplicableValue<GuiToolkit.EAxis2D> Axis => m_Axis;

		public override UiAbstractStyleBase Clone() => new UiStyleUiGradient()
		{
			m_Gradient = (ApplicableValueGradient) m_Gradient.Clone(),
			m_Axis = (ApplicableValueEAxis2D) m_Axis.Clone(),
		};
	}
}
