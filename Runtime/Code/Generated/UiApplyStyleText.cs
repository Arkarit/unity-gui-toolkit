// Auto-generated, please do not change!
using UnityEngine;
using GuiToolkit;
using GuiToolkit.Style;

namespace GuiToolkit.Style
{
	[ExecuteAlways]
	[RequireComponent(typeof(UnityEngine.UI.Text))]
	public class UiApplyStyleText : UiAbstractApplyStyle<UnityEngine.UI.Text, UiStyleText>
	{
		protected override void ApplyImpl()
		{
			if (!SpecificComponent || SpecificStyle == null)
				return;

			if (SpecificStyle.AlignByGeometry.IsApplicable)
				try { SpecificComponent.alignByGeometry = Tweenable ? SpecificStyle.AlignByGeometry.Value : SpecificStyle.AlignByGeometry.RawValue; } catch {}
			if (SpecificStyle.Alignment.IsApplicable)
				try { SpecificComponent.alignment = Tweenable ? SpecificStyle.Alignment.Value : SpecificStyle.Alignment.RawValue; } catch {}
			if (SpecificStyle.Color.IsApplicable)
				try { SpecificComponent.color = Tweenable ? SpecificStyle.Color.Value : SpecificStyle.Color.RawValue; } catch {}
			if (SpecificStyle.Enabled.IsApplicable)
				try { SpecificComponent.enabled = Tweenable ? SpecificStyle.Enabled.Value : SpecificStyle.Enabled.RawValue; } catch {}
			if (SpecificStyle.Font.IsApplicable)
				try { SpecificComponent.font = Tweenable ? SpecificStyle.Font.Value : SpecificStyle.Font.RawValue; } catch {}
			if (SpecificStyle.FontSize.IsApplicable)
				try { SpecificComponent.fontSize = Tweenable ? SpecificStyle.FontSize.Value : SpecificStyle.FontSize.RawValue; } catch {}
			if (SpecificStyle.FontStyle.IsApplicable)
				try { SpecificComponent.fontStyle = Tweenable ? SpecificStyle.FontStyle.Value : SpecificStyle.FontStyle.RawValue; } catch {}
			if (SpecificStyle.HorizontalOverflow.IsApplicable)
				try { SpecificComponent.horizontalOverflow = Tweenable ? SpecificStyle.HorizontalOverflow.Value : SpecificStyle.HorizontalOverflow.RawValue; } catch {}
			if (SpecificStyle.IsMaskingGraphic.IsApplicable)
				try { SpecificComponent.isMaskingGraphic = Tweenable ? SpecificStyle.IsMaskingGraphic.Value : SpecificStyle.IsMaskingGraphic.RawValue; } catch {}
			if (SpecificStyle.LineSpacing.IsApplicable)
				try { SpecificComponent.lineSpacing = Tweenable ? SpecificStyle.LineSpacing.Value : SpecificStyle.LineSpacing.RawValue; } catch {}
			if (SpecificStyle.Maskable.IsApplicable)
				try { SpecificComponent.maskable = Tweenable ? SpecificStyle.Maskable.Value : SpecificStyle.Maskable.RawValue; } catch {}
			if (SpecificStyle.Material.IsApplicable)
				try { SpecificComponent.material = Tweenable ? SpecificStyle.Material.Value : SpecificStyle.Material.RawValue; } catch {}
			if (SpecificStyle.OnCullStateChanged.IsApplicable)
				try { SpecificComponent.onCullStateChanged = Tweenable ? SpecificStyle.OnCullStateChanged.Value : SpecificStyle.OnCullStateChanged.RawValue; } catch {}
			if (SpecificStyle.RaycastPadding.IsApplicable)
				try { SpecificComponent.raycastPadding = Tweenable ? SpecificStyle.RaycastPadding.Value : SpecificStyle.RaycastPadding.RawValue; } catch {}
			if (SpecificStyle.RaycastTarget.IsApplicable)
				try { SpecificComponent.raycastTarget = Tweenable ? SpecificStyle.RaycastTarget.Value : SpecificStyle.RaycastTarget.RawValue; } catch {}
			if (SpecificStyle.ResizeTextForBestFit.IsApplicable)
				try { SpecificComponent.resizeTextForBestFit = Tweenable ? SpecificStyle.ResizeTextForBestFit.Value : SpecificStyle.ResizeTextForBestFit.RawValue; } catch {}
			if (SpecificStyle.ResizeTextMaxSize.IsApplicable)
				try { SpecificComponent.resizeTextMaxSize = Tweenable ? SpecificStyle.ResizeTextMaxSize.Value : SpecificStyle.ResizeTextMaxSize.RawValue; } catch {}
			if (SpecificStyle.ResizeTextMinSize.IsApplicable)
				try { SpecificComponent.resizeTextMinSize = Tweenable ? SpecificStyle.ResizeTextMinSize.Value : SpecificStyle.ResizeTextMinSize.RawValue; } catch {}
			if (SpecificStyle.SupportRichText.IsApplicable)
				try { SpecificComponent.supportRichText = Tweenable ? SpecificStyle.SupportRichText.Value : SpecificStyle.SupportRichText.RawValue; } catch {}
			if (SpecificStyle.Text.IsApplicable)
				try { SpecificComponent.text = Tweenable ? SpecificStyle.Text.Value : SpecificStyle.Text.RawValue; } catch {}
			if (SpecificStyle.VerticalOverflow.IsApplicable)
				try { SpecificComponent.verticalOverflow = Tweenable ? SpecificStyle.VerticalOverflow.Value : SpecificStyle.VerticalOverflow.RawValue; } catch {}
		}

