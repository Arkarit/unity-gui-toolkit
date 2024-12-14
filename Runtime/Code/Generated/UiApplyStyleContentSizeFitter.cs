// Auto-generated, please do not change!
using UnityEngine;
using GuiToolkit;
using GuiToolkit.Style;

namespace GuiToolkit.Style
{
	[ExecuteAlways]
	[RequireComponent(typeof(UnityEngine.UI.ContentSizeFitter))]
	public class UiApplyStyleContentSizeFitter : UiAbstractApplyStyle<UnityEngine.UI.ContentSizeFitter, UiStyleContentSizeFitter>
	{
		protected override void ApplyImpl()
		{
			if (!SpecificComponent || SpecificStyle == null)
				return;

			if (SpecificStyle.Enabled.IsApplicable)
				try { SpecificComponent.enabled = Tweenable ? SpecificStyle.Enabled.Value : SpecificStyle.Enabled.RawValue; } catch {}
			if (SpecificStyle.HorizontalFit.IsApplicable)
				try { SpecificComponent.horizontalFit = Tweenable ? SpecificStyle.HorizontalFit.Value : SpecificStyle.HorizontalFit.RawValue; } catch {}
			if (SpecificStyle.VerticalFit.IsApplicable)
				try { SpecificComponent.verticalFit = Tweenable ? SpecificStyle.VerticalFit.Value : SpecificStyle.VerticalFit.RawValue; } catch {}
		}

		protected override void RecordImpl()
		{
			if (!SpecificComponent || SpecificStyle == null)
				return;

			if (SpecificStyle.Enabled.IsApplicable)
				try { SpecificStyle.Enabled.RawValue = SpecificComponent.enabled; } catch {}
			if (SpecificStyle.HorizontalFit.IsApplicable)
				try { SpecificStyle.HorizontalFit.RawValue = SpecificComponent.horizontalFit; } catch {}
			if (SpecificStyle.VerticalFit.IsApplicable)
				try { SpecificStyle.VerticalFit.RawValue = SpecificComponent.verticalFit; } catch {}
		}

		public override UiAbstractStyleBase CreateStyle(UiStyleConfig _styleConfig, string _name, UiAbstractStyleBase _template = null)
		{
			UiStyleContentSizeFitter result = new UiStyleContentSizeFitter(_styleConfig, _name);

			if (!SpecificComponent)
				return result;

			if (_template != null)
			{
				var specificTemplate = (UiStyleContentSizeFitter) _template;

				result.Enabled.Value = specificTemplate.Enabled.Value;
				result.Enabled.IsApplicable = specificTemplate.Enabled.IsApplicable;
				result.HorizontalFit.Value = specificTemplate.HorizontalFit.Value;
				result.HorizontalFit.IsApplicable = specificTemplate.HorizontalFit.IsApplicable;
				result.VerticalFit.Value = specificTemplate.VerticalFit.Value;
				result.VerticalFit.IsApplicable = specificTemplate.VerticalFit.IsApplicable;

				return result;
			}

			try { result.Enabled.Value = SpecificComponent.enabled; } catch {}
			try { result.HorizontalFit.Value = SpecificComponent.horizontalFit; } catch {}
			try { result.VerticalFit.Value = SpecificComponent.verticalFit; } catch {}

			return result;
		}
	}
}
