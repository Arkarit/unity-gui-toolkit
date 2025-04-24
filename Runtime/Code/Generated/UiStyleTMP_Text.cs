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
		public UiStyleTMP_Text(UiStyleConfig _styleConfig, string _name)
		{
			StyleConfig = _styleConfig;
			Name = _name;
		}

		private class ApplicableValueString : ApplicableValue<System.String> {}
		private class ApplicableValueTMP_FontAsset : ApplicableValue<TMPro.TMP_FontAsset> {}
		private class ApplicableValueMaterial : ApplicableValue<UnityEngine.Material> {}
		private class ApplicableValueMaterialArray : ApplicableValue<UnityEngine.Material[]> {}
		private class ApplicableValueColor : ApplicableValue<UnityEngine.Color> {}
		private class ApplicableValueSingle : ApplicableValue<System.Single> {}
		private class ApplicableValueBoolean : ApplicableValue<System.Boolean> {}
		private class ApplicableValueVertexGradient : ApplicableValue<TMPro.VertexGradient> {}
		private class ApplicableValueTMP_SpriteAsset : ApplicableValue<TMPro.TMP_SpriteAsset> {}
		private class ApplicableValueTMP_StyleSheet : ApplicableValue<TMPro.TMP_StyleSheet> {}
		private class ApplicableValueTMP_Style : ApplicableValue<TMPro.TMP_Style> {}
		private class ApplicableValueColor32 : ApplicableValue<UnityEngine.Color32> {}
		private class ApplicableValueFontWeight : ApplicableValue<TMPro.FontWeight> {}
		private class ApplicableValueFontStyles : ApplicableValue<TMPro.FontStyles> {}
		private class ApplicableValueTextAlignmentOptions : ApplicableValue<TMPro.TextAlignmentOptions> {}
		private class ApplicableValueTextOverflowModes : ApplicableValue<TMPro.TextOverflowModes> {}
		private class ApplicableValueVector4 : ApplicableValue<UnityEngine.Vector4> {}

		protected override ApplicableValueBase[] GetValueArray()
		{
			return new ApplicableValueBase[]
			{
				Text,
				Font,
				FontSharedMaterial,
				FontSharedMaterials,
				Color,
				Alpha,
				EnableVertexGradient,
				ColorGradient,
				SpriteAsset,
				TintAllSprites,
				StyleSheet,
				TextStyle,
				OutlineColor,
				OutlineWidth,
				FontSize,
				FontWeight,
				EnableAutoSizing,
				FontSizeMin,
				FontSizeMax,
				FontStyle,
				Alignment,
				CharacterSpacing,
				WordSpacing,
				LineSpacing,
				LineSpacingAdjustment,
				ParagraphSpacing,
				CharacterWidthAdjustment,
				OverflowMode,
				EnableKerning,
				ExtraPadding,
				RichText,
				Margin,
				Enabled,
			};
		}

		[SerializeReference] private ApplicableValueString m_text = new();
		[SerializeReference] private ApplicableValueTMP_FontAsset m_font = new();
		[SerializeReference] private ApplicableValueMaterial m_fontSharedMaterial = new();
		[SerializeReference] private ApplicableValueMaterialArray m_fontSharedMaterials = new();
		[SerializeReference] private ApplicableValueColor m_color = new();
		[SerializeReference] private ApplicableValueSingle m_alpha = new();
		[SerializeReference] private ApplicableValueBoolean m_enableVertexGradient = new();
		[SerializeReference] private ApplicableValueVertexGradient m_colorGradient = new();
		[SerializeReference] private ApplicableValueTMP_SpriteAsset m_spriteAsset = new();
		[SerializeReference] private ApplicableValueBoolean m_tintAllSprites = new();
		[SerializeReference] private ApplicableValueTMP_StyleSheet m_styleSheet = new();
		[SerializeReference] private ApplicableValueTMP_Style m_textStyle = new();
		[SerializeReference] private ApplicableValueColor32 m_outlineColor = new();
		[SerializeReference] private ApplicableValueSingle m_outlineWidth = new();
		[SerializeReference] private ApplicableValueSingle m_fontSize = new();
		[SerializeReference] private ApplicableValueFontWeight m_fontWeight = new();
		[SerializeReference] private ApplicableValueBoolean m_enableAutoSizing = new();
		[SerializeReference] private ApplicableValueSingle m_fontSizeMin = new();
		[SerializeReference] private ApplicableValueSingle m_fontSizeMax = new();
		[SerializeReference] private ApplicableValueFontStyles m_fontStyle = new();
		[SerializeReference] private ApplicableValueTextAlignmentOptions m_alignment = new();
		[SerializeReference] private ApplicableValueSingle m_characterSpacing = new();
		[SerializeReference] private ApplicableValueSingle m_wordSpacing = new();
		[SerializeReference] private ApplicableValueSingle m_lineSpacing = new();
		[SerializeReference] private ApplicableValueSingle m_lineSpacingAdjustment = new();
		[SerializeReference] private ApplicableValueSingle m_paragraphSpacing = new();
		[SerializeReference] private ApplicableValueSingle m_characterWidthAdjustment = new();
		[SerializeReference] private ApplicableValueTextOverflowModes m_overflowMode = new();
		[SerializeReference] private ApplicableValueBoolean m_enableKerning = new();
		[SerializeReference] private ApplicableValueBoolean m_extraPadding = new();
		[SerializeReference] private ApplicableValueBoolean m_richText = new();
		[SerializeReference] private ApplicableValueVector4 m_margin = new();
		[SerializeReference] private ApplicableValueBoolean m_enabled = new();

		public ApplicableValue<System.String> Text
		{
			get
			{
				if (m_text == null)
					m_text = new ApplicableValueString();
				return m_text;
			}
		}

		public ApplicableValue<TMPro.TMP_FontAsset> Font
		{
			get
			{
				if (m_font == null)
					m_font = new ApplicableValueTMP_FontAsset();
				return m_font;
			}
		}

		public ApplicableValue<UnityEngine.Material> FontSharedMaterial
		{
			get
			{
				if (m_fontSharedMaterial == null)
					m_fontSharedMaterial = new ApplicableValueMaterial();
				return m_fontSharedMaterial;
			}
		}

		public ApplicableValue<UnityEngine.Material[]> FontSharedMaterials
		{
			get
			{
				if (m_fontSharedMaterials == null)
					m_fontSharedMaterials = new ApplicableValueMaterialArray();
				return m_fontSharedMaterials;
			}
		}

		public ApplicableValue<UnityEngine.Color> Color
		{
			get
			{
				if (m_color == null)
					m_color = new ApplicableValueColor();
				return m_color;
			}
		}

		public ApplicableValue<System.Single> Alpha
		{
			get
			{
				if (m_alpha == null)
					m_alpha = new ApplicableValueSingle();
				return m_alpha;
			}
		}

		public ApplicableValue<System.Boolean> EnableVertexGradient
		{
			get
			{
				if (m_enableVertexGradient == null)
					m_enableVertexGradient = new ApplicableValueBoolean();
				return m_enableVertexGradient;
			}
		}

		public ApplicableValue<TMPro.VertexGradient> ColorGradient
		{
			get
			{
				if (m_colorGradient == null)
					m_colorGradient = new ApplicableValueVertexGradient();
				return m_colorGradient;
			}
		}

		public ApplicableValue<TMPro.TMP_SpriteAsset> SpriteAsset
		{
			get
			{
				if (m_spriteAsset == null)
					m_spriteAsset = new ApplicableValueTMP_SpriteAsset();
				return m_spriteAsset;
			}
		}

		public ApplicableValue<System.Boolean> TintAllSprites
		{
			get
			{
				if (m_tintAllSprites == null)
					m_tintAllSprites = new ApplicableValueBoolean();
				return m_tintAllSprites;
			}
		}

		public ApplicableValue<TMPro.TMP_StyleSheet> StyleSheet
		{
			get
			{
				if (m_styleSheet == null)
					m_styleSheet = new ApplicableValueTMP_StyleSheet();
				return m_styleSheet;
			}
		}

		public ApplicableValue<TMPro.TMP_Style> TextStyle
		{
			get
			{
				if (m_textStyle == null)
					m_textStyle = new ApplicableValueTMP_Style();
				return m_textStyle;
			}
		}

		public ApplicableValue<UnityEngine.Color32> OutlineColor
		{
			get
			{
				if (m_outlineColor == null)
					m_outlineColor = new ApplicableValueColor32();
				return m_outlineColor;
			}
		}

		public ApplicableValue<System.Single> OutlineWidth
		{
			get
			{
				if (m_outlineWidth == null)
					m_outlineWidth = new ApplicableValueSingle();
				return m_outlineWidth;
			}
		}

		public ApplicableValue<System.Single> FontSize
		{
			get
			{
				if (m_fontSize == null)
					m_fontSize = new ApplicableValueSingle();
				return m_fontSize;
			}
		}

		public ApplicableValue<TMPro.FontWeight> FontWeight
		{
			get
			{
				if (m_fontWeight == null)
					m_fontWeight = new ApplicableValueFontWeight();
				return m_fontWeight;
			}
		}

		public ApplicableValue<System.Boolean> EnableAutoSizing
		{
			get
			{
				if (m_enableAutoSizing == null)
					m_enableAutoSizing = new ApplicableValueBoolean();
				return m_enableAutoSizing;
			}
		}

		public ApplicableValue<System.Single> FontSizeMin
		{
			get
			{
				if (m_fontSizeMin == null)
					m_fontSizeMin = new ApplicableValueSingle();
				return m_fontSizeMin;
			}
		}

		public ApplicableValue<System.Single> FontSizeMax
		{
			get
			{
				if (m_fontSizeMax == null)
					m_fontSizeMax = new ApplicableValueSingle();
				return m_fontSizeMax;
			}
		}

		public ApplicableValue<TMPro.FontStyles> FontStyle
		{
			get
			{
				if (m_fontStyle == null)
					m_fontStyle = new ApplicableValueFontStyles();
				return m_fontStyle;
			}
		}

		public ApplicableValue<TMPro.TextAlignmentOptions> Alignment
		{
			get
			{
				if (m_alignment == null)
					m_alignment = new ApplicableValueTextAlignmentOptions();
				return m_alignment;
			}
		}

		public ApplicableValue<System.Single> CharacterSpacing
		{
			get
			{
				if (m_characterSpacing == null)
					m_characterSpacing = new ApplicableValueSingle();
				return m_characterSpacing;
			}
		}

		public ApplicableValue<System.Single> WordSpacing
		{
			get
			{
				if (m_wordSpacing == null)
					m_wordSpacing = new ApplicableValueSingle();
				return m_wordSpacing;
			}
		}

		public ApplicableValue<System.Single> LineSpacing
		{
			get
			{
				if (m_lineSpacing == null)
					m_lineSpacing = new ApplicableValueSingle();
				return m_lineSpacing;
			}
		}

		public ApplicableValue<System.Single> LineSpacingAdjustment
		{
			get
			{
				if (m_lineSpacingAdjustment == null)
					m_lineSpacingAdjustment = new ApplicableValueSingle();
				return m_lineSpacingAdjustment;
			}
		}

		public ApplicableValue<System.Single> ParagraphSpacing
		{
			get
			{
				if (m_paragraphSpacing == null)
					m_paragraphSpacing = new ApplicableValueSingle();
				return m_paragraphSpacing;
			}
		}

		public ApplicableValue<System.Single> CharacterWidthAdjustment
		{
			get
			{
				if (m_characterWidthAdjustment == null)
					m_characterWidthAdjustment = new ApplicableValueSingle();
				return m_characterWidthAdjustment;
			}
		}

		public ApplicableValue<TMPro.TextOverflowModes> OverflowMode
		{
			get
			{
				if (m_overflowMode == null)
					m_overflowMode = new ApplicableValueTextOverflowModes();
				return m_overflowMode;
			}
		}

		public ApplicableValue<System.Boolean> EnableKerning
		{
			get
			{
				if (m_enableKerning == null)
					m_enableKerning = new ApplicableValueBoolean();
				return m_enableKerning;
			}
		}

		public ApplicableValue<System.Boolean> ExtraPadding
		{
			get
			{
				if (m_extraPadding == null)
					m_extraPadding = new ApplicableValueBoolean();
				return m_extraPadding;
			}
		}

		public ApplicableValue<System.Boolean> RichText
		{
			get
			{
				if (m_richText == null)
					m_richText = new ApplicableValueBoolean();
				return m_richText;
			}
		}

		public ApplicableValue<UnityEngine.Vector4> Margin
		{
			get
			{
				if (m_margin == null)
					m_margin = new ApplicableValueVector4();
				return m_margin;
			}
		}

		public ApplicableValue<System.Boolean> Enabled
		{
			get
			{
				if (m_enabled == null)
					m_enabled = new ApplicableValueBoolean();
				return m_enabled;
			}
		}

	}
}
