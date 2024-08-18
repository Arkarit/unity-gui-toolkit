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
				try { SpecificMonoBehaviour.ColorLeftOrTop = SpecificStyle.ColorLeftOrTop.Value; } catch {}
			if (SpecificStyle.ColorRightOrBottom.IsApplicable)
				try { SpecificMonoBehaviour.ColorRightOrBottom = SpecificStyle.ColorRightOrBottom.Value; } catch {}
			if (SpecificStyle.Axis.IsApplicable)
				try { SpecificMonoBehaviour.Axis = SpecificStyle.Axis.Value; } catch {}
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

				result.ColorLeftOrTop.Value = specificTemplate.ColorLeftOrTop.Value;
				result.ColorLeftOrTop.IsApplicable = specificTemplate.ColorLeftOrTop.IsApplicable;
				result.ColorRightOrBottom.Value = specificTemplate.ColorRightOrBottom.Value;
				result.ColorRightOrBottom.IsApplicable = specificTemplate.ColorRightOrBottom.IsApplicable;
				result.Axis.Value = specificTemplate.Axis.Value;
				result.Axis.IsApplicable = specificTemplate.Axis.IsApplicable;

				return result;
			}

			try { result.ColorLeftOrTop.Value = SpecificMonoBehaviour.ColorLeftOrTop; } catch {}
			try { result.ColorRightOrBottom.Value = SpecificMonoBehaviour.ColorRightOrBottom; } catch {}
			try { result.Axis.Value = SpecificMonoBehaviour.Axis; } catch {}

			return result;
		}
	}
}
