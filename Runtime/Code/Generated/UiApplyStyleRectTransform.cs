// Auto-generated, please do not change!
using UnityEngine;
using GuiToolkit;
using GuiToolkit.Style;

namespace GuiToolkit.Style
{
	[ExecuteAlways]
	[RequireComponent(typeof(UnityEngine.RectTransform))]
	public class UiApplyStyleRectTransform : UiAbstractApplyStyle<UnityEngine.RectTransform, UiStyleRectTransform>
	{
		protected override void ApplyImpl()
		{
			if (!SpecificComponent || SpecificStyle == null)
				return;

			if (SpecificStyle.AnchoredPosition.IsApplicable)
				try { SpecificComponent.anchoredPosition = Tweenable ? SpecificStyle.AnchoredPosition.Value : SpecificStyle.AnchoredPosition.RawValue; } catch {}
			if (SpecificStyle.AnchorMax.IsApplicable)
				try { SpecificComponent.anchorMax = Tweenable ? SpecificStyle.AnchorMax.Value : SpecificStyle.AnchorMax.RawValue; } catch {}
			if (SpecificStyle.AnchorMin.IsApplicable)
				try { SpecificComponent.anchorMin = Tweenable ? SpecificStyle.AnchorMin.Value : SpecificStyle.AnchorMin.RawValue; } catch {}
			if (SpecificStyle.LocalEulerAngles.IsApplicable)
				try { SpecificComponent.localEulerAngles = Tweenable ? SpecificStyle.LocalEulerAngles.Value : SpecificStyle.LocalEulerAngles.RawValue; } catch {}
			if (SpecificStyle.LocalPosition.IsApplicable)
				try { SpecificComponent.localPosition = Tweenable ? SpecificStyle.LocalPosition.Value : SpecificStyle.LocalPosition.RawValue; } catch {}
			if (SpecificStyle.LocalRotation.IsApplicable)
				try { SpecificComponent.localRotation = Tweenable ? SpecificStyle.LocalRotation.Value : SpecificStyle.LocalRotation.RawValue; } catch {}
			if (SpecificStyle.LocalScale.IsApplicable)
				try { SpecificComponent.localScale = Tweenable ? SpecificStyle.LocalScale.Value : SpecificStyle.LocalScale.RawValue; } catch {}
			if (SpecificStyle.OffsetMax.IsApplicable)
				try { SpecificComponent.offsetMax = Tweenable ? SpecificStyle.OffsetMax.Value : SpecificStyle.OffsetMax.RawValue; } catch {}
			if (SpecificStyle.OffsetMin.IsApplicable)
				try { SpecificComponent.offsetMin = Tweenable ? SpecificStyle.OffsetMin.Value : SpecificStyle.OffsetMin.RawValue; } catch {}
			if (SpecificStyle.Pivot.IsApplicable)
				try { SpecificComponent.pivot = Tweenable ? SpecificStyle.Pivot.Value : SpecificStyle.Pivot.RawValue; } catch {}
			if (SpecificStyle.SizeDelta.IsApplicable)
				try { SpecificComponent.sizeDelta = Tweenable ? SpecificStyle.SizeDelta.Value : SpecificStyle.SizeDelta.RawValue; } catch {}
		}

		protected override void RecordImpl()
		{
			if (!SpecificComponent || SpecificStyle == null)
				return;

			if (SpecificStyle.AnchoredPosition.IsApplicable)
				try { SpecificStyle.AnchoredPosition.RawValue = SpecificComponent.anchoredPosition; } catch {}
			if (SpecificStyle.AnchorMax.IsApplicable)
				try { SpecificStyle.AnchorMax.RawValue = SpecificComponent.anchorMax; } catch {}
			if (SpecificStyle.AnchorMin.IsApplicable)
				try { SpecificStyle.AnchorMin.RawValue = SpecificComponent.anchorMin; } catch {}
			if (SpecificStyle.LocalEulerAngles.IsApplicable)
				try { SpecificStyle.LocalEulerAngles.RawValue = SpecificComponent.localEulerAngles; } catch {}
			if (SpecificStyle.LocalPosition.IsApplicable)
				try { SpecificStyle.LocalPosition.RawValue = SpecificComponent.localPosition; } catch {}
			if (SpecificStyle.LocalRotation.IsApplicable)
				try { SpecificStyle.LocalRotation.RawValue = SpecificComponent.localRotation; } catch {}
			if (SpecificStyle.LocalScale.IsApplicable)
				try { SpecificStyle.LocalScale.RawValue = SpecificComponent.localScale; } catch {}
			if (SpecificStyle.OffsetMax.IsApplicable)
				try { SpecificStyle.OffsetMax.RawValue = SpecificComponent.offsetMax; } catch {}
			if (SpecificStyle.OffsetMin.IsApplicable)
				try { SpecificStyle.OffsetMin.RawValue = SpecificComponent.offsetMin; } catch {}
			if (SpecificStyle.Pivot.IsApplicable)
				try { SpecificStyle.Pivot.RawValue = SpecificComponent.pivot; } catch {}
			if (SpecificStyle.SizeDelta.IsApplicable)
				try { SpecificStyle.SizeDelta.RawValue = SpecificComponent.sizeDelta; } catch {}
		}

