// Auto-generated, please do not change!
using System;
using UnityEngine;
using GuiToolkit.Style;

namespace GuiToolkit.Style
{
	[Serializable]
	public class UiStyleTMP_Text : UiAbstractStyle<TMPro.TMP_Text>
	{
		[SerializeField] private ApplicableValue<System.Boolean> m_isRightToLeftText = new();
		[SerializeField] private ApplicableValue<TMPro.TMP_FontAsset> m_font = new();
		[SerializeField] private ApplicableValue<UnityEngine.Material> m_fontSharedMaterial = new();
		[SerializeField] private ApplicableValue<UnityEngine.Material[]> m_fontSharedMaterials = new();
		[SerializeField] private ApplicableValue<UnityEngine.Material> m_fontMaterial = new();
		[SerializeField] private ApplicableValue<UnityEngine.Material[]> m_fontMaterials = new();
		[SerializeField] private ApplicableValue<UnityEngine.Color> m_color = new();
		[SerializeField] private ApplicableValue<System.Single> m_alpha = new();
		[SerializeField] private ApplicableValue<System.Boolean> m_enableVertexGradient = new();
		[SerializeField] private ApplicableValue<TMPro.VertexGradient> m_colorGradient = new();
		[SerializeField] private ApplicableValue<TMPro.TMP_ColorGradient> m_colorGradientPreset = new();
		[SerializeField] private ApplicableValue<TMPro.TMP_SpriteAsset> m_spriteAsset = new();
		[SerializeField] private ApplicableValue<System.Boolean> m_tintAllSprites = new();
		[SerializeField] private ApplicableValue<TMPro.TMP_StyleSheet> m_styleSheet = new();
		[SerializeField] private ApplicableValue<TMPro.TMP_Style> m_textStyle = new();
		[SerializeField] private ApplicableValue<System.Boolean> m_overrideColorTags = new();
		[SerializeField] private ApplicableValue<UnityEngine.Color32> m_faceColor = new();
		[SerializeField] private ApplicableValue<UnityEngine.Color32> m_outlineColor = new();
		[SerializeField] private ApplicableValue<System.Single> m_outlineWidth = new();
		[SerializeField] private ApplicableValue<System.Single> m_fontSize = new();
		[SerializeField] private ApplicableValue<TMPro.FontWeight> m_fontWeight = new();
		[SerializeField] private ApplicableValue<System.Boolean> m_enableAutoSizing = new();
		[SerializeField] private ApplicableValue<System.Single> m_fontSizeMin = new();
		[SerializeField] private ApplicableValue<System.Single> m_fontSizeMax = new();
		[SerializeField] private ApplicableValue<TMPro.FontStyles> m_fontStyle = new();
		[SerializeField] private ApplicableValue<TMPro.HorizontalAlignmentOptions> m_horizontalAlignment = new();
		[SerializeField] private ApplicableValue<TMPro.VerticalAlignmentOptions> m_verticalAlignment = new();
		[SerializeField] private ApplicableValue<TMPro.TextAlignmentOptions> m_alignment = new();
		[SerializeField] private ApplicableValue<System.Single> m_characterSpacing = new();
		[SerializeField] private ApplicableValue<System.Single> m_wordSpacing = new();
		[SerializeField] private ApplicableValue<System.Single> m_lineSpacing = new();
		[SerializeField] private ApplicableValue<System.Single> m_lineSpacingAdjustment = new();
		[SerializeField] private ApplicableValue<System.Single> m_paragraphSpacing = new();
		[SerializeField] private ApplicableValue<System.Single> m_characterWidthAdjustment = new();
		[SerializeField] private ApplicableValue<System.Boolean> m_enableWordWrapping = new();
		[SerializeField] private ApplicableValue<System.Single> m_wordWrappingRatios = new();
		[SerializeField] private ApplicableValue<TMPro.TextOverflowModes> m_overflowMode = new();
		[SerializeField] private ApplicableValue<TMPro.TMP_Text> m_linkedTextComponent = new();
		[SerializeField] private ApplicableValue<System.Boolean> m_enableKerning = new();
		[SerializeField] private ApplicableValue<System.Boolean> m_extraPadding = new();
		[SerializeField] private ApplicableValue<System.Boolean> m_richText = new();
		[SerializeField] private ApplicableValue<System.Boolean> m_parseCtrlCharacters = new();
		[SerializeField] private ApplicableValue<System.Boolean> m_isOverlay = new();
		[SerializeField] private ApplicableValue<System.Boolean> m_isOrthographic = new();
		[SerializeField] private ApplicableValue<System.Boolean> m_enableCulling = new();
		[SerializeField] private ApplicableValue<System.Boolean> m_ignoreVisibility = new();
		[SerializeField] private ApplicableValue<TMPro.TextureMappingOptions> m_horizontalMapping = new();
		[SerializeField] private ApplicableValue<TMPro.TextureMappingOptions> m_verticalMapping = new();
		[SerializeField] private ApplicableValue<System.Single> m_mappingUvLineOffset = new();
		[SerializeField] private ApplicableValue<TMPro.TextRenderFlags> m_renderMode = new();
		[SerializeField] private ApplicableValue<TMPro.VertexSortingOrder> m_geometrySortingOrder = new();
		[SerializeField] private ApplicableValue<System.Boolean> m_isTextObjectScaleStatic = new();
		[SerializeField] private ApplicableValue<System.Boolean> m_vertexBufferAutoSizeReduction = new();
		[SerializeField] private ApplicableValue<System.Int32> m_firstVisibleCharacter = new();
		[SerializeField] private ApplicableValue<System.Int32> m_maxVisibleCharacters = new();
		[SerializeField] private ApplicableValue<System.Int32> m_maxVisibleWords = new();
		[SerializeField] private ApplicableValue<System.Int32> m_maxVisibleLines = new();
		[SerializeField] private ApplicableValue<System.Boolean> m_useMaxVisibleDescender = new();
		[SerializeField] private ApplicableValue<System.Int32> m_pageToDisplay = new();
		[SerializeField] private ApplicableValue<UnityEngine.Vector4> m_margin = new();
		[SerializeField] private ApplicableValue<System.Boolean> m_havePropertiesChanged = new();
		[SerializeField] private ApplicableValue<System.Boolean> m_isUsingLegacyAnimationComponent = new();
		[SerializeField] private ApplicableValue<System.Boolean> m_autoSizeTextContainer = new();
		[SerializeField] private ApplicableValue<System.Boolean> m_isVolumetricText = new();
		[SerializeField] private ApplicableValue<System.Boolean> m_maskable = new();
		[SerializeField] private ApplicableValue<System.Boolean> m_isMaskingGraphic = new();
		[SerializeField] private ApplicableValue<System.Boolean> m_raycastTarget = new();
		[SerializeField] private ApplicableValue<UnityEngine.Vector4> m_raycastPadding = new();
		[SerializeField] private ApplicableValue<UnityEngine.Material> m_material = new();

