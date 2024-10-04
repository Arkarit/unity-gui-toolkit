// Auto-generated, please do not change!
using UnityEngine;
using GuiToolkit;
using GuiToolkit.Style;

namespace GuiToolkit.Style
{
	[ExecuteAlways]
	[RequireComponent(typeof(TMPro.TMP_Text))]
	public class UiApplyStyleTMP_Text : UiAbstractApplyStyle<TMPro.TMP_Text, UiStyleTMP_Text>
	{
		protected override void ApplyImpl()
		{
			if (!SpecificComponent || SpecificStyle == null)
				return;

			if (SpecificStyle.Text.IsApplicable)
				try { SpecificComponent.text = Tweenable ? SpecificStyle.Text.Value : SpecificStyle.Text.RawValue; } catch {}
			if (SpecificStyle.Font.IsApplicable)
				try { SpecificComponent.font = Tweenable ? SpecificStyle.Font.Value : SpecificStyle.Font.RawValue; } catch {}
			if (SpecificStyle.Color.IsApplicable)
				try { SpecificComponent.color = Tweenable ? SpecificStyle.Color.Value : SpecificStyle.Color.RawValue; } catch {}
			if (SpecificStyle.Alpha.IsApplicable)
				try { SpecificComponent.alpha = Tweenable ? SpecificStyle.Alpha.Value : SpecificStyle.Alpha.RawValue; } catch {}
			if (SpecificStyle.ColorGradient.IsApplicable)
				try { SpecificComponent.colorGradient = Tweenable ? SpecificStyle.ColorGradient.Value : SpecificStyle.ColorGradient.RawValue; } catch {}
			if (SpecificStyle.StyleSheet.IsApplicable)
				try { SpecificComponent.styleSheet = Tweenable ? SpecificStyle.StyleSheet.Value : SpecificStyle.StyleSheet.RawValue; } catch {}
			if (SpecificStyle.TextStyle.IsApplicable)
				try { SpecificComponent.textStyle = Tweenable ? SpecificStyle.TextStyle.Value : SpecificStyle.TextStyle.RawValue; } catch {}
			if (SpecificStyle.OutlineColor.IsApplicable)
				try { SpecificComponent.outlineColor = Tweenable ? SpecificStyle.OutlineColor.Value : SpecificStyle.OutlineColor.RawValue; } catch {}
			if (SpecificStyle.OutlineWidth.IsApplicable)
				try { SpecificComponent.outlineWidth = Tweenable ? SpecificStyle.OutlineWidth.Value : SpecificStyle.OutlineWidth.RawValue; } catch {}
			if (SpecificStyle.FontSize.IsApplicable)
				try { SpecificComponent.fontSize = Tweenable ? SpecificStyle.FontSize.Value : SpecificStyle.FontSize.RawValue; } catch {}
			if (SpecificStyle.FontWeight.IsApplicable)
				try { SpecificComponent.fontWeight = Tweenable ? SpecificStyle.FontWeight.Value : SpecificStyle.FontWeight.RawValue; } catch {}
			if (SpecificStyle.FontSizeMin.IsApplicable)
				try { SpecificComponent.fontSizeMin = Tweenable ? SpecificStyle.FontSizeMin.Value : SpecificStyle.FontSizeMin.RawValue; } catch {}
			if (SpecificStyle.FontSizeMax.IsApplicable)
				try { SpecificComponent.fontSizeMax = Tweenable ? SpecificStyle.FontSizeMax.Value : SpecificStyle.FontSizeMax.RawValue; } catch {}
			if (SpecificStyle.FontStyle.IsApplicable)
				try { SpecificComponent.fontStyle = Tweenable ? SpecificStyle.FontStyle.Value : SpecificStyle.FontStyle.RawValue; } catch {}
			if (SpecificStyle.Alignment.IsApplicable)
				try { SpecificComponent.alignment = Tweenable ? SpecificStyle.Alignment.Value : SpecificStyle.Alignment.RawValue; } catch {}
			if (SpecificStyle.ExtraPadding.IsApplicable)
				try { SpecificComponent.extraPadding = Tweenable ? SpecificStyle.ExtraPadding.Value : SpecificStyle.ExtraPadding.RawValue; } catch {}
			if (SpecificStyle.Margin.IsApplicable)
				try { SpecificComponent.margin = Tweenable ? SpecificStyle.Margin.Value : SpecificStyle.Margin.RawValue; } catch {}
		}

