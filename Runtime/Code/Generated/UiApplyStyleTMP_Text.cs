// Auto-generated, please do not change!
using UnityEngine;
using GuiToolkit.Style;

namespace GuiToolkit.Style
{	[ExecuteAlways]
	public class UiApplyStyleTMP_Text : UiAbstractApplyStyle<TMPro.TMP_Text, UiStyleTMP_Text>
	{
		public override void Apply()
		{
			if (!SpecificMonoBehaviour || SpecificStyle == null)
				return;

			if (SpecificStyle.Font.IsApplicable)
				try { SpecificMonoBehaviour.font = SpecificStyle.Font.Value; } catch {}
			if (SpecificStyle.FontSharedMaterial.IsApplicable)
				try { SpecificMonoBehaviour.fontSharedMaterial = SpecificStyle.FontSharedMaterial.Value; } catch {}
			if (SpecificStyle.FontSharedMaterials.IsApplicable)
				try { SpecificMonoBehaviour.fontSharedMaterials = SpecificStyle.FontSharedMaterials.Value; } catch {}
			if (SpecificStyle.FontMaterial.IsApplicable)
				try { SpecificMonoBehaviour.fontMaterial = SpecificStyle.FontMaterial.Value; } catch {}
			if (SpecificStyle.FontMaterials.IsApplicable)
				try { SpecificMonoBehaviour.fontMaterials = SpecificStyle.FontMaterials.Value; } catch {}
			if (SpecificStyle.Color.IsApplicable)
				try { SpecificMonoBehaviour.color = SpecificStyle.Color.Value; } catch {}
			if (SpecificStyle.Alpha.IsApplicable)
				try { SpecificMonoBehaviour.alpha = SpecificStyle.Alpha.Value; } catch {}
			if (SpecificStyle.EnableVertexGradient.IsApplicable)
				try { SpecificMonoBehaviour.enableVertexGradient = SpecificStyle.EnableVertexGradient.Value; } catch {}
			if (SpecificStyle.ColorGradient.IsApplicable)
				try { SpecificMonoBehaviour.colorGradient = SpecificStyle.ColorGradient.Value; } catch {}
			if (SpecificStyle.ColorGradientPreset.IsApplicable)
				try { SpecificMonoBehaviour.colorGradientPreset = SpecificStyle.ColorGradientPreset.Value; } catch {}
			if (SpecificStyle.SpriteAsset.IsApplicable)
				try { SpecificMonoBehaviour.spriteAsset = SpecificStyle.SpriteAsset.Value; } catch {}
			if (SpecificStyle.StyleSheet.IsApplicable)
				try { SpecificMonoBehaviour.styleSheet = SpecificStyle.StyleSheet.Value; } catch {}
			if (SpecificStyle.TextStyle.IsApplicable)
				try { SpecificMonoBehaviour.textStyle = SpecificStyle.TextStyle.Value; } catch {}
			if (SpecificStyle.OverrideColorTags.IsApplicable)
				try { SpecificMonoBehaviour.overrideColorTags = SpecificStyle.OverrideColorTags.Value; } catch {}
			if (SpecificStyle.FaceColor.IsApplicable)
				try { SpecificMonoBehaviour.faceColor = SpecificStyle.FaceColor.Value; } catch {}
			if (SpecificStyle.OutlineColor.IsApplicable)
				try { SpecificMonoBehaviour.outlineColor = SpecificStyle.OutlineColor.Value; } catch {}
			if (SpecificStyle.OutlineWidth.IsApplicable)
				try { SpecificMonoBehaviour.outlineWidth = SpecificStyle.OutlineWidth.Value; } catch {}
			if (SpecificStyle.FontSize.IsApplicable)
				try { SpecificMonoBehaviour.fontSize = SpecificStyle.FontSize.Value; } catch {}
			if (SpecificStyle.FontWeight.IsApplicable)
				try { SpecificMonoBehaviour.fontWeight = SpecificStyle.FontWeight.Value; } catch {}
			if (SpecificStyle.FontSizeMin.IsApplicable)
				try { SpecificMonoBehaviour.fontSizeMin = SpecificStyle.FontSizeMin.Value; } catch {}
			if (SpecificStyle.FontSizeMax.IsApplicable)
				try { SpecificMonoBehaviour.fontSizeMax = SpecificStyle.FontSizeMax.Value; } catch {}
			if (SpecificStyle.FontStyle.IsApplicable)
				try { SpecificMonoBehaviour.fontStyle = SpecificStyle.FontStyle.Value; } catch {}
			if (SpecificStyle.HorizontalAlignment.IsApplicable)
				try { SpecificMonoBehaviour.horizontalAlignment = SpecificStyle.HorizontalAlignment.Value; } catch {}
			if (SpecificStyle.VerticalAlignment.IsApplicable)
				try { SpecificMonoBehaviour.verticalAlignment = SpecificStyle.VerticalAlignment.Value; } catch {}
			if (SpecificStyle.Alignment.IsApplicable)
				try { SpecificMonoBehaviour.alignment = SpecificStyle.Alignment.Value; } catch {}
			if (SpecificStyle.CharacterSpacing.IsApplicable)
				try { SpecificMonoBehaviour.characterSpacing = SpecificStyle.CharacterSpacing.Value; } catch {}
			if (SpecificStyle.WordSpacing.IsApplicable)
				try { SpecificMonoBehaviour.wordSpacing = SpecificStyle.WordSpacing.Value; } catch {}
			if (SpecificStyle.LineSpacing.IsApplicable)
				try { SpecificMonoBehaviour.lineSpacing = SpecificStyle.LineSpacing.Value; } catch {}
			if (SpecificStyle.LineSpacingAdjustment.IsApplicable)
				try { SpecificMonoBehaviour.lineSpacingAdjustment = SpecificStyle.LineSpacingAdjustment.Value; } catch {}
			if (SpecificStyle.ParagraphSpacing.IsApplicable)
				try { SpecificMonoBehaviour.paragraphSpacing = SpecificStyle.ParagraphSpacing.Value; } catch {}
			if (SpecificStyle.CharacterWidthAdjustment.IsApplicable)
				try { SpecificMonoBehaviour.characterWidthAdjustment = SpecificStyle.CharacterWidthAdjustment.Value; } catch {}
			if (SpecificStyle.ExtraPadding.IsApplicable)
				try { SpecificMonoBehaviour.extraPadding = SpecificStyle.ExtraPadding.Value; } catch {}
			if (SpecificStyle.Margin.IsApplicable)
				try { SpecificMonoBehaviour.margin = SpecificStyle.Margin.Value; } catch {}
			if (SpecificStyle.Material.IsApplicable)
				try { SpecificMonoBehaviour.material = SpecificStyle.Material.Value; } catch {}
		}

