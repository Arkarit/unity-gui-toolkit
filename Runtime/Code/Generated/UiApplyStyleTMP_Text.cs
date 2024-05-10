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

		public override UiAbstractStyleBase CreateStyle(string _name)
		{
			UiStyleTMP_Text result = new UiStyleTMP_Text();

			if (!SpecificMonoBehaviour)
				return result;

			result.Name = _name;
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
