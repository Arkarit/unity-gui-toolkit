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
			if (SpecificStyle.FontSharedMaterial.IsApplicable)
				try { SpecificComponent.fontSharedMaterial = Tweenable ? SpecificStyle.FontSharedMaterial.Value : SpecificStyle.FontSharedMaterial.RawValue; } catch {}
			if (SpecificStyle.FontSharedMaterials.IsApplicable)
				try { SpecificComponent.fontSharedMaterials = Tweenable ? SpecificStyle.FontSharedMaterials.Value : SpecificStyle.FontSharedMaterials.RawValue; } catch {}
			if (SpecificStyle.Color.IsApplicable)
				try { SpecificComponent.color = Tweenable ? SpecificStyle.Color.Value : SpecificStyle.Color.RawValue; } catch {}
			if (SpecificStyle.Alpha.IsApplicable)
				try { SpecificComponent.alpha = Tweenable ? SpecificStyle.Alpha.Value : SpecificStyle.Alpha.RawValue; } catch {}
			if (SpecificStyle.ColorGradient.IsApplicable)
				try { SpecificComponent.colorGradient = Tweenable ? SpecificStyle.ColorGradient.Value : SpecificStyle.ColorGradient.RawValue; } catch {}
			if (SpecificStyle.SpriteAsset.IsApplicable)
				try { SpecificComponent.spriteAsset = Tweenable ? SpecificStyle.SpriteAsset.Value : SpecificStyle.SpriteAsset.RawValue; } catch {}
			if (SpecificStyle.TintAllSprites.IsApplicable)
				try { SpecificComponent.tintAllSprites = Tweenable ? SpecificStyle.TintAllSprites.Value : SpecificStyle.TintAllSprites.RawValue; } catch {}
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
			if (SpecificStyle.EnableAutoSizing.IsApplicable)
				try { SpecificComponent.enableAutoSizing = Tweenable ? SpecificStyle.EnableAutoSizing.Value : SpecificStyle.EnableAutoSizing.RawValue; } catch {}
			if (SpecificStyle.FontSizeMin.IsApplicable)
				try { SpecificComponent.fontSizeMin = Tweenable ? SpecificStyle.FontSizeMin.Value : SpecificStyle.FontSizeMin.RawValue; } catch {}
			if (SpecificStyle.FontSizeMax.IsApplicable)
				try { SpecificComponent.fontSizeMax = Tweenable ? SpecificStyle.FontSizeMax.Value : SpecificStyle.FontSizeMax.RawValue; } catch {}
			if (SpecificStyle.FontStyle.IsApplicable)
				try { SpecificComponent.fontStyle = Tweenable ? SpecificStyle.FontStyle.Value : SpecificStyle.FontStyle.RawValue; } catch {}
			if (SpecificStyle.Alignment.IsApplicable)
				try { SpecificComponent.alignment = Tweenable ? SpecificStyle.Alignment.Value : SpecificStyle.Alignment.RawValue; } catch {}
			if (SpecificStyle.CharacterSpacing.IsApplicable)
				try { SpecificComponent.characterSpacing = Tweenable ? SpecificStyle.CharacterSpacing.Value : SpecificStyle.CharacterSpacing.RawValue; } catch {}
			if (SpecificStyle.WordSpacing.IsApplicable)
				try { SpecificComponent.wordSpacing = Tweenable ? SpecificStyle.WordSpacing.Value : SpecificStyle.WordSpacing.RawValue; } catch {}
			if (SpecificStyle.LineSpacing.IsApplicable)
				try { SpecificComponent.lineSpacing = Tweenable ? SpecificStyle.LineSpacing.Value : SpecificStyle.LineSpacing.RawValue; } catch {}
			if (SpecificStyle.LineSpacingAdjustment.IsApplicable)
				try { SpecificComponent.lineSpacingAdjustment = Tweenable ? SpecificStyle.LineSpacingAdjustment.Value : SpecificStyle.LineSpacingAdjustment.RawValue; } catch {}
			if (SpecificStyle.ParagraphSpacing.IsApplicable)
				try { SpecificComponent.paragraphSpacing = Tweenable ? SpecificStyle.ParagraphSpacing.Value : SpecificStyle.ParagraphSpacing.RawValue; } catch {}
			if (SpecificStyle.CharacterWidthAdjustment.IsApplicable)
				try { SpecificComponent.characterWidthAdjustment = Tweenable ? SpecificStyle.CharacterWidthAdjustment.Value : SpecificStyle.CharacterWidthAdjustment.RawValue; } catch {}
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
			if (SpecificStyle.FontSharedMaterial.IsApplicable)
				try { SpecificStyle.FontSharedMaterial.RawValue = SpecificComponent.fontSharedMaterial; } catch {}
			if (SpecificStyle.FontSharedMaterials.IsApplicable)
				try { SpecificStyle.FontSharedMaterials.RawValue = SpecificComponent.fontSharedMaterials; } catch {}
			if (SpecificStyle.Color.IsApplicable)
				try { SpecificStyle.Color.RawValue = SpecificComponent.color; } catch {}
			if (SpecificStyle.Alpha.IsApplicable)
				try { SpecificStyle.Alpha.RawValue = SpecificComponent.alpha; } catch {}
			if (SpecificStyle.ColorGradient.IsApplicable)
				try { SpecificStyle.ColorGradient.RawValue = SpecificComponent.colorGradient; } catch {}
			if (SpecificStyle.SpriteAsset.IsApplicable)
				try { SpecificStyle.SpriteAsset.RawValue = SpecificComponent.spriteAsset; } catch {}
			if (SpecificStyle.TintAllSprites.IsApplicable)
				try { SpecificStyle.TintAllSprites.RawValue = SpecificComponent.tintAllSprites; } catch {}
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
			if (SpecificStyle.EnableAutoSizing.IsApplicable)
				try { SpecificStyle.EnableAutoSizing.RawValue = SpecificComponent.enableAutoSizing; } catch {}
			if (SpecificStyle.FontSizeMin.IsApplicable)
				try { SpecificStyle.FontSizeMin.RawValue = SpecificComponent.fontSizeMin; } catch {}
			if (SpecificStyle.FontSizeMax.IsApplicable)
				try { SpecificStyle.FontSizeMax.RawValue = SpecificComponent.fontSizeMax; } catch {}
			if (SpecificStyle.FontStyle.IsApplicable)
				try { SpecificStyle.FontStyle.RawValue = SpecificComponent.fontStyle; } catch {}
			if (SpecificStyle.Alignment.IsApplicable)
				try { SpecificStyle.Alignment.RawValue = SpecificComponent.alignment; } catch {}
			if (SpecificStyle.CharacterSpacing.IsApplicable)
				try { SpecificStyle.CharacterSpacing.RawValue = SpecificComponent.characterSpacing; } catch {}
			if (SpecificStyle.WordSpacing.IsApplicable)
				try { SpecificStyle.WordSpacing.RawValue = SpecificComponent.wordSpacing; } catch {}
			if (SpecificStyle.LineSpacing.IsApplicable)
				try { SpecificStyle.LineSpacing.RawValue = SpecificComponent.lineSpacing; } catch {}
			if (SpecificStyle.LineSpacingAdjustment.IsApplicable)
				try { SpecificStyle.LineSpacingAdjustment.RawValue = SpecificComponent.lineSpacingAdjustment; } catch {}
			if (SpecificStyle.ParagraphSpacing.IsApplicable)
				try { SpecificStyle.ParagraphSpacing.RawValue = SpecificComponent.paragraphSpacing; } catch {}
			if (SpecificStyle.CharacterWidthAdjustment.IsApplicable)
				try { SpecificStyle.CharacterWidthAdjustment.RawValue = SpecificComponent.characterWidthAdjustment; } catch {}
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
				result.FontSharedMaterial.Value = specificTemplate.FontSharedMaterial.Value;
				result.FontSharedMaterial.IsApplicable = specificTemplate.FontSharedMaterial.IsApplicable;
				result.FontSharedMaterials.Value = specificTemplate.FontSharedMaterials.Value;
				result.FontSharedMaterials.IsApplicable = specificTemplate.FontSharedMaterials.IsApplicable;
				result.Color.Value = specificTemplate.Color.Value;
				result.Color.IsApplicable = specificTemplate.Color.IsApplicable;
				result.Alpha.Value = specificTemplate.Alpha.Value;
				result.Alpha.IsApplicable = specificTemplate.Alpha.IsApplicable;
				result.ColorGradient.Value = specificTemplate.ColorGradient.Value;
				result.ColorGradient.IsApplicable = specificTemplate.ColorGradient.IsApplicable;
				result.SpriteAsset.Value = specificTemplate.SpriteAsset.Value;
				result.SpriteAsset.IsApplicable = specificTemplate.SpriteAsset.IsApplicable;
				result.TintAllSprites.Value = specificTemplate.TintAllSprites.Value;
				result.TintAllSprites.IsApplicable = specificTemplate.TintAllSprites.IsApplicable;
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
				result.EnableAutoSizing.Value = specificTemplate.EnableAutoSizing.Value;
				result.EnableAutoSizing.IsApplicable = specificTemplate.EnableAutoSizing.IsApplicable;
				result.FontSizeMin.Value = specificTemplate.FontSizeMin.Value;
				result.FontSizeMin.IsApplicable = specificTemplate.FontSizeMin.IsApplicable;
				result.FontSizeMax.Value = specificTemplate.FontSizeMax.Value;
				result.FontSizeMax.IsApplicable = specificTemplate.FontSizeMax.IsApplicable;
				result.FontStyle.Value = specificTemplate.FontStyle.Value;
				result.FontStyle.IsApplicable = specificTemplate.FontStyle.IsApplicable;
				result.Alignment.Value = specificTemplate.Alignment.Value;
				result.Alignment.IsApplicable = specificTemplate.Alignment.IsApplicable;
				result.CharacterSpacing.Value = specificTemplate.CharacterSpacing.Value;
				result.CharacterSpacing.IsApplicable = specificTemplate.CharacterSpacing.IsApplicable;
				result.WordSpacing.Value = specificTemplate.WordSpacing.Value;
				result.WordSpacing.IsApplicable = specificTemplate.WordSpacing.IsApplicable;
				result.LineSpacing.Value = specificTemplate.LineSpacing.Value;
				result.LineSpacing.IsApplicable = specificTemplate.LineSpacing.IsApplicable;
				result.LineSpacingAdjustment.Value = specificTemplate.LineSpacingAdjustment.Value;
				result.LineSpacingAdjustment.IsApplicable = specificTemplate.LineSpacingAdjustment.IsApplicable;
				result.ParagraphSpacing.Value = specificTemplate.ParagraphSpacing.Value;
				result.ParagraphSpacing.IsApplicable = specificTemplate.ParagraphSpacing.IsApplicable;
				result.CharacterWidthAdjustment.Value = specificTemplate.CharacterWidthAdjustment.Value;
				result.CharacterWidthAdjustment.IsApplicable = specificTemplate.CharacterWidthAdjustment.IsApplicable;
				result.ExtraPadding.Value = specificTemplate.ExtraPadding.Value;
				result.ExtraPadding.IsApplicable = specificTemplate.ExtraPadding.IsApplicable;
				result.Margin.Value = specificTemplate.Margin.Value;
				result.Margin.IsApplicable = specificTemplate.Margin.IsApplicable;

				return result;
			}

			try { result.Text.Value = SpecificComponent.text; } catch {}
			try { result.Font.Value = SpecificComponent.font; } catch {}
			try { result.FontSharedMaterial.Value = SpecificComponent.fontSharedMaterial; } catch {}
			try { result.FontSharedMaterials.Value = SpecificComponent.fontSharedMaterials; } catch {}
			try { result.Color.Value = SpecificComponent.color; } catch {}
			try { result.Alpha.Value = SpecificComponent.alpha; } catch {}
			try { result.ColorGradient.Value = SpecificComponent.colorGradient; } catch {}
			try { result.SpriteAsset.Value = SpecificComponent.spriteAsset; } catch {}
			try { result.TintAllSprites.Value = SpecificComponent.tintAllSprites; } catch {}
			try { result.StyleSheet.Value = SpecificComponent.styleSheet; } catch {}
			try { result.TextStyle.Value = SpecificComponent.textStyle; } catch {}
			try { result.OutlineColor.Value = SpecificComponent.outlineColor; } catch {}
			try { result.OutlineWidth.Value = SpecificComponent.outlineWidth; } catch {}
			try { result.FontSize.Value = SpecificComponent.fontSize; } catch {}
			try { result.FontWeight.Value = SpecificComponent.fontWeight; } catch {}
			try { result.EnableAutoSizing.Value = SpecificComponent.enableAutoSizing; } catch {}
			try { result.FontSizeMin.Value = SpecificComponent.fontSizeMin; } catch {}
			try { result.FontSizeMax.Value = SpecificComponent.fontSizeMax; } catch {}
			try { result.FontStyle.Value = SpecificComponent.fontStyle; } catch {}
			try { result.Alignment.Value = SpecificComponent.alignment; } catch {}
			try { result.CharacterSpacing.Value = SpecificComponent.characterSpacing; } catch {}
			try { result.WordSpacing.Value = SpecificComponent.wordSpacing; } catch {}
			try { result.LineSpacing.Value = SpecificComponent.lineSpacing; } catch {}
			try { result.LineSpacingAdjustment.Value = SpecificComponent.lineSpacingAdjustment; } catch {}
			try { result.ParagraphSpacing.Value = SpecificComponent.paragraphSpacing; } catch {}
			try { result.CharacterWidthAdjustment.Value = SpecificComponent.characterWidthAdjustment; } catch {}
			try { result.ExtraPadding.Value = SpecificComponent.extraPadding; } catch {}
			try { result.Margin.Value = SpecificComponent.margin; } catch {}

			return result;
		}
	}
}
