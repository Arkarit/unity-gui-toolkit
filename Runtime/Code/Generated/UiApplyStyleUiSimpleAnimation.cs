// Auto-generated, please do not change!
using UnityEngine;
using GuiToolkit;
using GuiToolkit.Style;

namespace GuiToolkit.Style
{
	[ExecuteAlways]
	[RequireComponent(typeof(GuiToolkit.UiSimpleAnimation))]
	public class UiApplyStyleUiSimpleAnimation : UiAbstractApplyStyle<GuiToolkit.UiSimpleAnimation, UiStyleUiSimpleAnimation>
	{
		protected override void ApplyImpl()
		{
			if (!SpecificComponent || SpecificStyle == null)
				return;

			if (SpecificStyle.AlphaCanvasGroup.IsApplicable)
				try { SpecificComponent.AlphaCanvasGroup = Tweenable ? SpecificStyle.AlphaCanvasGroup.Value : SpecificStyle.AlphaCanvasGroup.RawValue; } catch {}
			if (SpecificStyle.AlphaCurve.IsApplicable)
				try { SpecificComponent.AlphaCurve = Tweenable ? SpecificStyle.AlphaCurve.Value : SpecificStyle.AlphaCurve.RawValue; } catch {}
			if (SpecificStyle.AlphaGraphic.IsApplicable)
				try { SpecificComponent.AlphaGraphic = Tweenable ? SpecificStyle.AlphaGraphic.Value : SpecificStyle.AlphaGraphic.RawValue; } catch {}
			if (SpecificStyle.BackwardsAnimation.IsApplicable)
				try { SpecificComponent.BackwardsAnimation = Tweenable ? SpecificStyle.BackwardsAnimation.Value : SpecificStyle.BackwardsAnimation.RawValue; } catch {}
			if (SpecificStyle.CanvasRectTransform.IsApplicable)
				try { SpecificComponent.CanvasRectTransform = Tweenable ? SpecificStyle.CanvasRectTransform.Value : SpecificStyle.CanvasRectTransform.RawValue; } catch {}
			if (SpecificStyle.CanvasScaler.IsApplicable)
				try { SpecificComponent.CanvasScaler = Tweenable ? SpecificStyle.CanvasScaler.Value : SpecificStyle.CanvasScaler.RawValue; } catch {}
			if (SpecificStyle.Delay.IsApplicable)
				try { SpecificComponent.Delay = Tweenable ? SpecificStyle.Delay.Value : SpecificStyle.Delay.RawValue; } catch {}
			if (SpecificStyle.Duration.IsApplicable)
				try { SpecificComponent.Duration = Tweenable ? SpecificStyle.Duration.Value : SpecificStyle.Duration.RawValue; } catch {}
			if (SpecificStyle.Enabled.IsApplicable)
				try { SpecificComponent.enabled = Tweenable ? SpecificStyle.Enabled.Value : SpecificStyle.Enabled.RawValue; } catch {}
			if (SpecificStyle.EnabledInHierarchy.IsApplicable)
				try { SpecificComponent.EnabledInHierarchy = Tweenable ? SpecificStyle.EnabledInHierarchy.Value : SpecificStyle.EnabledInHierarchy.RawValue; } catch {}
			if (SpecificStyle.MarkTargetForLayoutRebuild.IsApplicable)
				try { SpecificComponent.MarkTargetForLayoutRebuild = Tweenable ? SpecificStyle.MarkTargetForLayoutRebuild.Value : SpecificStyle.MarkTargetForLayoutRebuild.RawValue; } catch {}
			if (SpecificStyle.PosXCurve.IsApplicable)
				try { SpecificComponent.PosXCurve = Tweenable ? SpecificStyle.PosXCurve.Value : SpecificStyle.PosXCurve.RawValue; } catch {}
			if (SpecificStyle.PosXEnd.IsApplicable)
				try { SpecificComponent.PosXEnd = Tweenable ? SpecificStyle.PosXEnd.Value : SpecificStyle.PosXEnd.RawValue; } catch {}
			if (SpecificStyle.PosXStart.IsApplicable)
				try { SpecificComponent.PosXStart = Tweenable ? SpecificStyle.PosXStart.Value : SpecificStyle.PosXStart.RawValue; } catch {}
			if (SpecificStyle.PosYCurve.IsApplicable)
				try { SpecificComponent.PosYCurve = Tweenable ? SpecificStyle.PosYCurve.Value : SpecificStyle.PosYCurve.RawValue; } catch {}
			if (SpecificStyle.PosYEnd.IsApplicable)
				try { SpecificComponent.PosYEnd = Tweenable ? SpecificStyle.PosYEnd.Value : SpecificStyle.PosYEnd.RawValue; } catch {}
			if (SpecificStyle.PosYStart.IsApplicable)
				try { SpecificComponent.PosYStart = Tweenable ? SpecificStyle.PosYStart.Value : SpecificStyle.PosYStart.RawValue; } catch {}
			if (SpecificStyle.RotZCurve.IsApplicable)
				try { SpecificComponent.RotZCurve = Tweenable ? SpecificStyle.RotZCurve.Value : SpecificStyle.RotZCurve.RawValue; } catch {}
			if (SpecificStyle.RotZEnd.IsApplicable)
				try { SpecificComponent.RotZEnd = Tweenable ? SpecificStyle.RotZEnd.Value : SpecificStyle.RotZEnd.RawValue; } catch {}
			if (SpecificStyle.RotZStart.IsApplicable)
				try { SpecificComponent.RotZStart = Tweenable ? SpecificStyle.RotZStart.Value : SpecificStyle.RotZStart.RawValue; } catch {}
			if (SpecificStyle.ScaleByCanvasScaler.IsApplicable)
				try { SpecificComponent.ScaleByCanvasScaler = Tweenable ? SpecificStyle.ScaleByCanvasScaler.Value : SpecificStyle.ScaleByCanvasScaler.RawValue; } catch {}
			if (SpecificStyle.ScaleLocked.IsApplicable)
				try { SpecificComponent.ScaleLocked = Tweenable ? SpecificStyle.ScaleLocked.Value : SpecificStyle.ScaleLocked.RawValue; } catch {}
			if (SpecificStyle.ScaleXCurve.IsApplicable)
				try { SpecificComponent.ScaleXCurve = Tweenable ? SpecificStyle.ScaleXCurve.Value : SpecificStyle.ScaleXCurve.RawValue; } catch {}
			if (SpecificStyle.ScaleXEnd.IsApplicable)
				try { SpecificComponent.ScaleXEnd = Tweenable ? SpecificStyle.ScaleXEnd.Value : SpecificStyle.ScaleXEnd.RawValue; } catch {}
			if (SpecificStyle.ScaleXStart.IsApplicable)
				try { SpecificComponent.ScaleXStart = Tweenable ? SpecificStyle.ScaleXStart.Value : SpecificStyle.ScaleXStart.RawValue; } catch {}
			if (SpecificStyle.ScaleYCurve.IsApplicable)
				try { SpecificComponent.ScaleYCurve = Tweenable ? SpecificStyle.ScaleYCurve.Value : SpecificStyle.ScaleYCurve.RawValue; } catch {}
			if (SpecificStyle.ScaleYEnd.IsApplicable)
				try { SpecificComponent.ScaleYEnd = Tweenable ? SpecificStyle.ScaleYEnd.Value : SpecificStyle.ScaleYEnd.RawValue; } catch {}
			if (SpecificStyle.ScaleYStart.IsApplicable)
				try { SpecificComponent.ScaleYStart = Tweenable ? SpecificStyle.ScaleYStart.Value : SpecificStyle.ScaleYStart.RawValue; } catch {}
			if (SpecificStyle.Support.IsApplicable)
				try { SpecificComponent.Support = Tweenable ? SpecificStyle.Support.Value : SpecificStyle.Support.RawValue; } catch {}
			if (SpecificStyle.Target.IsApplicable)
				try { SpecificComponent.Target = Tweenable ? SpecificStyle.Target.Value : SpecificStyle.Target.RawValue; } catch {}
		}

