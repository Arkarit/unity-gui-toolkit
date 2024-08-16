// Auto-generated, please do not change!
using System;
using UnityEngine;
using GuiToolkit;
using GuiToolkit.Style;

namespace GuiToolkit.Style
{
	[Serializable]
	public class UiStyleTMP_Text : UiAbstractStyle<TMPro.TMP_Text>
	{
		private class ApplicableValueTMP_FontAsset : ApplicableValue<TMPro.TMP_FontAsset> {}
		private class ApplicableValueMaterial : ApplicableValue<UnityEngine.Material> {}
		private class ApplicableValueMaterialArray : ApplicableValue<UnityEngine.Material[]> {}
		private class ApplicableValueColor : ApplicableValue<UnityEngine.Color> {}
		private class ApplicableValueSingle : ApplicableValue<System.Single> {}
		private class ApplicableValueBoolean : ApplicableValue<System.Boolean> {}
		private class ApplicableValueVertexGradient : ApplicableValue<TMPro.VertexGradient> {}
		private class ApplicableValueTMP_ColorGradient : ApplicableValue<TMPro.TMP_ColorGradient> {}
		private class ApplicableValueTMP_SpriteAsset : ApplicableValue<TMPro.TMP_SpriteAsset> {}
		private class ApplicableValueTMP_StyleSheet : ApplicableValue<TMPro.TMP_StyleSheet> {}
		private class ApplicableValueTMP_Style : ApplicableValue<TMPro.TMP_Style> {}
		private class ApplicableValueColor32 : ApplicableValue<UnityEngine.Color32> {}
		private class ApplicableValueFontWeight : ApplicableValue<TMPro.FontWeight> {}
		private class ApplicableValueFontStyles : ApplicableValue<TMPro.FontStyles> {}
		private class ApplicableValueHorizontalAlignmentOptions : ApplicableValue<TMPro.HorizontalAlignmentOptions> {}
		private class ApplicableValueVerticalAlignmentOptions : ApplicableValue<TMPro.VerticalAlignmentOptions> {}
		private class ApplicableValueTextAlignmentOptions : ApplicableValue<TMPro.TextAlignmentOptions> {}
		private class ApplicableValueVector4 : ApplicableValue<UnityEngine.Vector4> {}

		[SerializeReference] private ApplicableValueTMP_FontAsset m_font = new();
		[SerializeReference] private ApplicableValueMaterial m_fontSharedMaterial = new();
		[SerializeReference] private ApplicableValueMaterialArray m_fontSharedMaterials = new();
		[SerializeReference] private ApplicableValueMaterial m_fontMaterial = new();
		[SerializeReference] private ApplicableValueMaterialArray m_fontMaterials = new();
		[SerializeReference] private ApplicableValueColor m_color = new();
		[SerializeReference] private ApplicableValueSingle m_alpha = new();
		[SerializeReference] private ApplicableValueBoolean m_enableVertexGradient = new();
		[SerializeReference] private ApplicableValueVertexGradient m_colorGradient = new();
		[SerializeReference] private ApplicableValueTMP_ColorGradient m_colorGradientPreset = new();
		[SerializeReference] private ApplicableValueTMP_SpriteAsset m_spriteAsset = new();
		[SerializeReference] private ApplicableValueTMP_StyleSheet m_styleSheet = new();
		[SerializeReference] private ApplicableValueTMP_Style m_textStyle = new();
		[SerializeReference] private ApplicableValueBoolean m_overrideColorTags = new();
		[SerializeReference] private ApplicableValueColor32 m_faceColor = new();
		[SerializeReference] private ApplicableValueColor32 m_outlineColor = new();
		[SerializeReference] private ApplicableValueSingle m_outlineWidth = new();
		[SerializeReference] private ApplicableValueSingle m_fontSize = new();
		[SerializeReference] private ApplicableValueFontWeight m_fontWeight = new();
		[SerializeReference] private ApplicableValueSingle m_fontSizeMin = new();
		[SerializeReference] private ApplicableValueSingle m_fontSizeMax = new();
		[SerializeReference] private ApplicableValueFontStyles m_fontStyle = new();
		[SerializeReference] private ApplicableValueHorizontalAlignmentOptions m_horizontalAlignment = new();
		[SerializeReference] private ApplicableValueVerticalAlignmentOptions m_verticalAlignment = new();
		[SerializeReference] private ApplicableValueTextAlignmentOptions m_alignment = new();
		[SerializeReference] private ApplicableValueSingle m_characterSpacing = new();
		[SerializeReference] private ApplicableValueSingle m_wordSpacing = new();
		[SerializeReference] private ApplicableValueSingle m_lineSpacing = new();
		[SerializeReference] private ApplicableValueSingle m_lineSpacingAdjustment = new();
		[SerializeReference] private ApplicableValueSingle m_paragraphSpacing = new();
		[SerializeReference] private ApplicableValueSingle m_characterWidthAdjustment = new();
		[SerializeReference] private ApplicableValueBoolean m_extraPadding = new();
		[SerializeReference] private ApplicableValueVector4 m_margin = new();
		[SerializeReference] private ApplicableValueMaterial m_material = new();

