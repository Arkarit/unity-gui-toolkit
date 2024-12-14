// Auto-generated, please do not change!
using UnityEngine;
using GuiToolkit;
using GuiToolkit.Style;

namespace GuiToolkit.Style
{
	[ExecuteAlways]
	[RequireComponent(typeof(UnityEngine.UI.ScrollRect))]
	public class UiApplyStyleScrollRect : UiAbstractApplyStyle<UnityEngine.UI.ScrollRect, UiStyleScrollRect>
	{
		protected override void ApplyImpl()
		{
			if (!SpecificComponent || SpecificStyle == null)
				return;

			if (SpecificStyle.Enabled.IsApplicable)
				try { SpecificComponent.enabled = Tweenable ? SpecificStyle.Enabled.Value : SpecificStyle.Enabled.RawValue; } catch {}
			if (SpecificStyle.Horizontal.IsApplicable)
				try { SpecificComponent.horizontal = Tweenable ? SpecificStyle.Horizontal.Value : SpecificStyle.Horizontal.RawValue; } catch {}
			if (SpecificStyle.Vertical.IsApplicable)
				try { SpecificComponent.vertical = Tweenable ? SpecificStyle.Vertical.Value : SpecificStyle.Vertical.RawValue; } catch {}
		}

		protected override void RecordImpl()
		{
			if (!SpecificComponent || SpecificStyle == null)
				return;

			if (SpecificStyle.Enabled.IsApplicable)
				try { SpecificStyle.Enabled.RawValue = SpecificComponent.enabled; } catch {}
			if (SpecificStyle.Horizontal.IsApplicable)
				try { SpecificStyle.Horizontal.RawValue = SpecificComponent.horizontal; } catch {}
			if (SpecificStyle.Vertical.IsApplicable)
				try { SpecificStyle.Vertical.RawValue = SpecificComponent.vertical; } catch {}
		}

		public override UiAbstractStyleBase CreateStyle(UiStyleConfig _styleConfig, string _name, UiAbstractStyleBase _template = null)
		{
			UiStyleScrollRect result = new UiStyleScrollRect(_styleConfig, _name);

			if (!SpecificComponent)
				return result;

			if (_template != null)
			{
				var specificTemplate = (UiStyleScrollRect) _template;

				result.Enabled.Value = specificTemplate.Enabled.Value;
				result.Enabled.IsApplicable = specificTemplate.Enabled.IsApplicable;
				result.Horizontal.Value = specificTemplate.Horizontal.Value;
				result.Horizontal.IsApplicable = specificTemplate.Horizontal.IsApplicable;
				result.Vertical.Value = specificTemplate.Vertical.Value;
				result.Vertical.IsApplicable = specificTemplate.Vertical.IsApplicable;

				return result;
			}

			try { result.Enabled.Value = SpecificComponent.enabled; } catch {}
			try { result.Horizontal.Value = SpecificComponent.horizontal; } catch {}
			try { result.Vertical.Value = SpecificComponent.vertical; } catch {}

			return result;
		}
	}
}