		public System.Boolean IsRightToLeftText
		{
			get => m_isRightToLeftText.Value;
			set => m_isRightToLeftText.Value = value;
		}

		public TMPro.TMP_FontAsset Font
		{
			get => m_font.Value;
			set => m_font.Value = value;
		}

		public UnityEngine.Material FontSharedMaterial
		{
			get => m_fontSharedMaterial.Value;
			set => m_fontSharedMaterial.Value = value;
		}

		public UnityEngine.Material[] FontSharedMaterials
		{
			get => m_fontSharedMaterials.Value;
			set => m_fontSharedMaterials.Value = value;
		}

		public UnityEngine.Material FontMaterial
		{
			get => m_fontMaterial.Value;
			set => m_fontMaterial.Value = value;
		}

		public UnityEngine.Material[] FontMaterials
		{
			get => m_fontMaterials.Value;
			set => m_fontMaterials.Value = value;
		}

		public UnityEngine.Color Color
		{
			get => m_color.Value;
			set => m_color.Value = value;
		}

		public System.Single Alpha
		{
			get => m_alpha.Value;
			set => m_alpha.Value = value;
		}

		public System.Boolean EnableVertexGradient
		{
			get => m_enableVertexGradient.Value;
			set => m_enableVertexGradient.Value = value;
		}

		public TMPro.VertexGradient ColorGradient
		{
			get => m_colorGradient.Value;
			set => m_colorGradient.Value = value;
		}

		public TMPro.TMP_ColorGradient ColorGradientPreset
		{
			get => m_colorGradientPreset.Value;
			set => m_colorGradientPreset.Value = value;
		}

		public TMPro.TMP_SpriteAsset SpriteAsset
		{
			get => m_spriteAsset.Value;
			set => m_spriteAsset.Value = value;
		}

