// Auto-generated, please do not change!
using UnityEngine;
using GuiToolkit.Style;

namespace GuiToolkit.Style
{
	[ExecuteAlways]
	[RequireComponent(typeof(GuiToolkit.UiGradientSimple))]
	public class UiApplyStyleUiGradientSimple : UiAbstractApplyStyle<GuiToolkit.UiGradientSimple, UiStyleUiGradientSimple>
	{
		protected override void ApplyImpl()
		{
			if (!SpecificMonoBehaviour || SpecificStyle == null)
				return;

			if (SpecificStyle.ColorLeftOrTop.IsApplicable)
				try { SpecificMonoBehaviour.ColorLeftOrTop = SpecificStyle.ColorLeftOrTop.m_value; } catch {}
			if (SpecificStyle.ColorRightOrBottom.IsApplicable)
				try { SpecificMonoBehaviour.ColorRightOrBottom = SpecificStyle.ColorRightOrBottom.m_value; } catch {}
			if (SpecificStyle.Axis.IsApplicable)
				try { SpecificMonoBehaviour.Axis = SpecificStyle.Axis.m_value; } catch {}
		}

		public override UiAbstractStyleBase CreateStyle(string _name, UiAbstractStyleBase _template = null)
		{
			UiStyleUiGradientSimple result = new UiStyleUiGradientSimple();

			if (!SpecificMonoBehaviour)
				return result;

			result.Name = _name;

			if (_template != null)
			{
				var specificTemplate = (UiStyleUiGradientSimple) _template;

				result.ColorLeftOrTop.m_value = specificTemplate.ColorLeftOrTop.m_value;
				result.ColorLeftOrTop.IsApplicable = specificTemplate.ColorLeftOrTop.IsApplicable;
				result.ColorRightOrBottom.m_value = specificTemplate.ColorRightOrBottom.m_value;
				result.ColorRightOrBottom.IsApplicable = specificTemplate.ColorRightOrBottom.IsApplicable;
				result.Axis.m_value = specificTemplate.Axis.m_value;
				result.Axis.IsApplicable = specificTemplate.Axis.IsApplicable;

				return result;
			}

			try { result.ColorLeftOrTop.m_value = SpecificMonoBehaviour.ColorLeftOrTop; } catch {}
			try { result.ColorRightOrBottom.m_value = SpecificMonoBehaviour.ColorRightOrBottom; } catch {}
			try { result.Axis.m_value = SpecificMonoBehaviour.Axis; } catch {}

			return result;
		}
	}
}