		protected override void RecordImpl()
		{
			if (!SpecificComponent || SpecificStyle == null)
				return;

			if (SpecificStyle.AlignByGeometry.IsApplicable)
				try { SpecificStyle.AlignByGeometry.RawValue = SpecificComponent.alignByGeometry; } catch {}
			if (SpecificStyle.Alignment.IsApplicable)
				try { SpecificStyle.Alignment.RawValue = SpecificComponent.alignment; } catch {}
			if (SpecificStyle.Color.IsApplicable)
				try { SpecificStyle.Color.RawValue = SpecificComponent.color; } catch {}
			if (SpecificStyle.Enabled.IsApplicable)
				try { SpecificStyle.Enabled.RawValue = SpecificComponent.enabled; } catch {}
			if (SpecificStyle.Font.IsApplicable)
				try { SpecificStyle.Font.RawValue = SpecificComponent.font; } catch {}
			if (SpecificStyle.FontSize.IsApplicable)
				try { SpecificStyle.FontSize.RawValue = SpecificComponent.fontSize; } catch {}
			if (SpecificStyle.FontStyle.IsApplicable)
				try { SpecificStyle.FontStyle.RawValue = SpecificComponent.fontStyle; } catch {}
			if (SpecificStyle.HorizontalOverflow.IsApplicable)
				try { SpecificStyle.HorizontalOverflow.RawValue = SpecificComponent.horizontalOverflow; } catch {}
			if (SpecificStyle.IsMaskingGraphic.IsApplicable)
				try { SpecificStyle.IsMaskingGraphic.RawValue = SpecificComponent.isMaskingGraphic; } catch {}
			if (SpecificStyle.LineSpacing.IsApplicable)
				try { SpecificStyle.LineSpacing.RawValue = SpecificComponent.lineSpacing; } catch {}
			if (SpecificStyle.Maskable.IsApplicable)
				try { SpecificStyle.Maskable.RawValue = SpecificComponent.maskable; } catch {}
			if (SpecificStyle.Material.IsApplicable)
				try { SpecificStyle.Material.RawValue = SpecificComponent.material; } catch {}
			if (SpecificStyle.OnCullStateChanged.IsApplicable)
				try { SpecificStyle.OnCullStateChanged.RawValue = SpecificComponent.onCullStateChanged; } catch {}
			if (SpecificStyle.RaycastPadding.IsApplicable)
				try { SpecificStyle.RaycastPadding.RawValue = SpecificComponent.raycastPadding; } catch {}
			if (SpecificStyle.RaycastTarget.IsApplicable)
				try { SpecificStyle.RaycastTarget.RawValue = SpecificComponent.raycastTarget; } catch {}
			if (SpecificStyle.ResizeTextForBestFit.IsApplicable)
				try { SpecificStyle.ResizeTextForBestFit.RawValue = SpecificComponent.resizeTextForBestFit; } catch {}
			if (SpecificStyle.ResizeTextMaxSize.IsApplicable)
				try { SpecificStyle.ResizeTextMaxSize.RawValue = SpecificComponent.resizeTextMaxSize; } catch {}
			if (SpecificStyle.ResizeTextMinSize.IsApplicable)
				try { SpecificStyle.ResizeTextMinSize.RawValue = SpecificComponent.resizeTextMinSize; } catch {}
			if (SpecificStyle.SupportRichText.IsApplicable)
				try { SpecificStyle.SupportRichText.RawValue = SpecificComponent.supportRichText; } catch {}
			if (SpecificStyle.Text.IsApplicable)
				try { SpecificStyle.Text.RawValue = SpecificComponent.text; } catch {}
			if (SpecificStyle.VerticalOverflow.IsApplicable)
				try { SpecificStyle.VerticalOverflow.RawValue = SpecificComponent.verticalOverflow; } catch {}
		}

