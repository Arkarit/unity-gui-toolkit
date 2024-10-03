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
		private class ApplicableValueOrientation : ApplicableValue<GuiToolkit.UiGradientSimple.EOrientation> {}

		protected override ApplicableValueBase[] GetValueList()
		{
			return new ApplicableValueBase[]
			{
				m_ColorLeftOrTop,
				m_ColorRightOrBottom,
				m_Orientation,
			};
		}

		[SerializeReference] private ApplicableValueColor m_ColorLeftOrTop = new();
		[SerializeReference] private ApplicableValueColor m_ColorRightOrBottom = new();
		[SerializeReference] private ApplicableValueOrientation m_Orientation = new();

		public ApplicableValue<UnityEngine.Color> ColorLeftOrTop => m_ColorLeftOrTop;
		public ApplicableValue<UnityEngine.Color> ColorRightOrBottom => m_ColorRightOrBottom;
		public ApplicableValue<GuiToolkit.UiGradientSimple.EOrientation> Orientation => m_Orientation;
	}
}