		public override UiAbstractStyleBase CreateStyle(string _name, UiAbstractStyleBase _template = null)
		{
			UiStyleTMP_Text result = new UiStyleTMP_Text();

			if (!SpecificMonoBehaviour)
				return result;

			result.Name = _name;

			if (_template != null)
			{
				var specificTemplate = (UiStyleTMP_Text) _template;

				result.Font.Value = specificTemplate.Font.Value;
				result.Font.IsApplicable = specificTemplate.Font.IsApplicable;
				result.FontSharedMaterial.Value = specificTemplate.FontSharedMaterial.Value;
				result.FontSharedMaterial.IsApplicable = specificTemplate.FontSharedMaterial.IsApplicable;
				result.FontSharedMaterials.Value = specificTemplate.FontSharedMaterials.Value;
				result.FontSharedMaterials.IsApplicable = specificTemplate.FontSharedMaterials.IsApplicable;
				result.FontMaterial.Value = specificTemplate.FontMaterial.Value;
				result.FontMaterial.IsApplicable = specificTemplate.FontMaterial.IsApplicable;
				result.FontMaterials.Value = specificTemplate.FontMaterials.Value;
				result.FontMaterials.IsApplicable = specificTemplate.FontMaterials.IsApplicable;
				result.Color.Value = specificTemplate.Color.Value;
				result.Color.IsApplicable = specificTemplate.Color.IsApplicable;
				result.Alpha.Value = specificTemplate.Alpha.Value;
				result.Alpha.IsApplicable = specificTemplate.Alpha.IsApplicable;
				result.EnableVertexGradient.Value = specificTemplate.EnableVertexGradient.Value;
				result.EnableVertexGradient.IsApplicable = specificTemplate.EnableVertexGradient.IsApplicable;
				result.ColorGradient.Value = specificTemplate.ColorGradient.Value;
				result.ColorGradient.IsApplicable = specificTemplate.ColorGradient.IsApplicable;
				result.ColorGradientPreset.Value = specificTemplate.ColorGradientPreset.Value;
				result.ColorGradientPreset.IsApplicable = specificTemplate.ColorGradientPreset.IsApplicable;
				result.SpriteAsset.Value = specificTemplate.SpriteAsset.Value;
				result.SpriteAsset.IsApplicable = specificTemplate.SpriteAsset.IsApplicable;
				result.StyleSheet.Value = specificTemplate.StyleSheet.Value;
				result.StyleSheet.IsApplicable = specificTemplate.StyleSheet.IsApplicable;
				result.TextStyle.Value = specificTemplate.TextStyle.Value;
				result.TextStyle.IsApplicable = specificTemplate.TextStyle.IsApplicable;
				result.OverrideColorTags.Value = specificTemplate.OverrideColorTags.Value;
				result.OverrideColorTags.IsApplicable = specificTemplate.OverrideColorTags.IsApplicable;
				result.FaceColor.Value = specificTemplate.FaceColor.Value;
				result.FaceColor.IsApplicable = specificTemplate.FaceColor.IsApplicable;
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
				result.HorizontalAlignment.Value = specificTemplate.HorizontalAlignment.Value;
				result.HorizontalAlignment.IsApplicable = specificTemplate.HorizontalAlignment.IsApplicable;
				result.VerticalAlignment.Value = specificTemplate.VerticalAlignment.Value;
				result.VerticalAlignment.IsApplicable = specificTemplate.VerticalAlignment.IsApplicable;
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
				result.Material.Value = specificTemplate.Material.Value;
				result.Material.IsApplicable = specificTemplate.Material.IsApplicable;

				return result;
			}

			try { result.Font.Value = SpecificMonoBehaviour.font; } catch {}
			try { result.FontSharedMaterial.Value = SpecificMonoBehaviour.fontSharedMaterial; } catch {}
			try { result.FontSharedMaterials.Value = SpecificMonoBehaviour.fontSharedMaterials; } catch {}
			try { result.FontMaterial.Value = SpecificMonoBehaviour.fontMaterial; } catch {}
			try { result.FontMaterials.Value = SpecificMonoBehaviour.fontMaterials; } catch {}
			try { result.Color.Value = SpecificMonoBehaviour.color; } catch {}
			try { result.Alpha.Value = SpecificMonoBehaviour.alpha; } catch {}
			try { result.EnableVertexGradient.Value = SpecificMonoBehaviour.enableVertexGradient; } catch {}
			try { result.ColorGradient.Value = SpecificMonoBehaviour.colorGradient; } catch {}
			try { result.ColorGradientPreset.Value = SpecificMonoBehaviour.colorGradientPreset; } catch {}
			try { result.SpriteAsset.Value = SpecificMonoBehaviour.spriteAsset; } catch {}
			try { result.StyleSheet.Value = SpecificMonoBehaviour.styleSheet; } catch {}
			try { result.TextStyle.Value = SpecificMonoBehaviour.textStyle; } catch {}
			try { result.OverrideColorTags.Value = SpecificMonoBehaviour.overrideColorTags; } catch {}
			try { result.FaceColor.Value = SpecificMonoBehaviour.faceColor; } catch {}
			try { result.OutlineColor.Value = SpecificMonoBehaviour.outlineColor; } catch {}
			try { result.OutlineWidth.Value = SpecificMonoBehaviour.outlineWidth; } catch {}
			try { result.FontSize.Value = SpecificMonoBehaviour.fontSize; } catch {}
			try { result.FontWeight.Value = SpecificMonoBehaviour.fontWeight; } catch {}
			try { result.FontSizeMin.Value = SpecificMonoBehaviour.fontSizeMin; } catch {}
			try { result.FontSizeMax.Value = SpecificMonoBehaviour.fontSizeMax; } catch {}
			try { result.FontStyle.Value = SpecificMonoBehaviour.fontStyle; } catch {}
			try { result.HorizontalAlignment.Value = SpecificMonoBehaviour.horizontalAlignment; } catch {}
			try { result.VerticalAlignment.Value = SpecificMonoBehaviour.verticalAlignment; } catch {}
			try { result.Alignment.Value = SpecificMonoBehaviour.alignment; } catch {}
			try { result.CharacterSpacing.Value = SpecificMonoBehaviour.characterSpacing; } catch {}
			try { result.WordSpacing.Value = SpecificMonoBehaviour.wordSpacing; } catch {}
			try { result.LineSpacing.Value = SpecificMonoBehaviour.lineSpacing; } catch {}
			try { result.LineSpacingAdjustment.Value = SpecificMonoBehaviour.lineSpacingAdjustment; } catch {}
			try { result.ParagraphSpacing.Value = SpecificMonoBehaviour.paragraphSpacing; } catch {}
			try { result.CharacterWidthAdjustment.Value = SpecificMonoBehaviour.characterWidthAdjustment; } catch {}
			try { result.ExtraPadding.Value = SpecificMonoBehaviour.extraPadding; } catch {}
			try { result.Margin.Value = SpecificMonoBehaviour.margin; } catch {}
			try { result.Material.Value = SpecificMonoBehaviour.material; } catch {}

			return result;
		}
	}
}