		public System.Boolean TintAllSprites
		{
			get => m_tintAllSprites.Value;
			set => m_tintAllSprites.Value = value;
		}

		public TMPro.TMP_StyleSheet StyleSheet
		{
			get => m_styleSheet.Value;
			set => m_styleSheet.Value = value;
		}

		public TMPro.TMP_Style TextStyle
		{
			get => m_textStyle.Value;
			set => m_textStyle.Value = value;
		}

		public System.Boolean OverrideColorTags
		{
			get => m_overrideColorTags.Value;
			set => m_overrideColorTags.Value = value;
		}

		public UnityEngine.Color32 FaceColor
		{
			get => m_faceColor.Value;
			set => m_faceColor.Value = value;
		}

		public UnityEngine.Color32 OutlineColor
		{
			get => m_outlineColor.Value;
			set => m_outlineColor.Value = value;
		}

		public System.Single OutlineWidth
		{
			get => m_outlineWidth.Value;
			set => m_outlineWidth.Value = value;
		}

		public System.Single FontSize
		{
			get => m_fontSize.Value;
			set => m_fontSize.Value = value;
		}

		public TMPro.FontWeight FontWeight
		{
			get => m_fontWeight.Value;
			set => m_fontWeight.Value = value;
		}

		public System.Boolean EnableAutoSizing
		{
			get => m_enableAutoSizing.Value;
			set => m_enableAutoSizing.Value = value;
		}

		public System.Single FontSizeMin
		{
			get => m_fontSizeMin.Value;
			set => m_fontSizeMin.Value = value;
		}

		public System.Single FontSizeMax
		{
			get => m_fontSizeMax.Value;
			set => m_fontSizeMax.Value = value;
		}

		public TMPro.FontStyles FontStyle
		{
			get => m_fontStyle.Value;
			set => m_fontStyle.Value = value;
		}

		public TMPro.HorizontalAlignmentOptions HorizontalAlignment
		{
			get => m_horizontalAlignment.Value;
			set => m_horizontalAlignment.Value = value;
		}

		public TMPro.VerticalAlignmentOptions VerticalAlignment
		{
			get => m_verticalAlignment.Value;
			set => m_verticalAlignment.Value = value;
		}

		public TMPro.TextAlignmentOptions Alignment
		{
			get => m_alignment.Value;
			set => m_alignment.Value = value;
		}

		public System.Single CharacterSpacing
		{
			get => m_characterSpacing.Value;
			set => m_characterSpacing.Value = value;
		}

		public System.Single WordSpacing
		{
			get => m_wordSpacing.Value;
			set => m_wordSpacing.Value = value;
		}

		public System.Single LineSpacing
		{
			get => m_lineSpacing.Value;
			set => m_lineSpacing.Value = value;
		}

		public System.Single LineSpacingAdjustment
		{
			get => m_lineSpacingAdjustment.Value;
			set => m_lineSpacingAdjustment.Value = value;
		}

		public System.Single ParagraphSpacing
		{
			get => m_paragraphSpacing.Value;
			set => m_paragraphSpacing.Value = value;
		}

		public System.Single CharacterWidthAdjustment
		{
			get => m_characterWidthAdjustment.Value;
			set => m_characterWidthAdjustment.Value = value;
		}

		public System.Boolean EnableWordWrapping
		{
			get => m_enableWordWrapping.Value;
			set => m_enableWordWrapping.Value = value;
		}

		public System.Single WordWrappingRatios
		{
			get => m_wordWrappingRatios.Value;
			set => m_wordWrappingRatios.Value = value;
		}

		public TMPro.TextOverflowModes OverflowMode
		{
			get => m_overflowMode.Value;
			set => m_overflowMode.Value = value;
		}

		public TMPro.TMP_Text LinkedTextComponent
		{
			get => m_linkedTextComponent.Value;
			set => m_linkedTextComponent.Value = value;
		}

		public System.Boolean EnableKerning
		{
			get => m_enableKerning.Value;
			set => m_enableKerning.Value = value;
		}

		public System.Boolean ExtraPadding
		{
			get => m_extraPadding.Value;
			set => m_extraPadding.Value = value;
		}

		public System.Boolean RichText
		{
			get => m_richText.Value;
			set => m_richText.Value = value;
		}

