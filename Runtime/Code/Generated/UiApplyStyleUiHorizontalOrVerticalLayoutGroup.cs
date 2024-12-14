// Auto-generated, please do not change!
using UnityEngine;
using GuiToolkit;
using GuiToolkit.Style;

namespace GuiToolkit.Style
{
	[ExecuteAlways]
	[RequireComponent(typeof(GuiToolkit.UiHorizontalOrVerticalLayoutGroup))]
	public class UiApplyStyleUiHorizontalOrVerticalLayoutGroup : UiAbstractApplyStyle<GuiToolkit.UiHorizontalOrVerticalLayoutGroup, UiStyleUiHorizontalOrVerticalLayoutGroup>
	{
		protected override void ApplyImpl()
		{
			if (!SpecificComponent || SpecificStyle == null)
				return;

			if (SpecificStyle.ChildAlignment.IsApplicable)
				try { SpecificComponent.childAlignment = Tweenable ? SpecificStyle.ChildAlignment.Value : SpecificStyle.ChildAlignment.RawValue; } catch {}
			if (SpecificStyle.ChildControlHeight.IsApplicable)
				try { SpecificComponent.childControlHeight = Tweenable ? SpecificStyle.ChildControlHeight.Value : SpecificStyle.ChildControlHeight.RawValue; } catch {}
			if (SpecificStyle.ChildControlWidth.IsApplicable)
				try { SpecificComponent.childControlWidth = Tweenable ? SpecificStyle.ChildControlWidth.Value : SpecificStyle.ChildControlWidth.RawValue; } catch {}
			if (SpecificStyle.ChildForceExpandHeight.IsApplicable)
				try { SpecificComponent.childForceExpandHeight = Tweenable ? SpecificStyle.ChildForceExpandHeight.Value : SpecificStyle.ChildForceExpandHeight.RawValue; } catch {}
			if (SpecificStyle.ChildForceExpandWidth.IsApplicable)
				try { SpecificComponent.childForceExpandWidth = Tweenable ? SpecificStyle.ChildForceExpandWidth.Value : SpecificStyle.ChildForceExpandWidth.RawValue; } catch {}
			if (SpecificStyle.ChildScaleHeight.IsApplicable)
				try { SpecificComponent.childScaleHeight = Tweenable ? SpecificStyle.ChildScaleHeight.Value : SpecificStyle.ChildScaleHeight.RawValue; } catch {}
			if (SpecificStyle.ChildScaleWidth.IsApplicable)
				try { SpecificComponent.childScaleWidth = Tweenable ? SpecificStyle.ChildScaleWidth.Value : SpecificStyle.ChildScaleWidth.RawValue; } catch {}
			if (SpecificStyle.Enabled.IsApplicable)
				try { SpecificComponent.enabled = Tweenable ? SpecificStyle.Enabled.Value : SpecificStyle.Enabled.RawValue; } catch {}
			if (SpecificStyle.Padding.IsApplicable)
				try { SpecificComponent.padding = Tweenable ? SpecificStyle.Padding.Value : SpecificStyle.Padding.RawValue; } catch {}
			if (SpecificStyle.ReverseArrangement.IsApplicable)
				try { SpecificComponent.reverseArrangement = Tweenable ? SpecificStyle.ReverseArrangement.Value : SpecificStyle.ReverseArrangement.RawValue; } catch {}
			if (SpecificStyle.ReverseOrder.IsApplicable)
				try { SpecificComponent.ReverseOrder = Tweenable ? SpecificStyle.ReverseOrder.Value : SpecificStyle.ReverseOrder.RawValue; } catch {}
			if (SpecificStyle.Spacing.IsApplicable)
				try { SpecificComponent.spacing = Tweenable ? SpecificStyle.Spacing.Value : SpecificStyle.Spacing.RawValue; } catch {}
			if (SpecificStyle.Vertical.IsApplicable)
				try { SpecificComponent.Vertical = Tweenable ? SpecificStyle.Vertical.Value : SpecificStyle.Vertical.RawValue; } catch {}
		}

		protected override void RecordImpl()
		{
			if (!SpecificComponent || SpecificStyle == null)
				return;

			if (SpecificStyle.ChildAlignment.IsApplicable)
				try { SpecificStyle.ChildAlignment.RawValue = SpecificComponent.childAlignment; } catch {}
			if (SpecificStyle.ChildControlHeight.IsApplicable)
				try { SpecificStyle.ChildControlHeight.RawValue = SpecificComponent.childControlHeight; } catch {}
			if (SpecificStyle.ChildControlWidth.IsApplicable)
				try { SpecificStyle.ChildControlWidth.RawValue = SpecificComponent.childControlWidth; } catch {}
			if (SpecificStyle.ChildForceExpandHeight.IsApplicable)
				try { SpecificStyle.ChildForceExpandHeight.RawValue = SpecificComponent.childForceExpandHeight; } catch {}
			if (SpecificStyle.ChildForceExpandWidth.IsApplicable)
				try { SpecificStyle.ChildForceExpandWidth.RawValue = SpecificComponent.childForceExpandWidth; } catch {}
			if (SpecificStyle.ChildScaleHeight.IsApplicable)
				try { SpecificStyle.ChildScaleHeight.RawValue = SpecificComponent.childScaleHeight; } catch {}
			if (SpecificStyle.ChildScaleWidth.IsApplicable)
				try { SpecificStyle.ChildScaleWidth.RawValue = SpecificComponent.childScaleWidth; } catch {}
			if (SpecificStyle.Enabled.IsApplicable)
				try { SpecificStyle.Enabled.RawValue = SpecificComponent.enabled; } catch {}
			if (SpecificStyle.Padding.IsApplicable)
				try { SpecificStyle.Padding.RawValue = SpecificComponent.padding; } catch {}
			if (SpecificStyle.ReverseArrangement.IsApplicable)
				try { SpecificStyle.ReverseArrangement.RawValue = SpecificComponent.reverseArrangement; } catch {}
			if (SpecificStyle.ReverseOrder.IsApplicable)
				try { SpecificStyle.ReverseOrder.RawValue = SpecificComponent.ReverseOrder; } catch {}
			if (SpecificStyle.Spacing.IsApplicable)
				try { SpecificStyle.Spacing.RawValue = SpecificComponent.spacing; } catch {}
			if (SpecificStyle.Vertical.IsApplicable)
				try { SpecificStyle.Vertical.RawValue = SpecificComponent.Vertical; } catch {}
		}

