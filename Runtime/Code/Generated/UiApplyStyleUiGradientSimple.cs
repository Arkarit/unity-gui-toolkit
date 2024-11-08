// Auto-generated, please do not change!
using UnityEngine;
using GuiToolkit;
using GuiToolkit.Style;

namespace GuiToolkit.Style
{
	[ExecuteAlways]
	[RequireComponent(typeof(GuiToolkit.UiGradientSimple))]
	public class UiApplyStyleUiGradientSimple : UiAbstractApplyStyle<GuiToolkit.UiGradientSimple, UiStyleUiGradientSimple>
	{
		protected override void ApplyImpl()
		{
			if (!SpecificComponent || SpecificStyle == null)
				return;

			if (SpecificStyle.ColorLeftOrTop.IsApplicable)
				try { SpecificComponent.ColorLeftOrTop = Tweenable ? SpecificStyle.ColorLeftOrTop.Value : SpecificStyle.ColorLeftOrTop.RawValue; } catch {}
			if (SpecificStyle.ColorRightOrBottom.IsApplicable)
				try { SpecificComponent.ColorRightOrBottom = Tweenable ? SpecificStyle.ColorRightOrBottom.Value : SpecificStyle.ColorRightOrBottom.RawValue; } catch {}
			if (SpecificStyle.Orientation.IsApplicable)
				try { SpecificComponent.Orientation = Tweenable ? SpecificStyle.Orientation.Value : SpecificStyle.Orientation.RawValue; } catch {}
			if (SpecificStyle.Enabled.IsApplicable)
				try { SpecificComponent.enabled = Tweenable ? SpecificStyle.Enabled.Value : SpecificStyle.Enabled.RawValue; } catch {}
		}

		protected override void RecordImpl()
		{
			if (!SpecificComponent || SpecificStyle == null)
				return;

			if (SpecificStyle.ColorLeftOrTop.IsApplicable)
				try { SpecificStyle.ColorLeftOrTop.RawValue = SpecificComponent.ColorLeftOrTop; } catch {}
			if (SpecificStyle.ColorRightOrBottom.IsApplicable)
				try { SpecificStyle.ColorRightOrBottom.RawValue = SpecificComponent.ColorRightOrBottom; } catch {}
			if (SpecificStyle.Orientation.IsApplicable)
				try { SpecificStyle.Orientation.RawValue = SpecificComponent.Orientation; } catch {}
			if (SpecificStyle.Enabled.IsApplicable)
				try { SpecificStyle.Enabled.RawValue = SpecificComponent.enabled; } catch {}
		}

		public override UiAbstractStyleBase CreateStyle(UiStyleConfig _styleConfig, string _name, UiAbstractStyleBase _template = null)
		{
			UiStyleUiGradientSimple result = new UiStyleUiGradientSimple(_styleConfig, _name);

			if (!SpecificComponent)
				return result;

			if (_template != null)
			{
				var specificTemplate = (UiStyleUiGradientSimple) _template;

				result.ColorLeftOrTop.Value = specificTemplate.ColorLeftOrTop.Value;
				result.ColorLeftOrTop.IsApplicable = specificTemplate.ColorLeftOrTop.IsApplicable;
				result.ColorRightOrBottom.Value = specificTemplate.ColorRightOrBottom.Value;
				result.ColorRightOrBottom.IsApplicable = specificTemplate.ColorRightOrBottom.IsApplicable;
				result.Orientation.Value = specificTemplate.Orientation.Value;
				result.Orientation.IsApplicable = specificTemplate.Orientation.IsApplicable;
				result.Enabled.Value = specificTemplate.Enabled.Value;
				result.Enabled.IsApplicable = specificTemplate.Enabled.IsApplicable;

				return result;
			}

			try { result.ColorLeftOrTop.Value = SpecificComponent.ColorLeftOrTop; } catch {}
			try { result.ColorRightOrBottom.Value = SpecificComponent.ColorRightOrBottom; } catch {}
			try { result.Orientation.Value = SpecificComponent.Orientation; } catch {}
			try { result.Enabled.Value = SpecificComponent.enabled; } catch {}

			return result;
		}
	}
}
