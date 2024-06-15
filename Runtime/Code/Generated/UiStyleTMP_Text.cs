// Auto-generated, please do not change!
using System;
using UnityEngine;
using GuiToolkit.Style;
using System.Collections.Generic;

namespace GuiToolkit.Style
{
	[Serializable]
	public class UiStyleTMP_Text : UiAbstractStyle<TMPro.TMP_Text>
	{
		private readonly List<ApplicableValueBase> m_values = new();
		private readonly List<object> m_defaultValues = new();

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

		public override List<ApplicableValueBase> Values
		{
			get
			{
				if (m_values.Count == 0)
				{
					m_values.Add(m_font);
					m_values.Add(m_fontSharedMaterial);
					m_values.Add(m_fontSharedMaterials);
					m_values.Add(m_fontMaterial);
					m_values.Add(m_fontMaterials);
					m_values.Add(m_color);
					m_values.Add(m_alpha);
					m_values.Add(m_enableVertexGradient);
					m_values.Add(m_colorGradient);
					m_values.Add(m_colorGradientPreset);
					m_values.Add(m_spriteAsset);
					m_values.Add(m_styleSheet);
					m_values.Add(m_textStyle);
					m_values.Add(m_overrideColorTags);
					m_values.Add(m_faceColor);
					m_values.Add(m_outlineColor);
					m_values.Add(m_outlineWidth);
					m_values.Add(m_fontSize);
					m_values.Add(m_fontWeight);
					m_values.Add(m_fontSizeMin);
					m_values.Add(m_fontSizeMax);
					m_values.Add(m_fontStyle);
					m_values.Add(m_horizontalAlignment);
					m_values.Add(m_verticalAlignment);
					m_values.Add(m_alignment);
					m_values.Add(m_characterSpacing);
					m_values.Add(m_wordSpacing);
					m_values.Add(m_lineSpacing);
					m_values.Add(m_lineSpacingAdjustment);
					m_values.Add(m_paragraphSpacing);
					m_values.Add(m_characterWidthAdjustment);
					m_values.Add(m_extraPadding);
					m_values.Add(m_margin);
					m_values.Add(m_material);
				}

				return m_values;
			}
		}

		public override List<object> DefaultValues
		{
			get
			{
				if (m_defaultValues.Count == 0)
				{
					var defaultComponent = this.GetOrCreateComponent<TMPro.TextMeshPro>();

					m_defaultValues.Add(defaultComponent.font);
					m_defaultValues.Add(defaultComponent.fontSharedMaterial);
					m_defaultValues.Add(defaultComponent.fontSharedMaterials);
					m_defaultValues.Add(defaultComponent.fontMaterial);
					m_defaultValues.Add(defaultComponent.fontMaterials);
					m_defaultValues.Add(defaultComponent.color);
					m_defaultValues.Add(defaultComponent.alpha);
					m_defaultValues.Add(defaultComponent.enableVertexGradient);
					m_defaultValues.Add(defaultComponent.colorGradient);
					m_defaultValues.Add(defaultComponent.colorGradientPreset);
					m_defaultValues.Add(defaultComponent.spriteAsset);
					m_defaultValues.Add(defaultComponent.styleSheet);
					m_defaultValues.Add(defaultComponent.textStyle);
					m_defaultValues.Add(defaultComponent.overrideColorTags);
					m_defaultValues.Add(defaultComponent.faceColor);
					m_defaultValues.Add(defaultComponent.outlineColor);
					m_defaultValues.Add(defaultComponent.outlineWidth);
					m_defaultValues.Add(defaultComponent.fontSize);
					m_defaultValues.Add(defaultComponent.fontWeight);
					m_defaultValues.Add(defaultComponent.fontSizeMin);
					m_defaultValues.Add(defaultComponent.fontSizeMax);
					m_defaultValues.Add(defaultComponent.fontStyle);
					m_defaultValues.Add(defaultComponent.horizontalAlignment);
					m_defaultValues.Add(defaultComponent.verticalAlignment);
					m_defaultValues.Add(defaultComponent.alignment);
					m_defaultValues.Add(defaultComponent.characterSpacing);
					m_defaultValues.Add(defaultComponent.wordSpacing);
					m_defaultValues.Add(defaultComponent.lineSpacing);
					m_defaultValues.Add(defaultComponent.lineSpacingAdjustment);
					m_defaultValues.Add(defaultComponent.paragraphSpacing);
					m_defaultValues.Add(defaultComponent.characterWidthAdjustment);
					m_defaultValues.Add(defaultComponent.extraPadding);
					m_defaultValues.Add(defaultComponent.margin);
					m_defaultValues.Add(defaultComponent.material);
				}

				return m_defaultValues;
			}
		}

	}
}
