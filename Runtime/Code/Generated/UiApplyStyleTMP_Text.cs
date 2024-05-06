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

			SpecificMonoBehaviour.isRightToLeftText = SpecificStyle.IsRightToLeftText;
			SpecificMonoBehaviour.font = SpecificStyle.Font;
			SpecificMonoBehaviour.fontSharedMaterial = SpecificStyle.FontSharedMaterial;
			SpecificMonoBehaviour.fontSharedMaterials = SpecificStyle.FontSharedMaterials;
			SpecificMonoBehaviour.fontMaterial = SpecificStyle.FontMaterial;
			SpecificMonoBehaviour.fontMaterials = SpecificStyle.FontMaterials;
			SpecificMonoBehaviour.color = SpecificStyle.Color;
			SpecificMonoBehaviour.alpha = SpecificStyle.Alpha;
			SpecificMonoBehaviour.enableVertexGradient = SpecificStyle.EnableVertexGradient;
			SpecificMonoBehaviour.colorGradient = SpecificStyle.ColorGradient;
			SpecificMonoBehaviour.colorGradientPreset = SpecificStyle.ColorGradientPreset;
			SpecificMonoBehaviour.spriteAsset = SpecificStyle.SpriteAsset;
			SpecificMonoBehaviour.tintAllSprites = SpecificStyle.TintAllSprites;
			SpecificMonoBehaviour.styleSheet = SpecificStyle.StyleSheet;
			SpecificMonoBehaviour.textStyle = SpecificStyle.TextStyle;
			SpecificMonoBehaviour.overrideColorTags = SpecificStyle.OverrideColorTags;
			SpecificMonoBehaviour.faceColor = SpecificStyle.FaceColor;
			SpecificMonoBehaviour.outlineColor = SpecificStyle.OutlineColor;
			SpecificMonoBehaviour.outlineWidth = SpecificStyle.OutlineWidth;
			SpecificMonoBehaviour.fontSize = SpecificStyle.FontSize;
			SpecificMonoBehaviour.fontWeight = SpecificStyle.FontWeight;
			SpecificMonoBehaviour.enableAutoSizing = SpecificStyle.EnableAutoSizing;
			SpecificMonoBehaviour.fontSizeMin = SpecificStyle.FontSizeMin;
			SpecificMonoBehaviour.fontSizeMax = SpecificStyle.FontSizeMax;
			SpecificMonoBehaviour.fontStyle = SpecificStyle.FontStyle;
			SpecificMonoBehaviour.horizontalAlignment = SpecificStyle.HorizontalAlignment;
			SpecificMonoBehaviour.verticalAlignment = SpecificStyle.VerticalAlignment;
			SpecificMonoBehaviour.alignment = SpecificStyle.Alignment;
			SpecificMonoBehaviour.characterSpacing = SpecificStyle.CharacterSpacing;
			SpecificMonoBehaviour.wordSpacing = SpecificStyle.WordSpacing;
			SpecificMonoBehaviour.lineSpacing = SpecificStyle.LineSpacing;
			SpecificMonoBehaviour.lineSpacingAdjustment = SpecificStyle.LineSpacingAdjustment;
			SpecificMonoBehaviour.paragraphSpacing = SpecificStyle.ParagraphSpacing;
			SpecificMonoBehaviour.characterWidthAdjustment = SpecificStyle.CharacterWidthAdjustment;
			SpecificMonoBehaviour.enableWordWrapping = SpecificStyle.EnableWordWrapping;
			SpecificMonoBehaviour.wordWrappingRatios = SpecificStyle.WordWrappingRatios;
			SpecificMonoBehaviour.overflowMode = SpecificStyle.OverflowMode;
			SpecificMonoBehaviour.linkedTextComponent = SpecificStyle.LinkedTextComponent;
			SpecificMonoBehaviour.enableKerning = SpecificStyle.EnableKerning;
			SpecificMonoBehaviour.extraPadding = SpecificStyle.ExtraPadding;
			SpecificMonoBehaviour.richText = SpecificStyle.RichText;
			SpecificMonoBehaviour.parseCtrlCharacters = SpecificStyle.ParseCtrlCharacters;
			SpecificMonoBehaviour.isOverlay = SpecificStyle.IsOverlay;
			SpecificMonoBehaviour.isOrthographic = SpecificStyle.IsOrthographic;
			SpecificMonoBehaviour.enableCulling = SpecificStyle.EnableCulling;
			SpecificMonoBehaviour.ignoreVisibility = SpecificStyle.IgnoreVisibility;
			SpecificMonoBehaviour.horizontalMapping = SpecificStyle.HorizontalMapping;
			SpecificMonoBehaviour.verticalMapping = SpecificStyle.VerticalMapping;
			SpecificMonoBehaviour.mappingUvLineOffset = SpecificStyle.MappingUvLineOffset;
			SpecificMonoBehaviour.renderMode = SpecificStyle.RenderMode;
			SpecificMonoBehaviour.geometrySortingOrder = SpecificStyle.GeometrySortingOrder;
			SpecificMonoBehaviour.isTextObjectScaleStatic = SpecificStyle.IsTextObjectScaleStatic;
			SpecificMonoBehaviour.vertexBufferAutoSizeReduction = SpecificStyle.VertexBufferAutoSizeReduction;
			SpecificMonoBehaviour.firstVisibleCharacter = SpecificStyle.FirstVisibleCharacter;
			SpecificMonoBehaviour.maxVisibleCharacters = SpecificStyle.MaxVisibleCharacters;
			SpecificMonoBehaviour.maxVisibleWords = SpecificStyle.MaxVisibleWords;
			SpecificMonoBehaviour.maxVisibleLines = SpecificStyle.MaxVisibleLines;
			SpecificMonoBehaviour.useMaxVisibleDescender = SpecificStyle.UseMaxVisibleDescender;
			SpecificMonoBehaviour.pageToDisplay = SpecificStyle.PageToDisplay;
			SpecificMonoBehaviour.margin = SpecificStyle.Margin;
			SpecificMonoBehaviour.havePropertiesChanged = SpecificStyle.HavePropertiesChanged;
			SpecificMonoBehaviour.isUsingLegacyAnimationComponent = SpecificStyle.IsUsingLegacyAnimationComponent;
			SpecificMonoBehaviour.autoSizeTextContainer = SpecificStyle.AutoSizeTextContainer;
			SpecificMonoBehaviour.isVolumetricText = SpecificStyle.IsVolumetricText;
			SpecificMonoBehaviour.maskable = SpecificStyle.Maskable;
			SpecificMonoBehaviour.isMaskingGraphic = SpecificStyle.IsMaskingGraphic;
			SpecificMonoBehaviour.raycastTarget = SpecificStyle.RaycastTarget;
			SpecificMonoBehaviour.raycastPadding = SpecificStyle.RaycastPadding;
			SpecificMonoBehaviour.material = SpecificStyle.Material;
		}

		public override UiAbstractStyleBase CreateStyle(string _name)
		{
			UiStyleTMP_Text result = new UiStyleTMP_Text();

			if (!SpecificMonoBehaviour)
				return result;

			result.Name = _name;
			result.IsRightToLeftText = SpecificMonoBehaviour.isRightToLeftText;
			result.Font = SpecificMonoBehaviour.font;
			result.FontSharedMaterial = SpecificMonoBehaviour.fontSharedMaterial;
			result.FontSharedMaterials = SpecificMonoBehaviour.fontSharedMaterials;
			result.FontMaterial = SpecificMonoBehaviour.fontMaterial;
			result.FontMaterials = SpecificMonoBehaviour.fontMaterials;
			result.Color = SpecificMonoBehaviour.color;
			result.Alpha = SpecificMonoBehaviour.alpha;
			result.EnableVertexGradient = SpecificMonoBehaviour.enableVertexGradient;
			result.ColorGradient = SpecificMonoBehaviour.colorGradient;
			result.ColorGradientPreset = SpecificMonoBehaviour.colorGradientPreset;
			result.SpriteAsset = SpecificMonoBehaviour.spriteAsset;
			result.TintAllSprites = SpecificMonoBehaviour.tintAllSprites;
			result.StyleSheet = SpecificMonoBehaviour.styleSheet;
			result.TextStyle = SpecificMonoBehaviour.textStyle;
			result.OverrideColorTags = SpecificMonoBehaviour.overrideColorTags;
			result.FaceColor = SpecificMonoBehaviour.faceColor;
			result.OutlineColor = SpecificMonoBehaviour.outlineColor;
			result.OutlineWidth = SpecificMonoBehaviour.outlineWidth;
			result.FontSize = SpecificMonoBehaviour.fontSize;
			result.FontWeight = SpecificMonoBehaviour.fontWeight;
			result.EnableAutoSizing = SpecificMonoBehaviour.enableAutoSizing;
			result.FontSizeMin = SpecificMonoBehaviour.fontSizeMin;
			result.FontSizeMax = SpecificMonoBehaviour.fontSizeMax;
			result.FontStyle = SpecificMonoBehaviour.fontStyle;
			result.HorizontalAlignment = SpecificMonoBehaviour.horizontalAlignment;
			result.VerticalAlignment = SpecificMonoBehaviour.verticalAlignment;
			result.Alignment = SpecificMonoBehaviour.alignment;
			result.CharacterSpacing = SpecificMonoBehaviour.characterSpacing;
			result.WordSpacing = SpecificMonoBehaviour.wordSpacing;
			result.LineSpacing = SpecificMonoBehaviour.lineSpacing;
			result.LineSpacingAdjustment = SpecificMonoBehaviour.lineSpacingAdjustment;
			result.ParagraphSpacing = SpecificMonoBehaviour.paragraphSpacing;
			result.CharacterWidthAdjustment = SpecificMonoBehaviour.characterWidthAdjustment;
			result.EnableWordWrapping = SpecificMonoBehaviour.enableWordWrapping;
			result.WordWrappingRatios = SpecificMonoBehaviour.wordWrappingRatios;
			result.OverflowMode = SpecificMonoBehaviour.overflowMode;
			result.LinkedTextComponent = SpecificMonoBehaviour.linkedTextComponent;
			result.EnableKerning = SpecificMonoBehaviour.enableKerning;
			result.ExtraPadding = SpecificMonoBehaviour.extraPadding;
			result.RichText = SpecificMonoBehaviour.richText;
			result.ParseCtrlCharacters = SpecificMonoBehaviour.parseCtrlCharacters;
			result.IsOverlay = SpecificMonoBehaviour.isOverlay;
			result.IsOrthographic = SpecificMonoBehaviour.isOrthographic;
			result.EnableCulling = SpecificMonoBehaviour.enableCulling;
			result.IgnoreVisibility = SpecificMonoBehaviour.ignoreVisibility;
			result.HorizontalMapping = SpecificMonoBehaviour.horizontalMapping;
			result.VerticalMapping = SpecificMonoBehaviour.verticalMapping;
			result.MappingUvLineOffset = SpecificMonoBehaviour.mappingUvLineOffset;
			result.RenderMode = SpecificMonoBehaviour.renderMode;
			result.GeometrySortingOrder = SpecificMonoBehaviour.geometrySortingOrder;
			result.IsTextObjectScaleStatic = SpecificMonoBehaviour.isTextObjectScaleStatic;
			result.VertexBufferAutoSizeReduction = SpecificMonoBehaviour.vertexBufferAutoSizeReduction;
			result.FirstVisibleCharacter = SpecificMonoBehaviour.firstVisibleCharacter;
			result.MaxVisibleCharacters = SpecificMonoBehaviour.maxVisibleCharacters;
			result.MaxVisibleWords = SpecificMonoBehaviour.maxVisibleWords;
			result.MaxVisibleLines = SpecificMonoBehaviour.maxVisibleLines;
			result.UseMaxVisibleDescender = SpecificMonoBehaviour.useMaxVisibleDescender;
			result.PageToDisplay = SpecificMonoBehaviour.pageToDisplay;
			result.Margin = SpecificMonoBehaviour.margin;
			result.HavePropertiesChanged = SpecificMonoBehaviour.havePropertiesChanged;
			result.IsUsingLegacyAnimationComponent = SpecificMonoBehaviour.isUsingLegacyAnimationComponent;
			result.AutoSizeTextContainer = SpecificMonoBehaviour.autoSizeTextContainer;
			result.IsVolumetricText = SpecificMonoBehaviour.isVolumetricText;
			result.Maskable = SpecificMonoBehaviour.maskable;
			result.IsMaskingGraphic = SpecificMonoBehaviour.isMaskingGraphic;
			result.RaycastTarget = SpecificMonoBehaviour.raycastTarget;
			result.RaycastPadding = SpecificMonoBehaviour.raycastPadding;
			result.Material = SpecificMonoBehaviour.material;

			return result;
		}
	}
}