		protected override void RecordImpl()
		{
			if (!SpecificComponent || SpecificStyle == null)
				return;

			if (SpecificStyle.AlphaCanvasGroup.IsApplicable)
				try { SpecificStyle.AlphaCanvasGroup.RawValue = SpecificComponent.AlphaCanvasGroup; } catch {}
			if (SpecificStyle.AlphaCurve.IsApplicable)
				try { SpecificStyle.AlphaCurve.RawValue = SpecificComponent.AlphaCurve; } catch {}
			if (SpecificStyle.AlphaGraphic.IsApplicable)
				try { SpecificStyle.AlphaGraphic.RawValue = SpecificComponent.AlphaGraphic; } catch {}
			if (SpecificStyle.BackwardsAnimation.IsApplicable)
				try { SpecificStyle.BackwardsAnimation.RawValue = SpecificComponent.BackwardsAnimation; } catch {}
			if (SpecificStyle.CanvasRectTransform.IsApplicable)
				try { SpecificStyle.CanvasRectTransform.RawValue = SpecificComponent.CanvasRectTransform; } catch {}
			if (SpecificStyle.CanvasScaler.IsApplicable)
				try { SpecificStyle.CanvasScaler.RawValue = SpecificComponent.CanvasScaler; } catch {}
			if (SpecificStyle.Delay.IsApplicable)
				try { SpecificStyle.Delay.RawValue = SpecificComponent.Delay; } catch {}
			if (SpecificStyle.Duration.IsApplicable)
				try { SpecificStyle.Duration.RawValue = SpecificComponent.Duration; } catch {}
			if (SpecificStyle.Enabled.IsApplicable)
				try { SpecificStyle.Enabled.RawValue = SpecificComponent.enabled; } catch {}
			if (SpecificStyle.EnabledInHierarchy.IsApplicable)
				try { SpecificStyle.EnabledInHierarchy.RawValue = SpecificComponent.EnabledInHierarchy; } catch {}
			if (SpecificStyle.MarkTargetForLayoutRebuild.IsApplicable)
				try { SpecificStyle.MarkTargetForLayoutRebuild.RawValue = SpecificComponent.MarkTargetForLayoutRebuild; } catch {}
			if (SpecificStyle.PosXCurve.IsApplicable)
				try { SpecificStyle.PosXCurve.RawValue = SpecificComponent.PosXCurve; } catch {}
			if (SpecificStyle.PosXEnd.IsApplicable)
				try { SpecificStyle.PosXEnd.RawValue = SpecificComponent.PosXEnd; } catch {}
			if (SpecificStyle.PosXStart.IsApplicable)
				try { SpecificStyle.PosXStart.RawValue = SpecificComponent.PosXStart; } catch {}
			if (SpecificStyle.PosYCurve.IsApplicable)
				try { SpecificStyle.PosYCurve.RawValue = SpecificComponent.PosYCurve; } catch {}
			if (SpecificStyle.PosYEnd.IsApplicable)
				try { SpecificStyle.PosYEnd.RawValue = SpecificComponent.PosYEnd; } catch {}
			if (SpecificStyle.PosYStart.IsApplicable)
				try { SpecificStyle.PosYStart.RawValue = SpecificComponent.PosYStart; } catch {}
			if (SpecificStyle.RotZCurve.IsApplicable)
				try { SpecificStyle.RotZCurve.RawValue = SpecificComponent.RotZCurve; } catch {}
			if (SpecificStyle.RotZEnd.IsApplicable)
				try { SpecificStyle.RotZEnd.RawValue = SpecificComponent.RotZEnd; } catch {}
			if (SpecificStyle.RotZStart.IsApplicable)
				try { SpecificStyle.RotZStart.RawValue = SpecificComponent.RotZStart; } catch {}
			if (SpecificStyle.ScaleByCanvasScaler.IsApplicable)
				try { SpecificStyle.ScaleByCanvasScaler.RawValue = SpecificComponent.ScaleByCanvasScaler; } catch {}
			if (SpecificStyle.ScaleLocked.IsApplicable)
				try { SpecificStyle.ScaleLocked.RawValue = SpecificComponent.ScaleLocked; } catch {}
			if (SpecificStyle.ScaleXCurve.IsApplicable)
				try { SpecificStyle.ScaleXCurve.RawValue = SpecificComponent.ScaleXCurve; } catch {}
			if (SpecificStyle.ScaleXEnd.IsApplicable)
				try { SpecificStyle.ScaleXEnd.RawValue = SpecificComponent.ScaleXEnd; } catch {}
			if (SpecificStyle.ScaleXStart.IsApplicable)
				try { SpecificStyle.ScaleXStart.RawValue = SpecificComponent.ScaleXStart; } catch {}
			if (SpecificStyle.ScaleYCurve.IsApplicable)
				try { SpecificStyle.ScaleYCurve.RawValue = SpecificComponent.ScaleYCurve; } catch {}
			if (SpecificStyle.ScaleYEnd.IsApplicable)
				try { SpecificStyle.ScaleYEnd.RawValue = SpecificComponent.ScaleYEnd; } catch {}
			if (SpecificStyle.ScaleYStart.IsApplicable)
				try { SpecificStyle.ScaleYStart.RawValue = SpecificComponent.ScaleYStart; } catch {}
			if (SpecificStyle.Support.IsApplicable)
				try { SpecificStyle.Support.RawValue = SpecificComponent.Support; } catch {}
			if (SpecificStyle.Target.IsApplicable)
				try { SpecificStyle.Target.RawValue = SpecificComponent.Target; } catch {}
		}

