// Auto-generated, please do not change!
using UnityEngine;
using GuiToolkit;
using GuiToolkit.Style;

namespace GuiToolkit.Style
{
	[ExecuteAlways]
	[RequireComponent(typeof(UnityEngine.UI.LayoutElement))]
	public class UiApplyStyleLayoutElement : UiAbstractApplyStyle<UnityEngine.UI.LayoutElement, UiStyleLayoutElement>
	{
		protected override void ApplyImpl()
		{
			if (!SpecificComponent || SpecificStyle == null)
				return;

			if (SpecificStyle.Enabled.IsApplicable)
				try { SpecificComponent.enabled = Tweenable ? SpecificStyle.Enabled.Value : SpecificStyle.Enabled.RawValue; } catch {}
			if (SpecificStyle.FlexibleHeight.IsApplicable)
				try { SpecificComponent.flexibleHeight = Tweenable ? SpecificStyle.FlexibleHeight.Value : SpecificStyle.FlexibleHeight.RawValue; } catch {}
			if (SpecificStyle.FlexibleWidth.IsApplicable)
				try { SpecificComponent.flexibleWidth = Tweenable ? SpecificStyle.FlexibleWidth.Value : SpecificStyle.FlexibleWidth.RawValue; } catch {}
			if (SpecificStyle.IgnoreLayout.IsApplicable)
				try { SpecificComponent.ignoreLayout = Tweenable ? SpecificStyle.IgnoreLayout.Value : SpecificStyle.IgnoreLayout.RawValue; } catch {}
			if (SpecificStyle.LayoutPriority.IsApplicable)
				try { SpecificComponent.layoutPriority = Tweenable ? SpecificStyle.LayoutPriority.Value : SpecificStyle.LayoutPriority.RawValue; } catch {}
			if (SpecificStyle.MinHeight.IsApplicable)
				try { SpecificComponent.minHeight = Tweenable ? SpecificStyle.MinHeight.Value : SpecificStyle.MinHeight.RawValue; } catch {}
			if (SpecificStyle.MinWidth.IsApplicable)
				try { SpecificComponent.minWidth = Tweenable ? SpecificStyle.MinWidth.Value : SpecificStyle.MinWidth.RawValue; } catch {}
			if (SpecificStyle.PreferredHeight.IsApplicable)
				try { SpecificComponent.preferredHeight = Tweenable ? SpecificStyle.PreferredHeight.Value : SpecificStyle.PreferredHeight.RawValue; } catch {}
			if (SpecificStyle.PreferredWidth.IsApplicable)
				try { SpecificComponent.preferredWidth = Tweenable ? SpecificStyle.PreferredWidth.Value : SpecificStyle.PreferredWidth.RawValue; } catch {}
		}

		protected override void RecordImpl()
		{
			if (!SpecificComponent || SpecificStyle == null)
				return;

			if (SpecificStyle.Enabled.IsApplicable)
				try { SpecificStyle.Enabled.RawValue = SpecificComponent.enabled; } catch {}
			if (SpecificStyle.FlexibleHeight.IsApplicable)
				try { SpecificStyle.FlexibleHeight.RawValue = SpecificComponent.flexibleHeight; } catch {}
			if (SpecificStyle.FlexibleWidth.IsApplicable)
				try { SpecificStyle.FlexibleWidth.RawValue = SpecificComponent.flexibleWidth; } catch {}
			if (SpecificStyle.IgnoreLayout.IsApplicable)
				try { SpecificStyle.IgnoreLayout.RawValue = SpecificComponent.ignoreLayout; } catch {}
			if (SpecificStyle.LayoutPriority.IsApplicable)
				try { SpecificStyle.LayoutPriority.RawValue = SpecificComponent.layoutPriority; } catch {}
			if (SpecificStyle.MinHeight.IsApplicable)
				try { SpecificStyle.MinHeight.RawValue = SpecificComponent.minHeight; } catch {}
			if (SpecificStyle.MinWidth.IsApplicable)
				try { SpecificStyle.MinWidth.RawValue = SpecificComponent.minWidth; } catch {}
			if (SpecificStyle.PreferredHeight.IsApplicable)
				try { SpecificStyle.PreferredHeight.RawValue = SpecificComponent.preferredHeight; } catch {}
			if (SpecificStyle.PreferredWidth.IsApplicable)
				try { SpecificStyle.PreferredWidth.RawValue = SpecificComponent.preferredWidth; } catch {}
		}

		public override UiAbstractStyleBase CreateStyle(UiStyleConfig _styleConfig, string _name, UiAbstractStyleBase _template = null)
		{
			UiStyleLayoutElement result = new UiStyleLayoutElement(_styleConfig, _name);

			if (!SpecificComponent)
				return result;

			if (_template != null)
			{
				var specificTemplate = (UiStyleLayoutElement) _template;

				result.Enabled.Value = specificTemplate.Enabled.Value;
				result.Enabled.IsApplicable = specificTemplate.Enabled.IsApplicable;
				result.FlexibleHeight.Value = specificTemplate.FlexibleHeight.Value;
				result.FlexibleHeight.IsApplicable = specificTemplate.FlexibleHeight.IsApplicable;
				result.FlexibleWidth.Value = specificTemplate.FlexibleWidth.Value;
				result.FlexibleWidth.IsApplicable = specificTemplate.FlexibleWidth.IsApplicable;
				result.IgnoreLayout.Value = specificTemplate.IgnoreLayout.Value;
				result.IgnoreLayout.IsApplicable = specificTemplate.IgnoreLayout.IsApplicable;
				result.LayoutPriority.Value = specificTemplate.LayoutPriority.Value;
				result.LayoutPriority.IsApplicable = specificTemplate.LayoutPriority.IsApplicable;
				result.MinHeight.Value = specificTemplate.MinHeight.Value;
				result.MinHeight.IsApplicable = specificTemplate.MinHeight.IsApplicable;
				result.MinWidth.Value = specificTemplate.MinWidth.Value;
				result.MinWidth.IsApplicable = specificTemplate.MinWidth.IsApplicable;
				result.PreferredHeight.Value = specificTemplate.PreferredHeight.Value;
				result.PreferredHeight.IsApplicable = specificTemplate.PreferredHeight.IsApplicable;
				result.PreferredWidth.Value = specificTemplate.PreferredWidth.Value;
				result.PreferredWidth.IsApplicable = specificTemplate.PreferredWidth.IsApplicable;

				return result;
			}

			try { result.Enabled.Value = SpecificComponent.enabled; } catch {}
			try { result.FlexibleHeight.Value = SpecificComponent.flexibleHeight; } catch {}
			try { result.FlexibleWidth.Value = SpecificComponent.flexibleWidth; } catch {}
			try { result.IgnoreLayout.Value = SpecificComponent.ignoreLayout; } catch {}
			try { result.LayoutPriority.Value = SpecificComponent.layoutPriority; } catch {}
			try { result.MinHeight.Value = SpecificComponent.minHeight; } catch {}
			try { result.MinWidth.Value = SpecificComponent.minWidth; } catch {}
			try { result.PreferredHeight.Value = SpecificComponent.preferredHeight; } catch {}
			try { result.PreferredWidth.Value = SpecificComponent.preferredWidth; } catch {}

			return result;
		}
	}
}
