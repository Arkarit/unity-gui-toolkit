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
	public class UiStyleUiGradientSimple : UiAbstractStyle<GuiToolkit.UiGradientSimple>
	{
		private class ApplicableValueColor : ApplicableValue<UnityEngine.Color> {}
		private class ApplicableValueEAxis2D : ApplicableValue<GuiToolkit.EAxis2D> {}

		[SerializeReference] private ApplicableValueColor m_ColorLeftOrTop = new();
		[SerializeReference] private ApplicableValueColor m_ColorRightOrBottom = new();
		[SerializeReference] private ApplicableValueEAxis2D m_Axis = new();

		protected override ApplicableValueBase[] GetValueList()
		{
			return new ApplicableValueBase[]
			{
			};
		}

		public ApplicableValue<UnityEngine.Color> ColorLeftOrTop => m_ColorLeftOrTop;
		public ApplicableValue<UnityEngine.Color> ColorRightOrBottom => m_ColorRightOrBottom;
		public ApplicableValue<GuiToolkit.EAxis2D> Axis => m_Axis;

		public override UiAbstractStyleBase Clone() => new UiStyleUiGradientSimple()
		{
			m_ColorLeftOrTop = (ApplicableValueColor) m_ColorLeftOrTop.Clone(),
			m_ColorRightOrBottom = (ApplicableValueColor) m_ColorRightOrBottom.Clone(),
			m_Axis = (ApplicableValueEAxis2D) m_Axis.Clone(),
		};
	}
}