		public System.Boolean ParseCtrlCharacters
		{
			get => m_parseCtrlCharacters.Value;
			set => m_parseCtrlCharacters.Value = value;
		}

		public System.Boolean IsOverlay
		{
			get => m_isOverlay.Value;
			set => m_isOverlay.Value = value;
		}

		public System.Boolean IsOrthographic
		{
			get => m_isOrthographic.Value;
			set => m_isOrthographic.Value = value;
		}

		public System.Boolean EnableCulling
		{
			get => m_enableCulling.Value;
			set => m_enableCulling.Value = value;
		}

		public System.Boolean IgnoreVisibility
		{
			get => m_ignoreVisibility.Value;
			set => m_ignoreVisibility.Value = value;
		}

		public TMPro.TextureMappingOptions HorizontalMapping
		{
			get => m_horizontalMapping.Value;
			set => m_horizontalMapping.Value = value;
		}

		public TMPro.TextureMappingOptions VerticalMapping
		{
			get => m_verticalMapping.Value;
			set => m_verticalMapping.Value = value;
		}

		public System.Single MappingUvLineOffset
		{
			get => m_mappingUvLineOffset.Value;
			set => m_mappingUvLineOffset.Value = value;
		}

		public TMPro.TextRenderFlags RenderMode
		{
			get => m_renderMode.Value;
			set => m_renderMode.Value = value;
		}

		public TMPro.VertexSortingOrder GeometrySortingOrder
		{
			get => m_geometrySortingOrder.Value;
			set => m_geometrySortingOrder.Value = value;
		}

		public System.Boolean IsTextObjectScaleStatic
		{
			get => m_isTextObjectScaleStatic.Value;
			set => m_isTextObjectScaleStatic.Value = value;
		}

		public System.Boolean VertexBufferAutoSizeReduction
		{
			get => m_vertexBufferAutoSizeReduction.Value;
			set => m_vertexBufferAutoSizeReduction.Value = value;
		}

		public System.Int32 FirstVisibleCharacter
		{
			get => m_firstVisibleCharacter.Value;
			set => m_firstVisibleCharacter.Value = value;
		}

		public System.Int32 MaxVisibleCharacters
		{
			get => m_maxVisibleCharacters.Value;
			set => m_maxVisibleCharacters.Value = value;
		}

		public System.Int32 MaxVisibleWords
		{
			get => m_maxVisibleWords.Value;
			set => m_maxVisibleWords.Value = value;
		}

		public System.Int32 MaxVisibleLines
		{
			get => m_maxVisibleLines.Value;
			set => m_maxVisibleLines.Value = value;
		}

		public System.Boolean UseMaxVisibleDescender
		{
			get => m_useMaxVisibleDescender.Value;
			set => m_useMaxVisibleDescender.Value = value;
		}

		public System.Int32 PageToDisplay
		{
			get => m_pageToDisplay.Value;
			set => m_pageToDisplay.Value = value;
		}

		public UnityEngine.Vector4 Margin
		{
			get => m_margin.Value;
			set => m_margin.Value = value;
		}

		public System.Boolean HavePropertiesChanged
		{
			get => m_havePropertiesChanged.Value;
			set => m_havePropertiesChanged.Value = value;
		}

		public System.Boolean IsUsingLegacyAnimationComponent
		{
			get => m_isUsingLegacyAnimationComponent.Value;
			set => m_isUsingLegacyAnimationComponent.Value = value;
		}

		public System.Boolean AutoSizeTextContainer
		{
			get => m_autoSizeTextContainer.Value;
			set => m_autoSizeTextContainer.Value = value;
		}

		public System.Boolean IsVolumetricText
		{
			get => m_isVolumetricText.Value;
			set => m_isVolumetricText.Value = value;
		}

		public System.Boolean Maskable
		{
			get => m_maskable.Value;
			set => m_maskable.Value = value;
		}

		public System.Boolean IsMaskingGraphic
		{
			get => m_isMaskingGraphic.Value;
			set => m_isMaskingGraphic.Value = value;
		}

		public System.Boolean RaycastTarget
		{
			get => m_raycastTarget.Value;
			set => m_raycastTarget.Value = value;
		}

		public UnityEngine.Vector4 RaycastPadding
		{
			get => m_raycastPadding.Value;
			set => m_raycastPadding.Value = value;
		}

		public UnityEngine.Material Material
		{
			get => m_material.Value;
			set => m_material.Value = value;
		}

	}
}
