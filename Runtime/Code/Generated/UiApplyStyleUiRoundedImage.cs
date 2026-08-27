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
			if (SpecificStyle.DisabledMaterial.IsApplicable)
				try { SpecificComponent.DisabledMaterial = Tweenable ? SpecificStyle.DisabledMaterial.Value : SpecificStyle.DisabledMaterial.RawValue; } catch {}
			if (SpecificStyle.Enabled.IsApplicable)
				try { SpecificComponent.enabled = Tweenable ? SpecificStyle.Enabled.Value : SpecificStyle.Enabled.RawValue; } catch {}
			if (SpecificStyle.EnabledInHierarchy.IsApplicable)
				try { SpecificComponent.EnabledInHierarchy = Tweenable ? SpecificStyle.EnabledInHierarchy.Value : SpecificStyle.EnabledInHierarchy.RawValue; } catch {}
			if (SpecificStyle.FadeColor.IsApplicable)
				try { SpecificComponent.FadeColor = Tweenable ? SpecificStyle.FadeColor.Value : SpecificStyle.FadeColor.RawValue; } catch {}
			if (SpecificStyle.FadeSize.IsApplicable)
				try { SpecificComponent.FadeSize = Tweenable ? SpecificStyle.FadeSize.Value : SpecificStyle.FadeSize.RawValue; } catch {}
			if (SpecificStyle.FillAmount.IsApplicable)
				try { SpecificComponent.fillAmount = Tweenable ? SpecificStyle.FillAmount.Value : SpecificStyle.FillAmount.RawValue; } catch {}
			if (SpecificStyle.FillCenter.IsApplicable)
				try { SpecificComponent.fillCenter = Tweenable ? SpecificStyle.FillCenter.Value : SpecificStyle.FillCenter.RawValue; } catch {}
			if (SpecificStyle.FillClockwise.IsApplicable)
				try { SpecificComponent.fillClockwise = Tweenable ? SpecificStyle.FillClockwise.Value : SpecificStyle.FillClockwise.RawValue; } catch {}
			if (SpecificStyle.FillMethod.IsApplicable)
				try { SpecificComponent.fillMethod = Tweenable ? SpecificStyle.FillMethod.Value : SpecificStyle.FillMethod.RawValue; } catch {}
			if (SpecificStyle.FillOrigin.IsApplicable)
				try { SpecificComponent.fillOrigin = Tweenable ? SpecificStyle.FillOrigin.Value : SpecificStyle.FillOrigin.RawValue; } catch {}
			if (SpecificStyle.FixedSize.IsApplicable)
				try { SpecificComponent.FixedSize = Tweenable ? SpecificStyle.FixedSize.Value : SpecificStyle.FixedSize.RawValue; } catch {}
			if (SpecificStyle.FrameSize.IsApplicable)
				try { SpecificComponent.FrameSize = Tweenable ? SpecificStyle.FrameSize.Value : SpecificStyle.FrameSize.RawValue; } catch {}
			if (SpecificStyle.GapBottom.IsApplicable)
				try { SpecificComponent.GapBottom = Tweenable ? SpecificStyle.GapBottom.Value : SpecificStyle.GapBottom.RawValue; } catch {}
			if (SpecificStyle.GapHorizontal.IsApplicable)
				try { SpecificComponent.GapHorizontal = Tweenable ? SpecificStyle.GapHorizontal.Value : SpecificStyle.GapHorizontal.RawValue; } catch {}
			if (SpecificStyle.GapLeft.IsApplicable)
				try { SpecificComponent.GapLeft = Tweenable ? SpecificStyle.GapLeft.Value : SpecificStyle.GapLeft.RawValue; } catch {}
			if (SpecificStyle.GapRight.IsApplicable)
				try { SpecificComponent.GapRight = Tweenable ? SpecificStyle.GapRight.Value : SpecificStyle.GapRight.RawValue; } catch {}
			if (SpecificStyle.GapTop.IsApplicable)
				try { SpecificComponent.GapTop = Tweenable ? SpecificStyle.GapTop.Value : SpecificStyle.GapTop.RawValue; } catch {}
			if (SpecificStyle.GapUnit.IsApplicable)
				try { SpecificComponent.GapUnit = Tweenable ? SpecificStyle.GapUnit.Value : SpecificStyle.GapUnit.RawValue; } catch {}
			if (SpecificStyle.GapVertical.IsApplicable)
				try { SpecificComponent.GapVertical = Tweenable ? SpecificStyle.GapVertical.Value : SpecificStyle.GapVertical.RawValue; } catch {}
			if (SpecificStyle.InvertMask.IsApplicable)
				try { SpecificComponent.InvertMask = Tweenable ? SpecificStyle.InvertMask.Value : SpecificStyle.InvertMask.RawValue; } catch {}
			if (SpecificStyle.IsMaskingGraphic.IsApplicable)
				try { SpecificComponent.isMaskingGraphic = Tweenable ? SpecificStyle.IsMaskingGraphic.Value : SpecificStyle.IsMaskingGraphic.RawValue; } catch {}
			if (SpecificStyle.Maskable.IsApplicable)
				try { SpecificComponent.maskable = Tweenable ? SpecificStyle.Maskable.Value : SpecificStyle.Maskable.RawValue; } catch {}
			if (SpecificStyle.Material.IsApplicable)
				try { SpecificComponent.material = Tweenable ? SpecificStyle.Material.Value : SpecificStyle.Material.RawValue; } catch {}
			if (SpecificStyle.Padding.IsApplicable)
				try { SpecificComponent.Padding = Tweenable ? SpecificStyle.Padding.Value : SpecificStyle.Padding.RawValue; } catch {}
			if (SpecificStyle.PixelsPerUnitMultiplier.IsApplicable)
				try { SpecificComponent.pixelsPerUnitMultiplier = Tweenable ? SpecificStyle.PixelsPerUnitMultiplier.Value : SpecificStyle.PixelsPerUnitMultiplier.RawValue; } catch {}
			if (SpecificStyle.PositionOffset.IsApplicable)
				try { SpecificComponent.PositionOffset = Tweenable ? SpecificStyle.PositionOffset.Value : SpecificStyle.PositionOffset.RawValue; } catch {}
			if (SpecificStyle.Radius.IsApplicable)
				try { SpecificComponent.Radius = Tweenable ? SpecificStyle.Radius.Value : SpecificStyle.Radius.RawValue; } catch {}
			if (SpecificStyle.SizeOffset.IsApplicable)
				try { SpecificComponent.SizeOffset = Tweenable ? SpecificStyle.SizeOffset.Value : SpecificStyle.SizeOffset.RawValue; } catch {}
			if (SpecificStyle.Sprite.IsApplicable)
				try { SpecificComponent.sprite = Tweenable ? SpecificStyle.Sprite.Value : SpecificStyle.Sprite.RawValue; } catch {}
			if (SpecificStyle.UniformSizeOffset.IsApplicable)
				try { SpecificComponent.UniformSizeOffset = Tweenable ? SpecificStyle.UniformSizeOffset.Value : SpecificStyle.UniformSizeOffset.RawValue; } catch {}
			if (SpecificStyle.UseFixedSize.IsApplicable)
				try { SpecificComponent.UseFixedSize = Tweenable ? SpecificStyle.UseFixedSize.Value : SpecificStyle.UseFixedSize.RawValue; } catch {}
			if (SpecificStyle.UsePadding.IsApplicable)
				try { SpecificComponent.UsePadding = Tweenable ? SpecificStyle.UsePadding.Value : SpecificStyle.UsePadding.RawValue; } catch {}
		}

		protected override void RecordImpl()
		{
			if (!SpecificComponent || SpecificStyle == null)
				return;

			if (SpecificStyle.Color.IsApplicable)
				try { SpecificStyle.Color.RawValue = SpecificComponent.color; } catch {}
			if (SpecificStyle.CornerSegments.IsApplicable)
				try { SpecificStyle.CornerSegments.RawValue = SpecificComponent.CornerSegments; } catch {}
			if (SpecificStyle.DisabledMaterial.IsApplicable)
				try { SpecificStyle.DisabledMaterial.RawValue = SpecificComponent.DisabledMaterial; } catch {}
			if (SpecificStyle.Enabled.IsApplicable)
				try { SpecificStyle.Enabled.RawValue = SpecificComponent.enabled; } catch {}
			if (SpecificStyle.EnabledInHierarchy.IsApplicable)
				try { SpecificStyle.EnabledInHierarchy.RawValue = SpecificComponent.EnabledInHierarchy; } catch {}
			if (SpecificStyle.FadeColor.IsApplicable)
				try { SpecificStyle.FadeColor.RawValue = SpecificComponent.FadeColor; } catch {}
			if (SpecificStyle.FadeSize.IsApplicable)
				try { SpecificStyle.FadeSize.RawValue = SpecificComponent.FadeSize; } catch {}
			if (SpecificStyle.FillAmount.IsApplicable)
				try { SpecificStyle.FillAmount.RawValue = SpecificComponent.fillAmount; } catch {}
			if (SpecificStyle.FillCenter.IsApplicable)
				try { SpecificStyle.FillCenter.RawValue = SpecificComponent.fillCenter; } catch {}
			if (SpecificStyle.FillClockwise.IsApplicable)
				try { SpecificStyle.FillClockwise.RawValue = SpecificComponent.fillClockwise; } catch {}
			if (SpecificStyle.FillMethod.IsApplicable)
				try { SpecificStyle.FillMethod.RawValue = SpecificComponent.fillMethod; } catch {}
			if (SpecificStyle.FillOrigin.IsApplicable)
				try { SpecificStyle.FillOrigin.RawValue = SpecificComponent.fillOrigin; } catch {}
			if (SpecificStyle.FixedSize.IsApplicable)
				try { SpecificStyle.FixedSize.RawValue = SpecificComponent.FixedSize; } catch {}
			if (SpecificStyle.FrameSize.IsApplicable)
				try { SpecificStyle.FrameSize.RawValue = SpecificComponent.FrameSize; } catch {}
			if (SpecificStyle.GapBottom.IsApplicable)
				try { SpecificStyle.GapBottom.RawValue = SpecificComponent.GapBottom; } catch {}
			if (SpecificStyle.GapHorizontal.IsApplicable)
				try { SpecificStyle.GapHorizontal.RawValue = SpecificComponent.GapHorizontal; } catch {}
			if (SpecificStyle.GapLeft.IsApplicable)
				try { SpecificStyle.GapLeft.RawValue = SpecificComponent.GapLeft; } catch {}
			if (SpecificStyle.GapRight.IsApplicable)
				try { SpecificStyle.GapRight.RawValue = SpecificComponent.GapRight; } catch {}
			if (SpecificStyle.GapTop.IsApplicable)
				try { SpecificStyle.GapTop.RawValue = SpecificComponent.GapTop; } catch {}
			if (SpecificStyle.GapUnit.IsApplicable)
				try { SpecificStyle.GapUnit.RawValue = SpecificComponent.GapUnit; } catch {}
			if (SpecificStyle.GapVertical.IsApplicable)
				try { SpecificStyle.GapVertical.RawValue = SpecificComponent.GapVertical; } catch {}
			if (SpecificStyle.InvertMask.IsApplicable)
				try { SpecificStyle.InvertMask.RawValue = SpecificComponent.InvertMask; } catch {}
			if (SpecificStyle.IsMaskingGraphic.IsApplicable)
				try { SpecificStyle.IsMaskingGraphic.RawValue = SpecificComponent.isMaskingGraphic; } catch {}
			if (SpecificStyle.Maskable.IsApplicable)
				try { SpecificStyle.Maskable.RawValue = SpecificComponent.maskable; } catch {}
			if (SpecificStyle.Material.IsApplicable)
				try { SpecificStyle.Material.RawValue = SpecificComponent.material; } catch {}
			if (SpecificStyle.Padding.IsApplicable)
				try { SpecificStyle.Padding.RawValue = SpecificComponent.Padding; } catch {}
			if (SpecificStyle.PixelsPerUnitMultiplier.IsApplicable)
				try { SpecificStyle.PixelsPerUnitMultiplier.RawValue = SpecificComponent.pixelsPerUnitMultiplier; } catch {}
			if (SpecificStyle.PositionOffset.IsApplicable)
				try { SpecificStyle.PositionOffset.RawValue = SpecificComponent.PositionOffset; } catch {}
			if (SpecificStyle.Radius.IsApplicable)
				try { SpecificStyle.Radius.RawValue = SpecificComponent.Radius; } catch {}
			if (SpecificStyle.SizeOffset.IsApplicable)
				try { SpecificStyle.SizeOffset.RawValue = SpecificComponent.SizeOffset; } catch {}
			if (SpecificStyle.Sprite.IsApplicable)
				try { SpecificStyle.Sprite.RawValue = SpecificComponent.sprite; } catch {}
			if (SpecificStyle.UniformSizeOffset.IsApplicable)
				try { SpecificStyle.UniformSizeOffset.RawValue = SpecificComponent.UniformSizeOffset; } catch {}
			if (SpecificStyle.UseFixedSize.IsApplicable)
				try { SpecificStyle.UseFixedSize.RawValue = SpecificComponent.UseFixedSize; } catch {}
			if (SpecificStyle.UsePadding.IsApplicable)
				try { SpecificStyle.UsePadding.RawValue = SpecificComponent.UsePadding; } catch {}
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
				result.DisabledMaterial.Value = specificTemplate.DisabledMaterial.Value;
				result.DisabledMaterial.IsApplicable = specificTemplate.DisabledMaterial.IsApplicable;
				result.Enabled.Value = specificTemplate.Enabled.Value;
				result.Enabled.IsApplicable = specificTemplate.Enabled.IsApplicable;
				result.EnabledInHierarchy.Value = specificTemplate.EnabledInHierarchy.Value;
				result.EnabledInHierarchy.IsApplicable = specificTemplate.EnabledInHierarchy.IsApplicable;
				result.FadeColor.Value = specificTemplate.FadeColor.Value;
				result.FadeColor.IsApplicable = specificTemplate.FadeColor.IsApplicable;
				result.FadeSize.Value = specificTemplate.FadeSize.Value;
				result.FadeSize.IsApplicable = specificTemplate.FadeSize.IsApplicable;
				result.FillAmount.Value = specificTemplate.FillAmount.Value;
				result.FillAmount.IsApplicable = specificTemplate.FillAmount.IsApplicable;
				result.FillCenter.Value = specificTemplate.FillCenter.Value;
				result.FillCenter.IsApplicable = specificTemplate.FillCenter.IsApplicable;
				result.FillClockwise.Value = specificTemplate.FillClockwise.Value;
				result.FillClockwise.IsApplicable = specificTemplate.FillClockwise.IsApplicable;
				result.FillMethod.Value = specificTemplate.FillMethod.Value;
				result.FillMethod.IsApplicable = specificTemplate.FillMethod.IsApplicable;
				result.FillOrigin.Value = specificTemplate.FillOrigin.Value;
				result.FillOrigin.IsApplicable = specificTemplate.FillOrigin.IsApplicable;
				result.FixedSize.Value = specificTemplate.FixedSize.Value;
				result.FixedSize.IsApplicable = specificTemplate.FixedSize.IsApplicable;
				result.FrameSize.Value = specificTemplate.FrameSize.Value;
				result.FrameSize.IsApplicable = specificTemplate.FrameSize.IsApplicable;
				result.GapBottom.Value = specificTemplate.GapBottom.Value;
				result.GapBottom.IsApplicable = specificTemplate.GapBottom.IsApplicable;
				result.GapHorizontal.Value = specificTemplate.GapHorizontal.Value;
				result.GapHorizontal.IsApplicable = specificTemplate.GapHorizontal.IsApplicable;
				result.GapLeft.Value = specificTemplate.GapLeft.Value;
				result.GapLeft.IsApplicable = specificTemplate.GapLeft.IsApplicable;
				result.GapRight.Value = specificTemplate.GapRight.Value;
				result.GapRight.IsApplicable = specificTemplate.GapRight.IsApplicable;
				result.GapTop.Value = specificTemplate.GapTop.Value;
				result.GapTop.IsApplicable = specificTemplate.GapTop.IsApplicable;
				result.GapUnit.Value = specificTemplate.GapUnit.Value;
				result.GapUnit.IsApplicable = specificTemplate.GapUnit.IsApplicable;
				result.GapVertical.Value = specificTemplate.GapVertical.Value;
				result.GapVertical.IsApplicable = specificTemplate.GapVertical.IsApplicable;
				result.InvertMask.Value = specificTemplate.InvertMask.Value;
				result.InvertMask.IsApplicable = specificTemplate.InvertMask.IsApplicable;
				result.IsMaskingGraphic.Value = specificTemplate.IsMaskingGraphic.Value;
				result.IsMaskingGraphic.IsApplicable = specificTemplate.IsMaskingGraphic.IsApplicable;
				result.Maskable.Value = specificTemplate.Maskable.Value;
				result.Maskable.IsApplicable = specificTemplate.Maskable.IsApplicable;
				result.Material.Value = specificTemplate.Material.Value;
				result.Material.IsApplicable = specificTemplate.Material.IsApplicable;
				result.Padding.Value = specificTemplate.Padding.Value;
				result.Padding.IsApplicable = specificTemplate.Padding.IsApplicable;
				result.PixelsPerUnitMultiplier.Value = specificTemplate.PixelsPerUnitMultiplier.Value;
				result.PixelsPerUnitMultiplier.IsApplicable = specificTemplate.PixelsPerUnitMultiplier.IsApplicable;
				result.PositionOffset.Value = specificTemplate.PositionOffset.Value;
				result.PositionOffset.IsApplicable = specificTemplate.PositionOffset.IsApplicable;
				result.Radius.Value = specificTemplate.Radius.Value;
				result.Radius.IsApplicable = specificTemplate.Radius.IsApplicable;
				result.SizeOffset.Value = specificTemplate.SizeOffset.Value;
				result.SizeOffset.IsApplicable = specificTemplate.SizeOffset.IsApplicable;
				result.Sprite.Value = specificTemplate.Sprite.Value;
				result.Sprite.IsApplicable = specificTemplate.Sprite.IsApplicable;
				result.UniformSizeOffset.Value = specificTemplate.UniformSizeOffset.Value;
				result.UniformSizeOffset.IsApplicable = specificTemplate.UniformSizeOffset.IsApplicable;
				result.UseFixedSize.Value = specificTemplate.UseFixedSize.Value;
				result.UseFixedSize.IsApplicable = specificTemplate.UseFixedSize.IsApplicable;
				result.UsePadding.Value = specificTemplate.UsePadding.Value;
				result.UsePadding.IsApplicable = specificTemplate.UsePadding.IsApplicable;

				return result;
			}

			try { result.Color.Value = SpecificComponent.color; } catch {}
			try { result.CornerSegments.Value = SpecificComponent.CornerSegments; } catch {}
			try { result.DisabledMaterial.Value = SpecificComponent.DisabledMaterial; } catch {}
			try { result.Enabled.Value = SpecificComponent.enabled; } catch {}
			try { result.EnabledInHierarchy.Value = SpecificComponent.EnabledInHierarchy; } catch {}
			try { result.FadeColor.Value = SpecificComponent.FadeColor; } catch {}
			try { result.FadeSize.Value = SpecificComponent.FadeSize; } catch {}
			try { result.FillAmount.Value = SpecificComponent.fillAmount; } catch {}
			try { result.FillCenter.Value = SpecificComponent.fillCenter; } catch {}
			try { result.FillClockwise.Value = SpecificComponent.fillClockwise; } catch {}
			try { result.FillMethod.Value = SpecificComponent.fillMethod; } catch {}
			try { result.FillOrigin.Value = SpecificComponent.fillOrigin; } catch {}
			try { result.FixedSize.Value = SpecificComponent.FixedSize; } catch {}
			try { result.FrameSize.Value = SpecificComponent.FrameSize; } catch {}
			try { result.GapBottom.Value = SpecificComponent.GapBottom; } catch {}
			try { result.GapHorizontal.Value = SpecificComponent.GapHorizontal; } catch {}
			try { result.GapLeft.Value = SpecificComponent.GapLeft; } catch {}
			try { result.GapRight.Value = SpecificComponent.GapRight; } catch {}
			try { result.GapTop.Value = SpecificComponent.GapTop; } catch {}
			try { result.GapUnit.Value = SpecificComponent.GapUnit; } catch {}
			try { result.GapVertical.Value = SpecificComponent.GapVertical; } catch {}
			try { result.InvertMask.Value = SpecificComponent.InvertMask; } catch {}
			try { result.IsMaskingGraphic.Value = SpecificComponent.isMaskingGraphic; } catch {}
			try { result.Maskable.Value = SpecificComponent.maskable; } catch {}
			try { result.Material.Value = SpecificComponent.material; } catch {}
			try { result.Padding.Value = SpecificComponent.Padding; } catch {}
			try { result.PixelsPerUnitMultiplier.Value = SpecificComponent.pixelsPerUnitMultiplier; } catch {}
			try { result.PositionOffset.Value = SpecificComponent.PositionOffset; } catch {}
			try { result.Radius.Value = SpecificComponent.Radius; } catch {}
			try { result.SizeOffset.Value = SpecificComponent.SizeOffset; } catch {}
			try { result.Sprite.Value = SpecificComponent.sprite; } catch {}
			try { result.UniformSizeOffset.Value = SpecificComponent.UniformSizeOffset; } catch {}
			try { result.UseFixedSize.Value = SpecificComponent.UseFixedSize; } catch {}
			try { result.UsePadding.Value = SpecificComponent.UsePadding; } catch {}

			return result;
		}
	}
}
