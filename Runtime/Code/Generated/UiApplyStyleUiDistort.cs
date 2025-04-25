// Auto-generated, please do not change!
using UnityEngine;
using GuiToolkit;
using GuiToolkit.Style;

namespace GuiToolkit.Style
{
	[ExecuteAlways]
	[RequireComponent(typeof(GuiToolkit.UiDistort))]
	public class UiApplyStyleUiDistort : UiAbstractApplyStyle<GuiToolkit.UiDistort, UiStyleUiDistort>
	{
		protected override void ApplyImpl()
		{
			if (!SpecificComponent || SpecificStyle == null)
				return;

			if (SpecificStyle.BottomLeft.IsApplicable)
				try { SpecificComponent.BottomLeft = Tweenable ? SpecificStyle.BottomLeft.Value : SpecificStyle.BottomLeft.RawValue; } catch {}
			if (SpecificStyle.BottomRight.IsApplicable)
				try { SpecificComponent.BottomRight = Tweenable ? SpecificStyle.BottomRight.Value : SpecificStyle.BottomRight.RawValue; } catch {}
			if (SpecificStyle.Enabled.IsApplicable)
				try { SpecificComponent.enabled = Tweenable ? SpecificStyle.Enabled.Value : SpecificStyle.Enabled.RawValue; } catch {}
			if (SpecificStyle.IsAbsolute.IsApplicable)
				try { SpecificComponent.IsAbsolute = Tweenable ? SpecificStyle.IsAbsolute.Value : SpecificStyle.IsAbsolute.RawValue; } catch {}
			if (SpecificStyle.Mirror.IsApplicable)
				try { SpecificComponent.Mirror = Tweenable ? SpecificStyle.Mirror.Value : SpecificStyle.Mirror.RawValue; } catch {}
			if (SpecificStyle.TopLeft.IsApplicable)
				try { SpecificComponent.TopLeft = Tweenable ? SpecificStyle.TopLeft.Value : SpecificStyle.TopLeft.RawValue; } catch {}
			if (SpecificStyle.TopRight.IsApplicable)
				try { SpecificComponent.TopRight = Tweenable ? SpecificStyle.TopRight.Value : SpecificStyle.TopRight.RawValue; } catch {}
		}

		protected override void RecordImpl()
		{
			if (!SpecificComponent || SpecificStyle == null)
				return;

			if (SpecificStyle.BottomLeft.IsApplicable)
				try { SpecificStyle.BottomLeft.RawValue = SpecificComponent.BottomLeft; } catch {}
			if (SpecificStyle.BottomRight.IsApplicable)
				try { SpecificStyle.BottomRight.RawValue = SpecificComponent.BottomRight; } catch {}
			if (SpecificStyle.Enabled.IsApplicable)
				try { SpecificStyle.Enabled.RawValue = SpecificComponent.enabled; } catch {}
			if (SpecificStyle.IsAbsolute.IsApplicable)
				try { SpecificStyle.IsAbsolute.RawValue = SpecificComponent.IsAbsolute; } catch {}
			if (SpecificStyle.Mirror.IsApplicable)
				try { SpecificStyle.Mirror.RawValue = SpecificComponent.Mirror; } catch {}
			if (SpecificStyle.TopLeft.IsApplicable)
				try { SpecificStyle.TopLeft.RawValue = SpecificComponent.TopLeft; } catch {}
			if (SpecificStyle.TopRight.IsApplicable)
				try { SpecificStyle.TopRight.RawValue = SpecificComponent.TopRight; } catch {}
		}

		public override UiAbstractStyleBase CreateStyle(UiStyleConfig _styleConfig, string _name, UiAbstractStyleBase _template = null)
		{
			UiStyleUiDistort result = new UiStyleUiDistort(_styleConfig, _name);

			if (!SpecificComponent)
				return result;

			if (_template != null)
			{
				var specificTemplate = (UiStyleUiDistort) _template;

				result.BottomLeft.Value = specificTemplate.BottomLeft.Value;
				result.BottomLeft.IsApplicable = specificTemplate.BottomLeft.IsApplicable;
				result.BottomRight.Value = specificTemplate.BottomRight.Value;
				result.BottomRight.IsApplicable = specificTemplate.BottomRight.IsApplicable;
				result.Enabled.Value = specificTemplate.Enabled.Value;
				result.Enabled.IsApplicable = specificTemplate.Enabled.IsApplicable;
				result.IsAbsolute.Value = specificTemplate.IsAbsolute.Value;
				result.IsAbsolute.IsApplicable = specificTemplate.IsAbsolute.IsApplicable;
				result.Mirror.Value = specificTemplate.Mirror.Value;
				result.Mirror.IsApplicable = specificTemplate.Mirror.IsApplicable;
				result.TopLeft.Value = specificTemplate.TopLeft.Value;
				result.TopLeft.IsApplicable = specificTemplate.TopLeft.IsApplicable;
				result.TopRight.Value = specificTemplate.TopRight.Value;
				result.TopRight.IsApplicable = specificTemplate.TopRight.IsApplicable;

				return result;
			}

			try { result.BottomLeft.Value = SpecificComponent.BottomLeft; } catch {}
			try { result.BottomRight.Value = SpecificComponent.BottomRight; } catch {}
			try { result.Enabled.Value = SpecificComponent.enabled; } catch {}
			try { result.IsAbsolute.Value = SpecificComponent.IsAbsolute; } catch {}
			try { result.Mirror.Value = SpecificComponent.Mirror; } catch {}
			try { result.TopLeft.Value = SpecificComponent.TopLeft; } catch {}
			try { result.TopRight.Value = SpecificComponent.TopRight; } catch {}

			return result;
		}
	}
}