		public override UiAbstractStyleBase CreateStyle(UiStyleConfig _styleConfig, string _name, UiAbstractStyleBase _template = null)
		{
			UiStyleRectTransform result = new UiStyleRectTransform(_styleConfig, _name);

			if (!SpecificComponent)
				return result;

			if (_template != null)
			{
				var specificTemplate = (UiStyleRectTransform) _template;

				result.AnchoredPosition.Value = specificTemplate.AnchoredPosition.Value;
				result.AnchoredPosition.IsApplicable = specificTemplate.AnchoredPosition.IsApplicable;
				result.AnchorMax.Value = specificTemplate.AnchorMax.Value;
				result.AnchorMax.IsApplicable = specificTemplate.AnchorMax.IsApplicable;
				result.AnchorMin.Value = specificTemplate.AnchorMin.Value;
				result.AnchorMin.IsApplicable = specificTemplate.AnchorMin.IsApplicable;
				result.LocalEulerAngles.Value = specificTemplate.LocalEulerAngles.Value;
				result.LocalEulerAngles.IsApplicable = specificTemplate.LocalEulerAngles.IsApplicable;
				result.LocalPosition.Value = specificTemplate.LocalPosition.Value;
				result.LocalPosition.IsApplicable = specificTemplate.LocalPosition.IsApplicable;
				result.LocalRotation.Value = specificTemplate.LocalRotation.Value;
				result.LocalRotation.IsApplicable = specificTemplate.LocalRotation.IsApplicable;
				result.LocalScale.Value = specificTemplate.LocalScale.Value;
				result.LocalScale.IsApplicable = specificTemplate.LocalScale.IsApplicable;
				result.OffsetMax.Value = specificTemplate.OffsetMax.Value;
				result.OffsetMax.IsApplicable = specificTemplate.OffsetMax.IsApplicable;
				result.OffsetMin.Value = specificTemplate.OffsetMin.Value;
				result.OffsetMin.IsApplicable = specificTemplate.OffsetMin.IsApplicable;
				result.Pivot.Value = specificTemplate.Pivot.Value;
				result.Pivot.IsApplicable = specificTemplate.Pivot.IsApplicable;
				result.SizeDelta.Value = specificTemplate.SizeDelta.Value;
				result.SizeDelta.IsApplicable = specificTemplate.SizeDelta.IsApplicable;

				return result;
			}

			try { result.AnchoredPosition.Value = SpecificComponent.anchoredPosition; } catch {}
			try { result.AnchorMax.Value = SpecificComponent.anchorMax; } catch {}
			try { result.AnchorMin.Value = SpecificComponent.anchorMin; } catch {}
			try { result.LocalEulerAngles.Value = SpecificComponent.localEulerAngles; } catch {}
			try { result.LocalPosition.Value = SpecificComponent.localPosition; } catch {}
			try { result.LocalRotation.Value = SpecificComponent.localRotation; } catch {}
			try { result.LocalScale.Value = SpecificComponent.localScale; } catch {}
			try { result.OffsetMax.Value = SpecificComponent.offsetMax; } catch {}
			try { result.OffsetMin.Value = SpecificComponent.offsetMin; } catch {}
			try { result.Pivot.Value = SpecificComponent.pivot; } catch {}
			try { result.SizeDelta.Value = SpecificComponent.sizeDelta; } catch {}

			return result;
		}
	}
}