		public override UiAbstractStyleBase CreateStyle(UiStyleConfig _styleConfig, string _name, UiAbstractStyleBase _template = null)
		{
			UiStyleUiHorizontalOrVerticalLayoutGroup result = new UiStyleUiHorizontalOrVerticalLayoutGroup(_styleConfig, _name);

			if (!SpecificComponent)
				return result;

			if (_template != null)
			{
				var specificTemplate = (UiStyleUiHorizontalOrVerticalLayoutGroup) _template;

				result.ChildAlignment.Value = specificTemplate.ChildAlignment.Value;
				result.ChildAlignment.IsApplicable = specificTemplate.ChildAlignment.IsApplicable;
				result.ChildControlHeight.Value = specificTemplate.ChildControlHeight.Value;
				result.ChildControlHeight.IsApplicable = specificTemplate.ChildControlHeight.IsApplicable;
				result.ChildControlWidth.Value = specificTemplate.ChildControlWidth.Value;
				result.ChildControlWidth.IsApplicable = specificTemplate.ChildControlWidth.IsApplicable;
				result.ChildForceExpandHeight.Value = specificTemplate.ChildForceExpandHeight.Value;
				result.ChildForceExpandHeight.IsApplicable = specificTemplate.ChildForceExpandHeight.IsApplicable;
				result.ChildForceExpandWidth.Value = specificTemplate.ChildForceExpandWidth.Value;
				result.ChildForceExpandWidth.IsApplicable = specificTemplate.ChildForceExpandWidth.IsApplicable;
				result.ChildScaleHeight.Value = specificTemplate.ChildScaleHeight.Value;
				result.ChildScaleHeight.IsApplicable = specificTemplate.ChildScaleHeight.IsApplicable;
				result.ChildScaleWidth.Value = specificTemplate.ChildScaleWidth.Value;
				result.ChildScaleWidth.IsApplicable = specificTemplate.ChildScaleWidth.IsApplicable;
				result.Enabled.Value = specificTemplate.Enabled.Value;
				result.Enabled.IsApplicable = specificTemplate.Enabled.IsApplicable;
				result.Padding.Value = specificTemplate.Padding.Value;
				result.Padding.IsApplicable = specificTemplate.Padding.IsApplicable;
				result.ReverseArrangement.Value = specificTemplate.ReverseArrangement.Value;
				result.ReverseArrangement.IsApplicable = specificTemplate.ReverseArrangement.IsApplicable;
				result.ReverseOrder.Value = specificTemplate.ReverseOrder.Value;
				result.ReverseOrder.IsApplicable = specificTemplate.ReverseOrder.IsApplicable;
				result.Spacing.Value = specificTemplate.Spacing.Value;
				result.Spacing.IsApplicable = specificTemplate.Spacing.IsApplicable;
				result.Vertical.Value = specificTemplate.Vertical.Value;
				result.Vertical.IsApplicable = specificTemplate.Vertical.IsApplicable;

				return result;
			}

			try { result.ChildAlignment.Value = SpecificComponent.childAlignment; } catch {}
			try { result.ChildControlHeight.Value = SpecificComponent.childControlHeight; } catch {}
			try { result.ChildControlWidth.Value = SpecificComponent.childControlWidth; } catch {}
			try { result.ChildForceExpandHeight.Value = SpecificComponent.childForceExpandHeight; } catch {}
			try { result.ChildForceExpandWidth.Value = SpecificComponent.childForceExpandWidth; } catch {}
			try { result.ChildScaleHeight.Value = SpecificComponent.childScaleHeight; } catch {}
			try { result.ChildScaleWidth.Value = SpecificComponent.childScaleWidth; } catch {}
			try { result.Enabled.Value = SpecificComponent.enabled; } catch {}
			try { result.Padding.Value = SpecificComponent.padding; } catch {}
			try { result.ReverseArrangement.Value = SpecificComponent.reverseArrangement; } catch {}
			try { result.ReverseOrder.Value = SpecificComponent.ReverseOrder; } catch {}
			try { result.Spacing.Value = SpecificComponent.spacing; } catch {}
			try { result.Vertical.Value = SpecificComponent.Vertical; } catch {}

			return result;
		}
	}
}
