// Auto-generated, please do not change!
using System;
using UnityEngine;
using GuiToolkit.Style;

namespace GuiToolkit.Style
{
	[Serializable]
	public class UiStyleUiGradient : UiAbstractStyle<GuiToolkit.UiGradient>
	{
		private class ApplicableValueGradient : ApplicableValue<UnityEngine.Gradient> {}
		private class ApplicableValueEAxis2D : ApplicableValue<GuiToolkit.EAxis2D> {}

		[SerializeReference] private ApplicableValueGradient m_Gradient = new();
		[SerializeReference] private ApplicableValueEAxis2D m_Axis = new();

		public ApplicableValue<UnityEngine.Gradient> Gradient => m_Gradient;
		public ApplicableValue<GuiToolkit.EAxis2D> Axis => m_Axis;
	}
}