		public ApplicableValue<TMPro.TMP_FontAsset> Font => m_font;
		public ApplicableValue<UnityEngine.Material> FontSharedMaterial => m_fontSharedMaterial;
		public ApplicableValue<UnityEngine.Material[]> FontSharedMaterials => m_fontSharedMaterials;
		public ApplicableValue<UnityEngine.Material> FontMaterial => m_fontMaterial;
		public ApplicableValue<UnityEngine.Material[]> FontMaterials => m_fontMaterials;
		public ApplicableValue<UnityEngine.Color> Color => m_color;
		public ApplicableValue<System.Single> Alpha => m_alpha;
		public ApplicableValue<System.Boolean> EnableVertexGradient => m_enableVertexGradient;
		public ApplicableValue<TMPro.VertexGradient> ColorGradient => m_colorGradient;
		public ApplicableValue<TMPro.TMP_ColorGradient> ColorGradientPreset => m_colorGradientPreset;
		public ApplicableValue<TMPro.TMP_SpriteAsset> SpriteAsset => m_spriteAsset;
		public ApplicableValue<TMPro.TMP_StyleSheet> StyleSheet => m_styleSheet;
		public ApplicableValue<TMPro.TMP_Style> TextStyle => m_textStyle;
		public ApplicableValue<System.Boolean> OverrideColorTags => m_overrideColorTags;
		public ApplicableValue<UnityEngine.Color32> FaceColor => m_faceColor;
		public ApplicableValue<UnityEngine.Color32> OutlineColor => m_outlineColor;
		public ApplicableValue<System.Single> OutlineWidth => m_outlineWidth;
		public ApplicableValue<System.Single> FontSize => m_fontSize;
		public ApplicableValue<TMPro.FontWeight> FontWeight => m_fontWeight;
		public ApplicableValue<System.Single> FontSizeMin => m_fontSizeMin;
		public ApplicableValue<System.Single> FontSizeMax => m_fontSizeMax;
		public ApplicableValue<TMPro.FontStyles> FontStyle => m_fontStyle;
		public ApplicableValue<TMPro.HorizontalAlignmentOptions> HorizontalAlignment => m_horizontalAlignment;
		public ApplicableValue<TMPro.VerticalAlignmentOptions> VerticalAlignment => m_verticalAlignment;
		public ApplicableValue<TMPro.TextAlignmentOptions> Alignment => m_alignment;
		public ApplicableValue<System.Single> CharacterSpacing => m_characterSpacing;
		public ApplicableValue<System.Single> WordSpacing => m_wordSpacing;
		public ApplicableValue<System.Single> LineSpacing => m_lineSpacing;
		public ApplicableValue<System.Single> LineSpacingAdjustment => m_lineSpacingAdjustment;
		public ApplicableValue<System.Single> ParagraphSpacing => m_paragraphSpacing;
		public ApplicableValue<System.Single> CharacterWidthAdjustment => m_characterWidthAdjustment;
		public ApplicableValue<System.Boolean> ExtraPadding => m_extraPadding;
		public ApplicableValue<UnityEngine.Vector4> Margin => m_margin;
		public ApplicableValue<UnityEngine.Material> Material => m_material;