		protected override void RecordImpl()
		{
			if (!SpecificComponent || SpecificStyle == null)
				return;

			if (SpecificStyle.Text.IsApplicable)
				try { SpecificStyle.Text.RawValue = SpecificComponent.text; } catch {}
			if (SpecificStyle.Font.IsApplicable)
				try { SpecificStyle.Font.RawValue = SpecificComponent.font; } catch {}
			if (SpecificStyle.Color.IsApplicable)
				try { SpecificStyle.Color.RawValue = SpecificComponent.color; } catch {}
			if (SpecificStyle.Alpha.IsApplicable)
				try { SpecificStyle.Alpha.RawValue = SpecificComponent.alpha; } catch {}
			if (SpecificStyle.ColorGradient.IsApplicable)
				try { SpecificStyle.ColorGradient.RawValue = SpecificComponent.colorGradient; } catch {}
			if (SpecificStyle.StyleSheet.IsApplicable)
				try { SpecificStyle.StyleSheet.RawValue = SpecificComponent.styleSheet; } catch {}
			if (SpecificStyle.TextStyle.IsApplicable)
				try { SpecificStyle.TextStyle.RawValue = SpecificComponent.textStyle; } catch {}
			if (SpecificStyle.OutlineColor.IsApplicable)
				try { SpecificStyle.OutlineColor.RawValue = SpecificComponent.outlineColor; } catch {}
			if (SpecificStyle.OutlineWidth.IsApplicable)
				try { SpecificStyle.OutlineWidth.RawValue = SpecificComponent.outlineWidth; } catch {}
			if (SpecificStyle.FontSize.IsApplicable)
				try { SpecificStyle.FontSize.RawValue = SpecificComponent.fontSize; } catch {}
			if (SpecificStyle.FontWeight.IsApplicable)
				try { SpecificStyle.FontWeight.RawValue = SpecificComponent.fontWeight; } catch {}
			if (SpecificStyle.FontSizeMin.IsApplicable)
				try { SpecificStyle.FontSizeMin.RawValue = SpecificComponent.fontSizeMin; } catch {}
			if (SpecificStyle.FontSizeMax.IsApplicable)
				try { SpecificStyle.FontSizeMax.RawValue = SpecificComponent.fontSizeMax; } catch {}
			if (SpecificStyle.FontStyle.IsApplicable)
				try { SpecificStyle.FontStyle.RawValue = SpecificComponent.fontStyle; } catch {}
			if (SpecificStyle.Alignment.IsApplicable)
				try { SpecificStyle.Alignment.RawValue = SpecificComponent.alignment; } catch {}
			if (SpecificStyle.ExtraPadding.IsApplicable)
				try { SpecificStyle.ExtraPadding.RawValue = SpecificComponent.extraPadding; } catch {}
			if (SpecificStyle.Margin.IsApplicable)
				try { SpecificStyle.Margin.RawValue = SpecificComponent.margin; } catch {}
		}