		public override UiAbstractStyleBase CreateStyle(UiStyleConfig _styleConfig, string _name, UiAbstractStyleBase _template = null)
		{
			UiStyleText result = new UiStyleText(_styleConfig, _name);

			if (!SpecificComponent)
				return result;

			if (_template != null)
			{
				var specificTemplate = (UiStyleText) _template;

				result.AlignByGeometry.Value = specificTemplate.AlignByGeometry.Value;
				result.AlignByGeometry.IsApplicable = specificTemplate.AlignByGeometry.IsApplicable;
				result.Alignment.Value = specificTemplate.Alignment.Value;
				result.Alignment.IsApplicable = specificTemplate.Alignment.IsApplicable;
				result.Color.Value = specificTemplate.Color.Value;
				result.Color.IsApplicable = specificTemplate.Color.IsApplicable;
				result.Enabled.Value = specificTemplate.Enabled.Value;
				result.Enabled.IsApplicable = specificTemplate.Enabled.IsApplicable;
				result.Font.Value = specificTemplate.Font.Value;
				result.Font.IsApplicable = specificTemplate.Font.IsApplicable;
				result.FontSize.Value = specificTemplate.FontSize.Value;
				result.FontSize.IsApplicable = specificTemplate.FontSize.IsApplicable;
				result.FontStyle.Value = specificTemplate.FontStyle.Value;
				result.FontStyle.IsApplicable = specificTemplate.FontStyle.IsApplicable;
				result.HorizontalOverflow.Value = specificTemplate.HorizontalOverflow.Value;
				result.HorizontalOverflow.IsApplicable = specificTemplate.HorizontalOverflow.IsApplicable;
				result.IsMaskingGraphic.Value = specificTemplate.IsMaskingGraphic.Value;
				result.IsMaskingGraphic.IsApplicable = specificTemplate.IsMaskingGraphic.IsApplicable;
				result.LineSpacing.Value = specificTemplate.LineSpacing.Value;
				result.LineSpacing.IsApplicable = specificTemplate.LineSpacing.IsApplicable;
				result.Maskable.Value = specificTemplate.Maskable.Value;
				result.Maskable.IsApplicable = specificTemplate.Maskable.IsApplicable;
				result.Material.Value = specificTemplate.Material.Value;
				result.Material.IsApplicable = specificTemplate.Material.IsApplicable;
				result.OnCullStateChanged.Value = specificTemplate.OnCullStateChanged.Value;
				result.OnCullStateChanged.IsApplicable = specificTemplate.OnCullStateChanged.IsApplicable;
				result.RaycastPadding.Value = specificTemplate.RaycastPadding.Value;
				result.RaycastPadding.IsApplicable = specificTemplate.RaycastPadding.IsApplicable;
				result.RaycastTarget.Value = specificTemplate.RaycastTarget.Value;
				result.RaycastTarget.IsApplicable = specificTemplate.RaycastTarget.IsApplicable;
				result.ResizeTextForBestFit.Value = specificTemplate.ResizeTextForBestFit.Value;
				result.ResizeTextForBestFit.IsApplicable = specificTemplate.ResizeTextForBestFit.IsApplicable;
				result.ResizeTextMaxSize.Value = specificTemplate.ResizeTextMaxSize.Value;
				result.ResizeTextMaxSize.IsApplicable = specificTemplate.ResizeTextMaxSize.IsApplicable;
				result.ResizeTextMinSize.Value = specificTemplate.ResizeTextMinSize.Value;
				result.ResizeTextMinSize.IsApplicable = specificTemplate.ResizeTextMinSize.IsApplicable;
				result.SupportRichText.Value = specificTemplate.SupportRichText.Value;
				result.SupportRichText.IsApplicable = specificTemplate.SupportRichText.IsApplicable;
				result.Text.Value = specificTemplate.Text.Value;
				result.Text.IsApplicable = specificTemplate.Text.IsApplicable;
				result.VerticalOverflow.Value = specificTemplate.VerticalOverflow.Value;
				result.VerticalOverflow.IsApplicable = specificTemplate.VerticalOverflow.IsApplicable;

				return result;
			}

			try { result.AlignByGeometry.Value = SpecificComponent.alignByGeometry; } catch {}
			try { result.Alignment.Value = SpecificComponent.alignment; } catch {}
			try { result.Color.Value = SpecificComponent.color; } catch {}
			try { result.Enabled.Value = SpecificComponent.enabled; } catch {}
			try { result.Font.Value = SpecificComponent.font; } catch {}
			try { result.FontSize.Value = SpecificComponent.fontSize; } catch {}
			try { result.FontStyle.Value = SpecificComponent.fontStyle; } catch {}
			try { result.HorizontalOverflow.Value = SpecificComponent.horizontalOverflow; } catch {}
			try { result.IsMaskingGraphic.Value = SpecificComponent.isMaskingGraphic; } catch {}
			try { result.LineSpacing.Value = SpecificComponent.lineSpacing; } catch {}
			try { result.Maskable.Value = SpecificComponent.maskable; } catch {}
			try { result.Material.Value = SpecificComponent.material; } catch {}
			try { result.OnCullStateChanged.Value = SpecificComponent.onCullStateChanged; } catch {}
			try { result.RaycastPadding.Value = SpecificComponent.raycastPadding; } catch {}
			try { result.RaycastTarget.Value = SpecificComponent.raycastTarget; } catch {}
			try { result.ResizeTextForBestFit.Value = SpecificComponent.resizeTextForBestFit; } catch {}
			try { result.ResizeTextMaxSize.Value = SpecificComponent.resizeTextMaxSize; } catch {}
			try { result.ResizeTextMinSize.Value = SpecificComponent.resizeTextMinSize; } catch {}
			try { result.SupportRichText.Value = SpecificComponent.supportRichText; } catch {}
			try { result.Text.Value = SpecificComponent.text; } catch {}
			try { result.VerticalOverflow.Value = SpecificComponent.verticalOverflow; } catch {}

			return result;
		}
	}
}
