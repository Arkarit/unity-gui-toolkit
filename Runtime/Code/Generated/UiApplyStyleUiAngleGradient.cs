// Auto-generated, please do not change!
using UnityEngine;
using GuiToolkit;
using GuiToolkit.Style;

namespace GuiToolkit.Style
{
	[ExecuteAlways]
	[RequireComponent(typeof(GuiToolkit.UiAngleGradient))]
	public class UiApplyStyleUiAngleGradient : UiAbstractApplyStyle<GuiToolkit.UiAngleGradient, UiStyleUiAngleGradient>
	{
		protected override void ApplyImpl()
		{
			if (!SpecificComponent || SpecificStyle == null)
				return;

			if (SpecificStyle.ColorBase.IsApplicable)
				try { SpecificComponent.ColorBase = Tweenable ? SpecificStyle.ColorBase.Value : SpecificStyle.ColorBase.RawValue; } catch {}
			if (SpecificStyle.ColorHighlight.IsApplicable)
				try { SpecificComponent.ColorHighlight = Tweenable ? SpecificStyle.ColorHighlight.Value : SpecificStyle.ColorHighlight.RawValue; } catch {}
			if (SpecificStyle.Count.IsApplicable)
				try { SpecificComponent.Count = Tweenable ? SpecificStyle.Count.Value : SpecificStyle.Count.RawValue; } catch {}
			if (SpecificStyle.Enabled.IsApplicable)
				try { SpecificComponent.enabled = Tweenable ? SpecificStyle.Enabled.Value : SpecificStyle.Enabled.RawValue; } catch {}
			if (SpecificStyle.Rotation.IsApplicable)
				try { SpecificComponent.Rotation = Tweenable ? SpecificStyle.Rotation.Value : SpecificStyle.Rotation.RawValue; } catch {}
			if (SpecificStyle.Sharpness.IsApplicable)
				try { SpecificComponent.Sharpness = Tweenable ? SpecificStyle.Sharpness.Value : SpecificStyle.Sharpness.RawValue; } catch {}
			if (SpecificStyle.Speed.IsApplicable)
				try { SpecificComponent.Speed = Tweenable ? SpecificStyle.Speed.Value : SpecificStyle.Speed.RawValue; } catch {}
		}

		protected override void RecordImpl()
		{
			if (!SpecificComponent || SpecificStyle == null)
				return;

			if (SpecificStyle.ColorBase.IsApplicable)
				try { SpecificStyle.ColorBase.RawValue = SpecificComponent.ColorBase; } catch {}
			if (SpecificStyle.ColorHighlight.IsApplicable)
				try { SpecificStyle.ColorHighlight.RawValue = SpecificComponent.ColorHighlight; } catch {}
			if (SpecificStyle.Count.IsApplicable)
				try { SpecificStyle.Count.RawValue = SpecificComponent.Count; } catch {}
			if (SpecificStyle.Enabled.IsApplicable)
				try { SpecificStyle.Enabled.RawValue = SpecificComponent.enabled; } catch {}
			if (SpecificStyle.Rotation.IsApplicable)
				try { SpecificStyle.Rotation.RawValue = SpecificComponent.Rotation; } catch {}
			if (SpecificStyle.Sharpness.IsApplicable)
				try { SpecificStyle.Sharpness.RawValue = SpecificComponent.Sharpness; } catch {}
			if (SpecificStyle.Speed.IsApplicable)
				try { SpecificStyle.Speed.RawValue = SpecificComponent.Speed; } catch {}
		}

		public override UiAbstractStyleBase CreateStyle(UiStyleConfig _styleConfig, string _name, UiAbstractStyleBase _template = null)
		{
			UiStyleUiAngleGradient result = new UiStyleUiAngleGradient(_styleConfig, _name);

			if (!SpecificComponent)
				return result;

			if (_template != null)
			{
				var specificTemplate = (UiStyleUiAngleGradient) _template;

				result.ColorBase.Value = specificTemplate.ColorBase.Value;
				result.ColorBase.IsApplicable = specificTemplate.ColorBase.IsApplicable;
				result.ColorHighlight.Value = specificTemplate.ColorHighlight.Value;
				result.ColorHighlight.IsApplicable = specificTemplate.ColorHighlight.IsApplicable;
				result.Count.Value = specificTemplate.Count.Value;
				result.Count.IsApplicable = specificTemplate.Count.IsApplicable;
				result.Enabled.Value = specificTemplate.Enabled.Value;
				result.Enabled.IsApplicable = specificTemplate.Enabled.IsApplicable;
				result.Rotation.Value = specificTemplate.Rotation.Value;
				result.Rotation.IsApplicable = specificTemplate.Rotation.IsApplicable;
				result.Sharpness.Value = specificTemplate.Sharpness.Value;
				result.Sharpness.IsApplicable = specificTemplate.Sharpness.IsApplicable;
				result.Speed.Value = specificTemplate.Speed.Value;
				result.Speed.IsApplicable = specificTemplate.Speed.IsApplicable;

				return result;
			}

			try { result.ColorBase.Value = SpecificComponent.ColorBase; } catch {}
			try { result.ColorHighlight.Value = SpecificComponent.ColorHighlight; } catch {}
			try { result.Count.Value = SpecificComponent.Count; } catch {}
			try { result.Enabled.Value = SpecificComponent.enabled; } catch {}
			try { result.Rotation.Value = SpecificComponent.Rotation; } catch {}
			try { result.Sharpness.Value = SpecificComponent.Sharpness; } catch {}
			try { result.Speed.Value = SpecificComponent.Speed; } catch {}

			return result;
		}
	}
}