		public override UiAbstractStyleBase CreateStyle(UiStyleConfig _styleConfig, string _name, UiAbstractStyleBase _template = null)
		{
			UiStyleUiSimpleAnimation result = new UiStyleUiSimpleAnimation(_styleConfig, _name);

			if (!SpecificComponent)
				return result;

			if (_template != null)
			{
				var specificTemplate = (UiStyleUiSimpleAnimation) _template;

				result.AlphaCanvasGroup.Value = specificTemplate.AlphaCanvasGroup.Value;
				result.AlphaCanvasGroup.IsApplicable = specificTemplate.AlphaCanvasGroup.IsApplicable;
				result.AlphaCurve.Value = specificTemplate.AlphaCurve.Value;
				result.AlphaCurve.IsApplicable = specificTemplate.AlphaCurve.IsApplicable;
				result.AlphaGraphic.Value = specificTemplate.AlphaGraphic.Value;
				result.AlphaGraphic.IsApplicable = specificTemplate.AlphaGraphic.IsApplicable;
				result.BackwardsAnimation.Value = specificTemplate.BackwardsAnimation.Value;
				result.BackwardsAnimation.IsApplicable = specificTemplate.BackwardsAnimation.IsApplicable;
				result.CanvasRectTransform.Value = specificTemplate.CanvasRectTransform.Value;
				result.CanvasRectTransform.IsApplicable = specificTemplate.CanvasRectTransform.IsApplicable;
				result.CanvasScaler.Value = specificTemplate.CanvasScaler.Value;
				result.CanvasScaler.IsApplicable = specificTemplate.CanvasScaler.IsApplicable;
				result.Delay.Value = specificTemplate.Delay.Value;
				result.Delay.IsApplicable = specificTemplate.Delay.IsApplicable;
				result.Duration.Value = specificTemplate.Duration.Value;
				result.Duration.IsApplicable = specificTemplate.Duration.IsApplicable;
				result.Enabled.Value = specificTemplate.Enabled.Value;
				result.Enabled.IsApplicable = specificTemplate.Enabled.IsApplicable;
				result.EnabledInHierarchy.Value = specificTemplate.EnabledInHierarchy.Value;
				result.EnabledInHierarchy.IsApplicable = specificTemplate.EnabledInHierarchy.IsApplicable;
				result.MarkTargetForLayoutRebuild.Value = specificTemplate.MarkTargetForLayoutRebuild.Value;
				result.MarkTargetForLayoutRebuild.IsApplicable = specificTemplate.MarkTargetForLayoutRebuild.IsApplicable;
				result.PosXCurve.Value = specificTemplate.PosXCurve.Value;
				result.PosXCurve.IsApplicable = specificTemplate.PosXCurve.IsApplicable;
				result.PosXEnd.Value = specificTemplate.PosXEnd.Value;
				result.PosXEnd.IsApplicable = specificTemplate.PosXEnd.IsApplicable;
				result.PosXStart.Value = specificTemplate.PosXStart.Value;
				result.PosXStart.IsApplicable = specificTemplate.PosXStart.IsApplicable;
				result.PosYCurve.Value = specificTemplate.PosYCurve.Value;
				result.PosYCurve.IsApplicable = specificTemplate.PosYCurve.IsApplicable;
				result.PosYEnd.Value = specificTemplate.PosYEnd.Value;
				result.PosYEnd.IsApplicable = specificTemplate.PosYEnd.IsApplicable;
				result.PosYStart.Value = specificTemplate.PosYStart.Value;
				result.PosYStart.IsApplicable = specificTemplate.PosYStart.IsApplicable;
				result.RotZCurve.Value = specificTemplate.RotZCurve.Value;
				result.RotZCurve.IsApplicable = specificTemplate.RotZCurve.IsApplicable;
				result.RotZEnd.Value = specificTemplate.RotZEnd.Value;
				result.RotZEnd.IsApplicable = specificTemplate.RotZEnd.IsApplicable;
				result.RotZStart.Value = specificTemplate.RotZStart.Value;
				result.RotZStart.IsApplicable = specificTemplate.RotZStart.IsApplicable;
				result.ScaleByCanvasScaler.Value = specificTemplate.ScaleByCanvasScaler.Value;
				result.ScaleByCanvasScaler.IsApplicable = specificTemplate.ScaleByCanvasScaler.IsApplicable;
				result.ScaleLocked.Value = specificTemplate.ScaleLocked.Value;
				result.ScaleLocked.IsApplicable = specificTemplate.ScaleLocked.IsApplicable;
				result.ScaleXCurve.Value = specificTemplate.ScaleXCurve.Value;
				result.ScaleXCurve.IsApplicable = specificTemplate.ScaleXCurve.IsApplicable;
				result.ScaleXEnd.Value = specificTemplate.ScaleXEnd.Value;
				result.ScaleXEnd.IsApplicable = specificTemplate.ScaleXEnd.IsApplicable;
				result.ScaleXStart.Value = specificTemplate.ScaleXStart.Value;
				result.ScaleXStart.IsApplicable = specificTemplate.ScaleXStart.IsApplicable;
				result.ScaleYCurve.Value = specificTemplate.ScaleYCurve.Value;
				result.ScaleYCurve.IsApplicable = specificTemplate.ScaleYCurve.IsApplicable;
				result.ScaleYEnd.Value = specificTemplate.ScaleYEnd.Value;
				result.ScaleYEnd.IsApplicable = specificTemplate.ScaleYEnd.IsApplicable;
				result.ScaleYStart.Value = specificTemplate.ScaleYStart.Value;
				result.ScaleYStart.IsApplicable = specificTemplate.ScaleYStart.IsApplicable;
				result.Support.Value = specificTemplate.Support.Value;
				result.Support.IsApplicable = specificTemplate.Support.IsApplicable;
				result.Target.Value = specificTemplate.Target.Value;
				result.Target.IsApplicable = specificTemplate.Target.IsApplicable;

				return result;
			}

			try { result.AlphaCanvasGroup.Value = SpecificComponent.AlphaCanvasGroup; } catch {}
			try { result.AlphaCurve.Value = SpecificComponent.AlphaCurve; } catch {}
			try { result.AlphaGraphic.Value = SpecificComponent.AlphaGraphic; } catch {}
			try { result.BackwardsAnimation.Value = SpecificComponent.BackwardsAnimation; } catch {}
			try { result.CanvasRectTransform.Value = SpecificComponent.CanvasRectTransform; } catch {}
			try { result.CanvasScaler.Value = SpecificComponent.CanvasScaler; } catch {}
			try { result.Delay.Value = SpecificComponent.Delay; } catch {}
			try { result.Duration.Value = SpecificComponent.Duration; } catch {}
			try { result.Enabled.Value = SpecificComponent.enabled; } catch {}
			try { result.EnabledInHierarchy.Value = SpecificComponent.EnabledInHierarchy; } catch {}
			try { result.MarkTargetForLayoutRebuild.Value = SpecificComponent.MarkTargetForLayoutRebuild; } catch {}
			try { result.PosXCurve.Value = SpecificComponent.PosXCurve; } catch {}
			try { result.PosXEnd.Value = SpecificComponent.PosXEnd; } catch {}
			try { result.PosXStart.Value = SpecificComponent.PosXStart; } catch {}
			try { result.PosYCurve.Value = SpecificComponent.PosYCurve; } catch {}
			try { result.PosYEnd.Value = SpecificComponent.PosYEnd; } catch {}
			try { result.PosYStart.Value = SpecificComponent.PosYStart; } catch {}
			try { result.RotZCurve.Value = SpecificComponent.RotZCurve; } catch {}
			try { result.RotZEnd.Value = SpecificComponent.RotZEnd; } catch {}
			try { result.RotZStart.Value = SpecificComponent.RotZStart; } catch {}
			try { result.ScaleByCanvasScaler.Value = SpecificComponent.ScaleByCanvasScaler; } catch {}
			try { result.ScaleLocked.Value = SpecificComponent.ScaleLocked; } catch {}
			try { result.ScaleXCurve.Value = SpecificComponent.ScaleXCurve; } catch {}
			try { result.ScaleXEnd.Value = SpecificComponent.ScaleXEnd; } catch {}
			try { result.ScaleXStart.Value = SpecificComponent.ScaleXStart; } catch {}
			try { result.ScaleYCurve.Value = SpecificComponent.ScaleYCurve; } catch {}
			try { result.ScaleYEnd.Value = SpecificComponent.ScaleYEnd; } catch {}
			try { result.ScaleYStart.Value = SpecificComponent.ScaleYStart; } catch {}
			try { result.Support.Value = SpecificComponent.Support; } catch {}
			try { result.Target.Value = SpecificComponent.Target; } catch {}

			return result;
		}
	}
}
