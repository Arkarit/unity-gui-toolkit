// Auto-generated, please do not change!
using UnityEngine;
using GuiToolkit;
using GuiToolkit.Style;

namespace GuiToolkit.Style
{
	[ExecuteAlways]
	[RequireComponent(typeof(GuiToolkit.UiRoundedImage))]
	public class UiApplyStyleUiRoundedImage : UiAbstractApplyStyle<GuiToolkit.UiRoundedImage, UiStyleUiRoundedImage>
	{
		protected override void ApplyImpl()
		{
			if (!SpecificComponent || SpecificStyle == null)
				return;

			if (SpecificStyle.Color.IsApplicable)
				try { SpecificComponent.color = Tweenable ? SpecificStyle.Color.Value : SpecificStyle.Color.RawValue; } catch {}
			if (SpecificStyle.CornerSegments.IsApplicable)
				try { SpecificComponent.CornerSegments = Tweenable ? SpecificStyle.CornerSegments.Value : SpecificStyle.CornerSegments.RawValue; } catch {}
			if (SpecificStyle.Enabled.IsApplicable)
				try { SpecificComponent.enabled = Tweenable ? SpecificStyle.Enabled.Value : SpecificStyle.Enabled.RawValue; } catch {}
			if (SpecificStyle.FadeSize.IsApplicable)
				try { SpecificComponent.FadeSize = Tweenable ? SpecificStyle.FadeSize.Value : SpecificStyle.FadeSize.RawValue; } catch {}
			if (SpecificStyle.FrameSize.IsApplicable)
				try { SpecificComponent.FrameSize = Tweenable ? SpecificStyle.FrameSize.Value : SpecificStyle.FrameSize.RawValue; } catch {}
			if (SpecificStyle.IsMaskingGraphic.IsApplicable)
				try { SpecificComponent.isMaskingGraphic = Tweenable ? SpecificStyle.IsMaskingGraphic.Value : SpecificStyle.IsMaskingGraphic.RawValue; } catch {}
			if (SpecificStyle.Maskable.IsApplicable)
				try { SpecificComponent.maskable = Tweenable ? SpecificStyle.Maskable.Value : SpecificStyle.Maskable.RawValue; } catch {}
			if (SpecificStyle.Material.IsApplicable)
				try { SpecificComponent.material = Tweenable ? SpecificStyle.Material.Value : SpecificStyle.Material.RawValue; } catch {}
			if (SpecificStyle.Radius.IsApplicable)
				try { SpecificComponent.Radius = Tweenable ? SpecificStyle.Radius.Value : SpecificStyle.Radius.RawValue; } catch {}
			if (SpecificStyle.Sprite.IsApplicable)
				try { SpecificComponent.sprite = Tweenable ? SpecificStyle.Sprite.Value : SpecificStyle.Sprite.RawValue; } catch {}
		}

		protected override void RecordImpl()
		{
			if (!SpecificComponent || SpecificStyle == null)
				return;

			if (SpecificStyle.Color.IsApplicable)
				try { SpecificStyle.Color.RawValue = SpecificComponent.color; } catch {}
			if (SpecificStyle.CornerSegments.IsApplicable)
				try { SpecificStyle.CornerSegments.RawValue = SpecificComponent.CornerSegments; } catch {}
			if (SpecificStyle.Enabled.IsApplicable)
				try { SpecificStyle.Enabled.RawValue = SpecificComponent.enabled; } catch {}
			if (SpecificStyle.FadeSize.IsApplicable)
				try { SpecificStyle.FadeSize.RawValue = SpecificComponent.FadeSize; } catch {}
			if (SpecificStyle.FrameSize.IsApplicable)
				try { SpecificStyle.FrameSize.RawValue = SpecificComponent.FrameSize; } catch {}
			if (SpecificStyle.IsMaskingGraphic.IsApplicable)
				try { SpecificStyle.IsMaskingGraphic.RawValue = SpecificComponent.isMaskingGraphic; } catch {}
			if (SpecificStyle.Maskable.IsApplicable)
				try { SpecificStyle.Maskable.RawValue = SpecificComponent.maskable; } catch {}
			if (SpecificStyle.Material.IsApplicable)
				try { SpecificStyle.Material.RawValue = SpecificComponent.material; } catch {}
			if (SpecificStyle.Radius.IsApplicable)
				try { SpecificStyle.Radius.RawValue = SpecificComponent.Radius; } catch {}
			if (SpecificStyle.Sprite.IsApplicable)
				try { SpecificStyle.Sprite.RawValue = SpecificComponent.sprite; } catch {}
		}

		public override UiAbstractStyleBase CreateStyle(UiStyleConfig _styleConfig, string _name, UiAbstractStyleBase _template = null)
		{
			UiStyleUiRoundedImage result = new UiStyleUiRoundedImage(_styleConfig, _name);

			if (!SpecificComponent)
				return result;

			if (_template != null)
			{
				var specificTemplate = (UiStyleUiRoundedImage) _template;

				result.Color.Value = specificTemplate.Color.Value;
				result.Color.IsApplicable = specificTemplate.Color.IsApplicable;
				result.CornerSegments.Value = specificTemplate.CornerSegments.Value;
				result.CornerSegments.IsApplicable = specificTemplate.CornerSegments.IsApplicable;
				result.Enabled.Value = specificTemplate.Enabled.Value;
				result.Enabled.IsApplicable = specificTemplate.Enabled.IsApplicable;
				result.FadeSize.Value = specificTemplate.FadeSize.Value;
				result.FadeSize.IsApplicable = specificTemplate.FadeSize.IsApplicable;
				result.FrameSize.Value = specificTemplate.FrameSize.Value;
				result.FrameSize.IsApplicable = specificTemplate.FrameSize.IsApplicable;
				result.IsMaskingGraphic.Value = specificTemplate.IsMaskingGraphic.Value;
				result.IsMaskingGraphic.IsApplicable = specificTemplate.IsMaskingGraphic.IsApplicable;
				result.Maskable.Value = specificTemplate.Maskable.Value;
				result.Maskable.IsApplicable = specificTemplate.Maskable.IsApplicable;
				result.Material.Value = specificTemplate.Material.Value;
				result.Material.IsApplicable = specificTemplate.Material.IsApplicable;
				result.Radius.Value = specificTemplate.Radius.Value;
				result.Radius.IsApplicable = specificTemplate.Radius.IsApplicable;
				result.Sprite.Value = specificTemplate.Sprite.Value;
				result.Sprite.IsApplicable = specificTemplate.Sprite.IsApplicable;

				return result;
			}

			try { result.Color.Value = SpecificComponent.color; } catch {}
			try { result.CornerSegments.Value = SpecificComponent.CornerSegments; } catch {}
			try { result.Enabled.Value = SpecificComponent.enabled; } catch {}
			try { result.FadeSize.Value = SpecificComponent.FadeSize; } catch {}
			try { result.FrameSize.Value = SpecificComponent.FrameSize; } catch {}
			try { result.IsMaskingGraphic.Value = SpecificComponent.isMaskingGraphic; } catch {}
			try { result.Maskable.Value = SpecificComponent.maskable; } catch {}
			try { result.Material.Value = SpecificComponent.material; } catch {}
			try { result.Radius.Value = SpecificComponent.Radius; } catch {}
			try { result.Sprite.Value = SpecificComponent.sprite; } catch {}

			return result;
		}
	}
}