		public override UiAbstractStyleBase Clone() => new UiStyleTMP_Text()
		{
			m_font = (ApplicableValueTMP_FontAsset) m_font.Clone(),
			m_fontSharedMaterial = (ApplicableValueMaterial) m_fontSharedMaterial.Clone(),
			m_fontSharedMaterials = (ApplicableValueMaterialArray) m_fontSharedMaterials.Clone(),
			m_fontMaterial = (ApplicableValueMaterial) m_fontMaterial.Clone(),
			m_fontMaterials = (ApplicableValueMaterialArray) m_fontMaterials.Clone(),
			m_color = (ApplicableValueColor) m_color.Clone(),
			m_alpha = (ApplicableValueSingle) m_alpha.Clone(),
			m_enableVertexGradient = (ApplicableValueBoolean) m_enableVertexGradient.Clone(),
			m_colorGradient = (ApplicableValueVertexGradient) m_colorGradient.Clone(),
			m_colorGradientPreset = (ApplicableValueTMP_ColorGradient) m_colorGradientPreset.Clone(),
			m_spriteAsset = (ApplicableValueTMP_SpriteAsset) m_spriteAsset.Clone(),
			m_styleSheet = (ApplicableValueTMP_StyleSheet) m_styleSheet.Clone(),
			m_textStyle = (ApplicableValueTMP_Style) m_textStyle.Clone(),
			m_overrideColorTags = (ApplicableValueBoolean) m_overrideColorTags.Clone(),
			m_faceColor = (ApplicableValueColor32) m_faceColor.Clone(),
			m_outlineColor = (ApplicableValueColor32) m_outlineColor.Clone(),
			m_outlineWidth = (ApplicableValueSingle) m_outlineWidth.Clone(),
			m_fontSize = (ApplicableValueSingle) m_fontSize.Clone(),
			m_fontWeight = (ApplicableValueFontWeight) m_fontWeight.Clone(),
			m_fontSizeMin = (ApplicableValueSingle) m_fontSizeMin.Clone(),
			m_fontSizeMax = (ApplicableValueSingle) m_fontSizeMax.Clone(),
			m_fontStyle = (ApplicableValueFontStyles) m_fontStyle.Clone(),
			m_horizontalAlignment = (ApplicableValueHorizontalAlignmentOptions) m_horizontalAlignment.Clone(),
			m_verticalAlignment = (ApplicableValueVerticalAlignmentOptions) m_verticalAlignment.Clone(),
			m_alignment = (ApplicableValueTextAlignmentOptions) m_alignment.Clone(),
			m_characterSpacing = (ApplicableValueSingle) m_characterSpacing.Clone(),
			m_wordSpacing = (ApplicableValueSingle) m_wordSpacing.Clone(),
			m_lineSpacing = (ApplicableValueSingle) m_lineSpacing.Clone(),
			m_lineSpacingAdjustment = (ApplicableValueSingle) m_lineSpacingAdjustment.Clone(),
			m_paragraphSpacing = (ApplicableValueSingle) m_paragraphSpacing.Clone(),
			m_characterWidthAdjustment = (ApplicableValueSingle) m_characterWidthAdjustment.Clone(),
			m_extraPadding = (ApplicableValueBoolean) m_extraPadding.Clone(),
			m_margin = (ApplicableValueVector4) m_margin.Clone(),
			m_material = (ApplicableValueMaterial) m_material.Clone(),
		};
	}
}
