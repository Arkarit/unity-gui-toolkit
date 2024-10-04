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

			if (SpecificStyle.Sprite.IsApplicable)
				try { SpecificComponent.sprite = Tweenable ? SpecificStyle.Sprite.Value : SpecificStyle.Sprite.RawValue; } catch {}
			if (SpecificStyle.Color.IsApplicable)
				try { SpecificComponent.color = Tweenable ? SpecificStyle.Color.Value : SpecificStyle.Color.RawValue; } catch {}
		}

		protected override void RecordImpl()
		{
			if (!SpecificComponent || SpecificStyle == null)
				return;

			if (SpecificStyle.Sprite.IsApplicable)
				try { SpecificStyle.Sprite.RawValue = SpecificComponent.sprite; } catch {}
			if (SpecificStyle.Color.IsApplicable)
				try { SpecificStyle.Color.RawValue = SpecificComponent.color; } catch {}
		}

		public override UiAbstractStyleBase CreateStyle(UiStyleConfig _styleConfig, string _name, UiAbstractStyleBase _template = null)
		{
			UiStyleImage result = new UiStyleImage(_styleConfig, _name);

			if (!SpecificComponent)
				return result;

			if (_template != null)
			{
				var specificTemplate = (UiStyleImage) _template;

				result.Sprite.Value = specificTemplate.Sprite.Value;
				result.Sprite.IsApplicable = specificTemplate.Sprite.IsApplicable;
				result.Color.Value = specificTemplate.Color.Value;
				result.Color.IsApplicable = specificTemplate.Color.IsApplicable;

				return result;
			}

			try { result.Sprite.Value = SpecificComponent.sprite; } catch {}
			try { result.Color.Value = SpecificComponent.color; } catch {}

			return result;
		}
	}
}
