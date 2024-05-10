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

			if (SpecificStyle.IsRightToLeftText.IsApplicable)
				try { SpecificMonoBehaviour.isRightToLeftText = SpecificStyle.IsRightToLeftText.Value; } catch {}
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
			if (SpecificStyle.TintAllSprites.IsApplicable)
				try { SpecificMonoBehaviour.tintAllSprites = SpecificStyle.TintAllSprites.Value; } catch {}
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
			if (SpecificStyle.EnableAutoSizing.IsApplicable)
				try { SpecificMonoBehaviour.enableAutoSizing = SpecificStyle.EnableAutoSizing.Value; } catch {}
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
			if (SpecificStyle.EnableWordWrapping.IsApplicable)
				try { SpecificMonoBehaviour.enableWordWrapping = SpecificStyle.EnableWordWrapping.Value; } catch {}
			if (SpecificStyle.WordWrappingRatios.IsApplicable)
				try { SpecificMonoBehaviour.wordWrappingRatios = SpecificStyle.WordWrappingRatios.Value; } catch {}
			if (SpecificStyle.OverflowMode.IsApplicable)
				try { SpecificMonoBehaviour.overflowMode = SpecificStyle.OverflowMode.Value; } catch {}
			if (SpecificStyle.LinkedTextComponent.IsApplicable)
				try { SpecificMonoBehaviour.linkedTextComponent = SpecificStyle.LinkedTextComponent.Value; } catch {}
			if (SpecificStyle.EnableKerning.IsApplicable)
				try { SpecificMonoBehaviour.enableKerning = SpecificStyle.EnableKerning.Value; } catch {}
			if (SpecificStyle.ExtraPadding.IsApplicable)
				try { SpecificMonoBehaviour.extraPadding = SpecificStyle.ExtraPadding.Value; } catch {}
			if (SpecificStyle.RichText.IsApplicable)
				try { SpecificMonoBehaviour.richText = SpecificStyle.RichText.Value; } catch {}
			if (SpecificStyle.ParseCtrlCharacters.IsApplicable)
				try { SpecificMonoBehaviour.parseCtrlCharacters = SpecificStyle.ParseCtrlCharacters.Value; } catch {}
			if (SpecificStyle.IsOverlay.IsApplicable)
				try { SpecificMonoBehaviour.isOverlay = SpecificStyle.IsOverlay.Value; } catch {}
			if (SpecificStyle.IsOrthographic.IsApplicable)
				try { SpecificMonoBehaviour.isOrthographic = SpecificStyle.IsOrthographic.Value; } catch {}
			if (SpecificStyle.EnableCulling.IsApplicable)
				try { SpecificMonoBehaviour.enableCulling = SpecificStyle.EnableCulling.Value; } catch {}
			if (SpecificStyle.IgnoreVisibility.IsApplicable)
				try { SpecificMonoBehaviour.ignoreVisibility = SpecificStyle.IgnoreVisibility.Value; } catch {}
			if (SpecificStyle.HorizontalMapping.IsApplicable)
				try { SpecificMonoBehaviour.horizontalMapping = SpecificStyle.HorizontalMapping.Value; } catch {}
			if (SpecificStyle.VerticalMapping.IsApplicable)
				try { SpecificMonoBehaviour.verticalMapping = SpecificStyle.VerticalMapping.Value; } catch {}
			if (SpecificStyle.MappingUvLineOffset.IsApplicable)
				try { SpecificMonoBehaviour.mappingUvLineOffset = SpecificStyle.MappingUvLineOffset.Value; } catch {}
			if (SpecificStyle.RenderMode.IsApplicable)
				try { SpecificMonoBehaviour.renderMode = SpecificStyle.RenderMode.Value; } catch {}
			if (SpecificStyle.GeometrySortingOrder.IsApplicable)
				try { SpecificMonoBehaviour.geometrySortingOrder = SpecificStyle.GeometrySortingOrder.Value; } catch {}
			if (SpecificStyle.IsTextObjectScaleStatic.IsApplicable)
				try { SpecificMonoBehaviour.isTextObjectScaleStatic = SpecificStyle.IsTextObjectScaleStatic.Value; } catch {}
			if (SpecificStyle.VertexBufferAutoSizeReduction.IsApplicable)
				try { SpecificMonoBehaviour.vertexBufferAutoSizeReduction = SpecificStyle.VertexBufferAutoSizeReduction.Value; } catch {}
			if (SpecificStyle.FirstVisibleCharacter.IsApplicable)
				try { SpecificMonoBehaviour.firstVisibleCharacter = SpecificStyle.FirstVisibleCharacter.Value; } catch {}
			if (SpecificStyle.MaxVisibleCharacters.IsApplicable)
				try { SpecificMonoBehaviour.maxVisibleCharacters = SpecificStyle.MaxVisibleCharacters.Value; } catch {}
			if (SpecificStyle.MaxVisibleWords.IsApplicable)
				try { SpecificMonoBehaviour.maxVisibleWords = SpecificStyle.MaxVisibleWords.Value; } catch {}
			if (SpecificStyle.MaxVisibleLines.IsApplicable)
				try { SpecificMonoBehaviour.maxVisibleLines = SpecificStyle.MaxVisibleLines.Value; } catch {}
			if (SpecificStyle.UseMaxVisibleDescender.IsApplicable)
				try { SpecificMonoBehaviour.useMaxVisibleDescender = SpecificStyle.UseMaxVisibleDescender.Value; } catch {}
			if (SpecificStyle.PageToDisplay.IsApplicable)
				try { SpecificMonoBehaviour.pageToDisplay = SpecificStyle.PageToDisplay.Value; } catch {}
			if (SpecificStyle.Margin.IsApplicable)
				try { SpecificMonoBehaviour.margin = SpecificStyle.Margin.Value; } catch {}
			if (SpecificStyle.HavePropertiesChanged.IsApplicable)
				try { SpecificMonoBehaviour.havePropertiesChanged = SpecificStyle.HavePropertiesChanged.Value; } catch {}
			if (SpecificStyle.IsUsingLegacyAnimationComponent.IsApplicable)
				try { SpecificMonoBehaviour.isUsingLegacyAnimationComponent = SpecificStyle.IsUsingLegacyAnimationComponent.Value; } catch {}
			if (SpecificStyle.AutoSizeTextContainer.IsApplicable)
				try { SpecificMonoBehaviour.autoSizeTextContainer = SpecificStyle.AutoSizeTextContainer.Value; } catch {}
			if (SpecificStyle.IsVolumetricText.IsApplicable)
				try { SpecificMonoBehaviour.isVolumetricText = SpecificStyle.IsVolumetricText.Value; } catch {}
			if (SpecificStyle.Maskable.IsApplicable)
				try { SpecificMonoBehaviour.maskable = SpecificStyle.Maskable.Value; } catch {}
			if (SpecificStyle.IsMaskingGraphic.IsApplicable)
				try { SpecificMonoBehaviour.isMaskingGraphic = SpecificStyle.IsMaskingGraphic.Value; } catch {}
			if (SpecificStyle.RaycastTarget.IsApplicable)
				try { SpecificMonoBehaviour.raycastTarget = SpecificStyle.RaycastTarget.Value; } catch {}
			if (SpecificStyle.RaycastPadding.IsApplicable)
				try { SpecificMonoBehaviour.raycastPadding = SpecificStyle.RaycastPadding.Value; } catch {}
			if (SpecificStyle.Material.IsApplicable)
				try { SpecificMonoBehaviour.material = SpecificStyle.Material.Value; } catch {}
		}

		public override UiAbstractStyleBase CreateStyle(string _name)
		{
			UiStyleTMP_Text result = new UiStyleTMP_Text();

			if (!SpecificMonoBehaviour)
				return result;

			result.Name = _name;
			try { result.IsRightToLeftText.Value = SpecificMonoBehaviour.isRightToLeftText; } catch {}
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
			try { result.TintAllSprites.Value = SpecificMonoBehaviour.tintAllSprites; } catch {}
			try { result.StyleSheet.Value = SpecificMonoBehaviour.styleSheet; } catch {}
			try { result.TextStyle.Value = SpecificMonoBehaviour.textStyle; } catch {}
			try { result.OverrideColorTags.Value = SpecificMonoBehaviour.overrideColorTags; } catch {}
			try { result.FaceColor.Value = SpecificMonoBehaviour.faceColor; } catch {}
			try { result.OutlineColor.Value = SpecificMonoBehaviour.outlineColor; } catch {}
			try { result.OutlineWidth.Value = SpecificMonoBehaviour.outlineWidth; } catch {}
			try { result.FontSize.Value = SpecificMonoBehaviour.fontSize; } catch {}
			try { result.FontWeight.Value = SpecificMonoBehaviour.fontWeight; } catch {}
			try { result.EnableAutoSizing.Value = SpecificMonoBehaviour.enableAutoSizing; } catch {}
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
			try { result.EnableWordWrapping.Value = SpecificMonoBehaviour.enableWordWrapping; } catch {}
			try { result.WordWrappingRatios.Value = SpecificMonoBehaviour.wordWrappingRatios; } catch {}
			try { result.OverflowMode.Value = SpecificMonoBehaviour.overflowMode; } catch {}
			try { result.LinkedTextComponent.Value = SpecificMonoBehaviour.linkedTextComponent; } catch {}
			try { result.EnableKerning.Value = SpecificMonoBehaviour.enableKerning; } catch {}
			try { result.ExtraPadding.Value = SpecificMonoBehaviour.extraPadding; } catch {}
			try { result.RichText.Value = SpecificMonoBehaviour.richText; } catch {}
			try { result.ParseCtrlCharacters.Value = SpecificMonoBehaviour.parseCtrlCharacters; } catch {}
			try { result.IsOverlay.Value = SpecificMonoBehaviour.isOverlay; } catch {}
			try { result.IsOrthographic.Value = SpecificMonoBehaviour.isOrthographic; } catch {}
			try { result.EnableCulling.Value = SpecificMonoBehaviour.enableCulling; } catch {}
			try { result.IgnoreVisibility.Value = SpecificMonoBehaviour.ignoreVisibility; } catch {}
			try { result.HorizontalMapping.Value = SpecificMonoBehaviour.horizontalMapping; } catch {}
			try { result.VerticalMapping.Value = SpecificMonoBehaviour.verticalMapping; } catch {}
			try { result.MappingUvLineOffset.Value = SpecificMonoBehaviour.mappingUvLineOffset; } catch {}
			try { result.RenderMode.Value = SpecificMonoBehaviour.renderMode; } catch {}
			try { result.GeometrySortingOrder.Value = SpecificMonoBehaviour.geometrySortingOrder; } catch {}
			try { result.IsTextObjectScaleStatic.Value = SpecificMonoBehaviour.isTextObjectScaleStatic; } catch {}
			try { result.VertexBufferAutoSizeReduction.Value = SpecificMonoBehaviour.vertexBufferAutoSizeReduction; } catch {}
			try { result.FirstVisibleCharacter.Value = SpecificMonoBehaviour.firstVisibleCharacter; } catch {}
			try { result.MaxVisibleCharacters.Value = SpecificMonoBehaviour.maxVisibleCharacters; } catch {}
			try { result.MaxVisibleWords.Value = SpecificMonoBehaviour.maxVisibleWords; } catch {}
			try { result.MaxVisibleLines.Value = SpecificMonoBehaviour.maxVisibleLines; } catch {}
			try { result.UseMaxVisibleDescender.Value = SpecificMonoBehaviour.useMaxVisibleDescender; } catch {}
			try { result.PageToDisplay.Value = SpecificMonoBehaviour.pageToDisplay; } catch {}
			try { result.Margin.Value = SpecificMonoBehaviour.margin; } catch {}
			try { result.HavePropertiesChanged.Value = SpecificMonoBehaviour.havePropertiesChanged; } catch {}
			try { result.IsUsingLegacyAnimationComponent.Value = SpecificMonoBehaviour.isUsingLegacyAnimationComponent; } catch {}
			try { result.AutoSizeTextContainer.Value = SpecificMonoBehaviour.autoSizeTextContainer; } catch {}
			try { result.IsVolumetricText.Value = SpecificMonoBehaviour.isVolumetricText; } catch {}
			try { result.Maskable.Value = SpecificMonoBehaviour.maskable; } catch {}
			try { result.IsMaskingGraphic.Value = SpecificMonoBehaviour.isMaskingGraphic; } catch {}
			try { result.RaycastTarget.Value = SpecificMonoBehaviour.raycastTarget; } catch {}
			try { result.RaycastPadding.Value = SpecificMonoBehaviour.raycastPadding; } catch {}
			try { result.Material.Value = SpecificMonoBehaviour.material; } catch {}

			return result;
		}
	}
}
