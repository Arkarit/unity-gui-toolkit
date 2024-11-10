// Auto-generated, please do not change!
using UnityEngine;
using GuiToolkit;
using GuiToolkit.Style;

namespace GuiToolkit.Style
{
	[ExecuteAlways]
	[RequireComponent(typeof(GuiToolkit.UiGradientEx))]
	public class UiApplyStyleUiGradientEx : UiAbstractApplyStyle<GuiToolkit.UiGradientEx, UiStyleUiGradientEx>
	{
		protected override void ApplyImpl()
		{
			if (!SpecificComponent || SpecificStyle == null)
				return;

			if (SpecificStyle.Gradient.IsApplicable)
				try { SpecificComponent.Gradient = Tweenable ? SpecificStyle.Gradient.Value : SpecificStyle.Gradient.RawValue; } catch {}
			if (SpecificStyle.Enabled.IsApplicable)
				try { SpecificComponent.enabled = Tweenable ? SpecificStyle.Enabled.Value : SpecificStyle.Enabled.RawValue; } catch {}
		}

		protected override void RecordImpl()
		{
			if (!SpecificComponent || SpecificStyle == null)
				return;

			if (SpecificStyle.Gradient.IsApplicable)
				try { SpecificStyle.Gradient.RawValue = SpecificComponent.Gradient; } catch {}
			if (SpecificStyle.Enabled.IsApplicable)
				try { SpecificStyle.Enabled.RawValue = SpecificComponent.enabled; } catch {}
		}

		public override UiAbstractStyleBase CreateStyle(UiStyleConfig _styleConfig, string _name, UiAbstractStyleBase _template = null)
		{
			UiStyleUiGradientEx result = new UiStyleUiGradientEx(_styleConfig, _name);

			if (!SpecificComponent)
				return result;

			if (_template != null)
			{
				var specificTemplate = (UiStyleUiGradientEx) _template;

				result.Gradient.Value = specificTemplate.Gradient.Value;
				result.Gradient.IsApplicable = specificTemplate.Gradient.IsApplicable;
				result.Enabled.Value = specificTemplate.Enabled.Value;
				result.Enabled.IsApplicable = specificTemplate.Enabled.IsApplicable;

				return result;
			}

			try { result.Gradient.Value = SpecificComponent.Gradient; } catch {}
			try { result.Enabled.Value = SpecificComponent.enabled; } catch {}

			return result;
		}
	}
}
