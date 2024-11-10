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
		public UiStyleUiGradientSimple(UiStyleConfig _styleConfig, string _name)
		{
			StyleConfig = _styleConfig;
			Name = _name;
		}

		private class ApplicableValueColor : ApplicableValue<UnityEngine.Color> {}
		private class ApplicableValueEOrientation : ApplicableValue<GuiToolkit.UiGradientSimple.EOrientation> {}
		private class ApplicableValueBoolean : ApplicableValue<System.Boolean> {}

		protected override ApplicableValueBase[] GetValueList()
		{
			return new ApplicableValueBase[]
			{
				ColorLeftOrTop,
				ColorRightOrBottom,
				Orientation,
				Enabled,
			};
		}

		[SerializeReference] private ApplicableValueColor m_ColorLeftOrTop = new();
		[SerializeReference] private ApplicableValueColor m_ColorRightOrBottom = new();
		[SerializeReference] private ApplicableValueEOrientation m_Orientation = new();
		[SerializeReference] private ApplicableValueBoolean m_enabled = new();

		public ApplicableValue<UnityEngine.Color> ColorLeftOrTop
		{
			get
			{
				if (m_ColorLeftOrTop == null)
					m_ColorLeftOrTop = new ApplicableValueColor();
				return m_ColorLeftOrTop;
			}
		}

		public ApplicableValue<UnityEngine.Color> ColorRightOrBottom
		{
			get
			{
				if (m_ColorRightOrBottom == null)
					m_ColorRightOrBottom = new ApplicableValueColor();
				return m_ColorRightOrBottom;
			}
		}

		public ApplicableValue<GuiToolkit.UiGradientSimple.EOrientation> Orientation
		{
			get
			{
				if (m_Orientation == null)
					m_Orientation = new ApplicableValueEOrientation();
				return m_Orientation;
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
