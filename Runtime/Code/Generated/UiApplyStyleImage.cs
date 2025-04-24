// Auto-generated, please do not change!
using UnityEngine;
using GuiToolkit;
using GuiToolkit.Style;

namespace GuiToolkit.Style
{
	[ExecuteAlways]
	[RequireComponent(typeof(UnityEngine.UI.Image))]
	public class UiApplyStyleImage : UiAbstractApplyStyle<UnityEngine.UI.Image, UiStyleImage>
	{
		protected override void ApplyImpl()
		{
			if (!SpecificComponent || SpecificStyle == null)
				return;

			if (SpecificStyle.Color.IsApplicable)
				try { SpecificComponent.color = Tweenable ? SpecificStyle.Color.Value : SpecificStyle.Color.RawValue; } catch {}
			if (SpecificStyle.Enabled.IsApplicable)
				try { SpecificComponent.enabled = Tweenable ? SpecificStyle.Enabled.Value : SpecificStyle.Enabled.RawValue; } catch {}
			if (SpecificStyle.Sprite.IsApplicable)
				try { SpecificComponent.sprite = Tweenable ? SpecificStyle.Sprite.Value : SpecificStyle.Sprite.RawValue; } catch {}
		}

		protected override void RecordImpl()
		{
			if (!SpecificComponent || SpecificStyle == null)
				return;

			if (SpecificStyle.Color.IsApplicable)
				try { SpecificStyle.Color.RawValue = SpecificComponent.color; } catch {}
			if (SpecificStyle.Enabled.IsApplicable)
				try { SpecificStyle.Enabled.RawValue = SpecificComponent.enabled; } catch {}
			if (SpecificStyle.Sprite.IsApplicable)
				try { SpecificStyle.Sprite.RawValue = SpecificComponent.sprite; } catch {}
		}

		public override UiAbstractStyleBase CreateStyle(UiStyleConfig _styleConfig, string _name, UiAbstractStyleBase _template = null)
		{
			UiStyleImage result = new UiStyleImage(_styleConfig, _name);

			if (!SpecificComponent)
				return result;

			if (_template != null)
			{
				var specificTemplate = (UiStyleImage) _template;

				result.Color.Value = specificTemplate.Color.Value;
				result.Color.IsApplicable = specificTemplate.Color.IsApplicable;
				result.Enabled.Value = specificTemplate.Enabled.Value;
				result.Enabled.IsApplicable = specificTemplate.Enabled.IsApplicable;
				result.Sprite.Value = specificTemplate.Sprite.Value;
				result.Sprite.IsApplicable = specificTemplate.Sprite.IsApplicable;

				return result;
			}

			try { result.Color.Value = SpecificComponent.color; } catch {}
			try { result.Enabled.Value = SpecificComponent.enabled; } catch {}
			try { result.Sprite.Value = SpecificComponent.sprite; } catch {}

			return result;
		}
	}
}