		public override UiAbstractStyleBase CreateStyle(UiStyleConfig _styleConfig, string _name, UiAbstractStyleBase _template = null)
		{
			UiStyleTMP_Text result = new UiStyleTMP_Text(_styleConfig, _name);

			if (!SpecificComponent)
				return result;

			if (_template != null)
			{
				var specificTemplate = (UiStyleTMP_Text) _template;

				result.Text.Value = specificTemplate.Text.Value;
				result.Text.IsApplicable = specificTemplate.Text.IsApplicable;
				result.Font.Value = specificTemplate.Font.Value;
				result.Font.IsApplicable = specificTemplate.Font.IsApplicable;
				result.Color.Value = specificTemplate.Color.Value;
				result.Color.IsApplicable = specificTemplate.Color.IsApplicable;
				result.Alpha.Value = specificTemplate.Alpha.Value;
				result.Alpha.IsApplicable = specificTemplate.Alpha.IsApplicable;
				result.ColorGradient.Value = specificTemplate.ColorGradient.Value;
				result.ColorGradient.IsApplicable = specificTemplate.ColorGradient.IsApplicable;
				result.StyleSheet.Value = specificTemplate.StyleSheet.Value;
				result.StyleSheet.IsApplicable = specificTemplate.StyleSheet.IsApplicable;
				result.TextStyle.Value = specificTemplate.TextStyle.Value;
				result.TextStyle.IsApplicable = specificTemplate.TextStyle.IsApplicable;
				result.OutlineColor.Value = specificTemplate.OutlineColor.Value;
				result.OutlineColor.IsApplicable = specificTemplate.OutlineColor.IsApplicable;
				result.OutlineWidth.Value = specificTemplate.OutlineWidth.Value;
				result.OutlineWidth.IsApplicable = specificTemplate.OutlineWidth.IsApplicable;
				result.FontSize.Value = specificTemplate.FontSize.Value;
				result.FontSize.IsApplicable = specificTemplate.FontSize.IsApplicable;
				result.FontWeight.Value = specificTemplate.FontWeight.Value;
				result.FontWeight.IsApplicable = specificTemplate.FontWeight.IsApplicable;
				result.FontSizeMin.Value = specificTemplate.FontSizeMin.Value;
				result.FontSizeMin.IsApplicable = specificTemplate.FontSizeMin.IsApplicable;
				result.FontSizeMax.Value = specificTemplate.FontSizeMax.Value;
				result.FontSizeMax.IsApplicable = specificTemplate.FontSizeMax.IsApplicable;
				result.FontStyle.Value = specificTemplate.FontStyle.Value;
				result.FontStyle.IsApplicable = specificTemplate.FontStyle.IsApplicable;
				result.Alignment.Value = specificTemplate.Alignment.Value;
				result.Alignment.IsApplicable = specificTemplate.Alignment.IsApplicable;
				result.ExtraPadding.Value = specificTemplate.ExtraPadding.Value;
				result.ExtraPadding.IsApplicable = specificTemplate.ExtraPadding.IsApplicable;
				result.Margin.Value = specificTemplate.Margin.Value;
				result.Margin.IsApplicable = specificTemplate.Margin.IsApplicable;

				return result;
			}

			try { result.Text.Value = SpecificComponent.text; } catch {}
			try { result.Font.Value = SpecificComponent.font; } catch {}
			try { result.Color.Value = SpecificComponent.color; } catch {}
			try { result.Alpha.Value = SpecificComponent.alpha; } catch {}
			try { result.ColorGradient.Value = SpecificComponent.colorGradient; } catch {}
			try { result.StyleSheet.Value = SpecificComponent.styleSheet; } catch {}
			try { result.TextStyle.Value = SpecificComponent.textStyle; } catch {}
			try { result.OutlineColor.Value = SpecificComponent.outlineColor; } catch {}
			try { result.OutlineWidth.Value = SpecificComponent.outlineWidth; } catch {}
			try { result.FontSize.Value = SpecificComponent.fontSize; } catch {}
			try { result.FontWeight.Value = SpecificComponent.fontWeight; } catch {}
			try { result.FontSizeMin.Value = SpecificComponent.fontSizeMin; } catch {}
			try { result.FontSizeMax.Value = SpecificComponent.fontSizeMax; } catch {}
			try { result.FontStyle.Value = SpecificComponent.fontStyle; } catch {}
			try { result.Alignment.Value = SpecificComponent.alignment; } catch {}
			try { result.ExtraPadding.Value = SpecificComponent.extraPadding; } catch {}
			try { result.Margin.Value = SpecificComponent.margin; } catch {}

			return result;
		}
	}
}
