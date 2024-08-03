// Auto-generated, please do not change!
using System;
using UnityEngine;
using GuiToolkit;
using GuiToolkit.Style;

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

		public ApplicableValue<UnityEngine.Color> ColorLeftOrTop => m_ColorLeftOrTop;
		public ApplicableValue<UnityEngine.Color> ColorRightOrBottom => m_ColorRightOrBottom;
		public ApplicableValue<GuiToolkit.EAxis2D> Axis => m_Axis;
	}
}
