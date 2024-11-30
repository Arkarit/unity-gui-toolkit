// Auto-generated, please do not change!
using UnityEngine;
using GuiToolkit;
using GuiToolkit.Style;

namespace GuiToolkit.Style
{
	[ExecuteAlways]
	[RequireComponent(typeof(UnityEngine.UI.Shadow))]
	public class UiApplyStyleShadow : UiAbstractApplyStyle<UnityEngine.UI.Shadow, UiStyleShadow>
	{
		protected override void ApplyImpl()
		{
			if (!SpecificComponent || SpecificStyle == null)
				return;

			if (SpecificStyle.EffectColor.IsApplicable)
				try { SpecificComponent.effectColor = Tweenable ? SpecificStyle.EffectColor.Value : SpecificStyle.EffectColor.RawValue; } catch {}
			if (SpecificStyle.EffectDistance.IsApplicable)
				try { SpecificComponent.effectDistance = Tweenable ? SpecificStyle.EffectDistance.Value : SpecificStyle.EffectDistance.RawValue; } catch {}
			if (SpecificStyle.Enabled.IsApplicable)
				try { SpecificComponent.enabled = Tweenable ? SpecificStyle.Enabled.Value : SpecificStyle.Enabled.RawValue; } catch {}
		}

		protected override void RecordImpl()
		{
			if (!SpecificComponent || SpecificStyle == null)
				return;

			if (SpecificStyle.EffectColor.IsApplicable)
				try { SpecificStyle.EffectColor.RawValue = SpecificComponent.effectColor; } catch {}
			if (SpecificStyle.EffectDistance.IsApplicable)
				try { SpecificStyle.EffectDistance.RawValue = SpecificComponent.effectDistance; } catch {}
			if (SpecificStyle.Enabled.IsApplicable)
				try { SpecificStyle.Enabled.RawValue = SpecificComponent.enabled; } catch {}
		}

		public override UiAbstractStyleBase CreateStyle(UiStyleConfig _styleConfig, string _name, UiAbstractStyleBase _template = null)
		{
			UiStyleShadow result = new UiStyleShadow(_styleConfig, _name);

			if (!SpecificComponent)
				return result;

			if (_template != null)
			{
				var specificTemplate = (UiStyleShadow) _template;

				result.EffectColor.Value = specificTemplate.EffectColor.Value;
				result.EffectColor.IsApplicable = specificTemplate.EffectColor.IsApplicable;
				result.EffectDistance.Value = specificTemplate.EffectDistance.Value;
				result.EffectDistance.IsApplicable = specificTemplate.EffectDistance.IsApplicable;
				result.Enabled.Value = specificTemplate.Enabled.Value;
				result.Enabled.IsApplicable = specificTemplate.Enabled.IsApplicable;

				return result;
			}

			try { result.EffectColor.Value = SpecificComponent.effectColor; } catch {}
			try { result.EffectDistance.Value = SpecificComponent.effectDistance; } catch {}
			try { result.Enabled.Value = SpecificComponent.enabled; } catch {}

			return result;
		}
	}
}
