// Auto-generated, please do not change!
using System;
using System.Collections.Generic;
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

		private class ApplicableValueTextAlignmentOptions : ApplicableValue<TMPro.TextAlignmentOptions> {}
		private class ApplicableValueSingle : ApplicableValue<System.Single> {}
		private class ApplicableValueColor : ApplicableValue<UnityEngine.Color> {}
		private class ApplicableValueVertexGradient : ApplicableValue<TMPro.VertexGradient> {}
		private class ApplicableValueBoolean : ApplicableValue<System.Boolean> {}
		private class ApplicableValueTMP_FontAsset : ApplicableValue<TMPro.TMP_FontAsset> {}
		private class ApplicableValueListUnityEngineTextCoreOTL_FeatureTag : ApplicableValue<System.Collections.Generic.List<UnityEngine.TextCore.OTL_FeatureTag>> {}
		private class ApplicableValueMaterial : ApplicableValue<UnityEngine.Material> {}
		private class ApplicableValueMaterialArray : ApplicableValue<UnityEngine.Material[]> {}
		private class ApplicableValueFontStyles : ApplicableValue<TMPro.FontStyles> {}
		private class ApplicableValueFontWeight : ApplicableValue<TMPro.FontWeight> {}
		private class ApplicableValueVector4 : ApplicableValue<UnityEngine.Vector4> {}
		private class ApplicableValueColor32 : ApplicableValue<UnityEngine.Color32> {}
		private class ApplicableValueTextOverflowModes : ApplicableValue<TMPro.TextOverflowModes> {}
		private class ApplicableValueTMP_SpriteAsset : ApplicableValue<TMPro.TMP_SpriteAsset> {}
		private class ApplicableValueTMP_StyleSheet : ApplicableValue<TMPro.TMP_StyleSheet> {}
		private class ApplicableValueString : ApplicableValue<System.String> {}
		private class ApplicableValueTMP_Style : ApplicableValue<TMPro.TMP_Style> {}
		private class ApplicableValueTextWrappingModes : ApplicableValue<TMPro.TextWrappingModes> {}

		protected override ApplicableValueBase[] GetValueArray()
		{
			return new ApplicableValueBase[]
			{
				Alignment,
				Alpha,
				CharacterSpacing,
				CharacterWidthAdjustment,
				Color,
				ColorGradient,
				EmojiFallbackSupport,
				EnableAutoSizing,
				Enabled,
				EnableKerning,
				EnableVertexGradient,
				ExtraPadding,
				Font,
				FontFeatures,
				FontSharedMaterial,
				FontSharedMaterials,
				FontSize,
				FontSizeMax,
				FontSizeMin,
				FontStyle,
				FontWeight,
				LineSpacing,
				LineSpacingAdjustment,
				Margin,
				OutlineColor,
				OutlineWidth,
				OverflowMode,
				ParagraphSpacing,
				RichText,
				SpriteAsset,
				StyleSheet,
				Text,
				TextStyle,
				TextWrappingMode,
				WordSpacing,
			};
		}

#if UNITY_EDITOR
		public override ValueInfo[] GetValueInfoArray()
		{
			return new ValueInfo[]
			{
				new ValueInfo()
				{
					GetterName = "Alignment",
					GetterType = typeof(ApplicableValueTextAlignmentOptions),
					Value = Alignment,
				},
				new ValueInfo()
				{
					GetterName = "Alpha",
					GetterType = typeof(ApplicableValueSingle),
					Value = Alpha,
				},
				new ValueInfo()
				{
					GetterName = "CharacterSpacing",
					GetterType = typeof(ApplicableValueSingle),
					Value = CharacterSpacing,
				},
				new ValueInfo()
				{
					GetterName = "CharacterWidthAdjustment",
					GetterType = typeof(ApplicableValueSingle),
					Value = CharacterWidthAdjustment,
				},
				new ValueInfo()
				{
					GetterName = "Color",
					GetterType = typeof(ApplicableValueColor),
					Value = Color,
				},
				new ValueInfo()
				{
					GetterName = "ColorGradient",
					GetterType = typeof(ApplicableValueVertexGradient),
					Value = ColorGradient,
				},
				new ValueInfo()
				{
					GetterName = "EmojiFallbackSupport",
					GetterType = typeof(ApplicableValueBoolean),
					Value = EmojiFallbackSupport,
				},
				new ValueInfo()
				{
					GetterName = "EnableAutoSizing",
					GetterType = typeof(ApplicableValueBoolean),
					Value = EnableAutoSizing,
				},
				new ValueInfo()
				{
					GetterName = "Enabled",
					GetterType = typeof(ApplicableValueBoolean),
					Value = Enabled,
				},
				new ValueInfo()
				{
					GetterName = "EnableKerning",
					GetterType = typeof(ApplicableValueBoolean),
					Value = EnableKerning,
				},
				new ValueInfo()
				{
					GetterName = "EnableVertexGradient",
					GetterType = typeof(ApplicableValueBoolean),
					Value = EnableVertexGradient,
				},
				new ValueInfo()
				{
					GetterName = "ExtraPadding",
					GetterType = typeof(ApplicableValueBoolean),
					Value = ExtraPadding,
				},
				new ValueInfo()
				{
					GetterName = "Font",
					GetterType = typeof(ApplicableValueTMP_FontAsset),
					Value = Font,
				},
				new ValueInfo()
				{
					GetterName = "FontFeatures",
					GetterType = typeof(ApplicableValueListUnityEngineTextCoreOTL_FeatureTag),
					Value = FontFeatures,
				},
				new ValueInfo()
				{
					GetterName = "FontSharedMaterial",
					GetterType = typeof(ApplicableValueMaterial),
					Value = FontSharedMaterial,
				},
				new ValueInfo()
				{
					GetterName = "FontSharedMaterials",
					GetterType = typeof(ApplicableValueMaterialArray),
					Value = FontSharedMaterials,
				},
				new ValueInfo()
				{
					GetterName = "FontSize",
					GetterType = typeof(ApplicableValueSingle),
					Value = FontSize,
				},
				new ValueInfo()
				{
					GetterName = "FontSizeMax",
					GetterType = typeof(ApplicableValueSingle),
					Value = FontSizeMax,
				},
				new ValueInfo()
				{
					GetterName = "FontSizeMin",
					GetterType = typeof(ApplicableValueSingle),
					Value = FontSizeMin,
				},
				new ValueInfo()
				{
					GetterName = "FontStyle",
					GetterType = typeof(ApplicableValueFontStyles),
					Value = FontStyle,
				},
				new ValueInfo()
				{
					GetterName = "FontWeight",
					GetterType = typeof(ApplicableValueFontWeight),
					Value = FontWeight,
				},
				new ValueInfo()
				{
					GetterName = "LineSpacing",
					GetterType = typeof(ApplicableValueSingle),
					Value = LineSpacing,
				},
				new ValueInfo()
				{
					GetterName = "LineSpacingAdjustment",
					GetterType = typeof(ApplicableValueSingle),
					Value = LineSpacingAdjustment,
				},
				new ValueInfo()
				{
					GetterName = "Margin",
					GetterType = typeof(ApplicableValueVector4),
					Value = Margin,
				},
				new ValueInfo()
				{
					GetterName = "OutlineColor",
					GetterType = typeof(ApplicableValueColor32),
					Value = OutlineColor,
				},
				new ValueInfo()
				{
					GetterName = "OutlineWidth",
					GetterType = typeof(ApplicableValueSingle),
					Value = OutlineWidth,
				},
				new ValueInfo()
				{
					GetterName = "OverflowMode",
					GetterType = typeof(ApplicableValueTextOverflowModes),
					Value = OverflowMode,
				},
				new ValueInfo()
				{
					GetterName = "ParagraphSpacing",
					GetterType = typeof(ApplicableValueSingle),
					Value = ParagraphSpacing,
				},
				new ValueInfo()
				{
					GetterName = "RichText",
					GetterType = typeof(ApplicableValueBoolean),
					Value = RichText,
				},
				new ValueInfo()
				{
					GetterName = "SpriteAsset",
					GetterType = typeof(ApplicableValueTMP_SpriteAsset),
					Value = SpriteAsset,
				},
				new ValueInfo()
				{
					GetterName = "StyleSheet",
					GetterType = typeof(ApplicableValueTMP_StyleSheet),
					Value = StyleSheet,
				},
				new ValueInfo()
				{
					GetterName = "Text",
					GetterType = typeof(ApplicableValueString),
					Value = Text,
				},
				new ValueInfo()
				{
					GetterName = "TextStyle",
					GetterType = typeof(ApplicableValueTMP_Style),
					Value = TextStyle,
				},
				new ValueInfo()
				{
					GetterName = "TextWrappingMode",
					GetterType = typeof(ApplicableValueTextWrappingModes),
					Value = TextWrappingMode,
				},
				new ValueInfo()
				{
					GetterName = "WordSpacing",
					GetterType = typeof(ApplicableValueSingle),
					Value = WordSpacing,
				},
			};
		}
#endif

		[SerializeReference] private ApplicableValueTextAlignmentOptions m_alignment = new();
		[SerializeReference] private ApplicableValueSingle m_alpha = new();
		[SerializeReference] private ApplicableValueSingle m_characterSpacing = new();
		[SerializeReference] private ApplicableValueSingle m_characterWidthAdjustment = new();
		[SerializeReference] private ApplicableValueColor m_color = new();
		[SerializeReference] private ApplicableValueVertexGradient m_colorGradient = new();
		[SerializeReference] private ApplicableValueBoolean m_emojiFallbackSupport = new();
		[SerializeReference] private ApplicableValueBoolean m_enableAutoSizing = new();
		[SerializeReference] private ApplicableValueBoolean m_enabled = new();
		[SerializeReference] private ApplicableValueBoolean m_enableKerning = new();
		[SerializeReference] private ApplicableValueBoolean m_enableVertexGradient = new();
		[SerializeReference] private ApplicableValueBoolean m_extraPadding = new();
		[SerializeReference] private ApplicableValueTMP_FontAsset m_font = new();
		[SerializeReference] private ApplicableValueListUnityEngineTextCoreOTL_FeatureTag m_fontFeatures = new();
		[SerializeReference] private ApplicableValueMaterial m_fontSharedMaterial = new();
		[SerializeReference] private ApplicableValueMaterialArray m_fontSharedMaterials = new();
		[SerializeReference] private ApplicableValueSingle m_fontSize = new();
		[SerializeReference] private ApplicableValueSingle m_fontSizeMax = new();
		[SerializeReference] private ApplicableValueSingle m_fontSizeMin = new();
		[SerializeReference] private ApplicableValueFontStyles m_fontStyle = new();
		[SerializeReference] private ApplicableValueFontWeight m_fontWeight = new();
		[SerializeReference] private ApplicableValueSingle m_lineSpacing = new();
		[SerializeReference] private ApplicableValueSingle m_lineSpacingAdjustment = new();
		[SerializeReference] private ApplicableValueVector4 m_margin = new();
		[SerializeReference] private ApplicableValueColor32 m_outlineColor = new();
		[SerializeReference] private ApplicableValueSingle m_outlineWidth = new();
		[SerializeReference] private ApplicableValueTextOverflowModes m_overflowMode = new();
		[SerializeReference] private ApplicableValueSingle m_paragraphSpacing = new();
		[SerializeReference] private ApplicableValueBoolean m_richText = new();
		[SerializeReference] private ApplicableValueTMP_SpriteAsset m_spriteAsset = new();
		[SerializeReference] private ApplicableValueTMP_StyleSheet m_styleSheet = new();
		[SerializeReference] private ApplicableValueString m_text = new();
		[SerializeReference] private ApplicableValueTMP_Style m_textStyle = new();
		[SerializeReference] private ApplicableValueTextWrappingModes m_textWrappingMode = new();
		[SerializeReference] private ApplicableValueSingle m_wordSpacing = new();

		public ApplicableValue<TMPro.TextAlignmentOptions> Alignment
		{
			get
			{
				if (m_alignment == null)
					m_alignment = new ApplicableValueTextAlignmentOptions();
				return m_alignment;
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

		public ApplicableValue<System.Single> CharacterSpacing
		{
			get
			{
				if (m_characterSpacing == null)
					m_characterSpacing = new ApplicableValueSingle();
				return m_characterSpacing;
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

		public ApplicableValue<UnityEngine.Color> Color
		{
			get
			{
				if (m_color == null)
					m_color = new ApplicableValueColor();
				return m_color;
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

		public ApplicableValue<System.Boolean> EmojiFallbackSupport
		{
			get
			{
				if (m_emojiFallbackSupport == null)
					m_emojiFallbackSupport = new ApplicableValueBoolean();
				return m_emojiFallbackSupport;
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

		public ApplicableValue<System.Boolean> Enabled
		{
			get
			{
				if (m_enabled == null)
					m_enabled = new ApplicableValueBoolean();
				return m_enabled;
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

		public ApplicableValue<System.Boolean> EnableVertexGradient
		{
			get
			{
				if (m_enableVertexGradient == null)
					m_enableVertexGradient = new ApplicableValueBoolean();
				return m_enableVertexGradient;
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

		public ApplicableValue<TMPro.TMP_FontAsset> Font
		{
			get
			{
				if (m_font == null)
					m_font = new ApplicableValueTMP_FontAsset();
				return m_font;
			}
		}

		public ApplicableValue<System.Collections.Generic.List<UnityEngine.TextCore.OTL_FeatureTag>> FontFeatures
		{
			get
			{
				if (m_fontFeatures == null)
					m_fontFeatures = new ApplicableValueListUnityEngineTextCoreOTL_FeatureTag();
				return m_fontFeatures;
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

		public ApplicableValue<System.Single> FontSize
		{
			get
			{
				if (m_fontSize == null)
					m_fontSize = new ApplicableValueSingle();
				return m_fontSize;
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

		public ApplicableValue<System.Single> FontSizeMin
		{
			get
			{
				if (m_fontSizeMin == null)
					m_fontSizeMin = new ApplicableValueSingle();
				return m_fontSizeMin;
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

		public ApplicableValue<TMPro.FontWeight> FontWeight
		{
			get
			{
				if (m_fontWeight == null)
					m_fontWeight = new ApplicableValueFontWeight();
				return m_fontWeight;
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

		public ApplicableValue<UnityEngine.Vector4> Margin
		{
			get
			{
				if (m_margin == null)
					m_margin = new ApplicableValueVector4();
				return m_margin;
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

		public ApplicableValue<TMPro.TextOverflowModes> OverflowMode
		{
			get
			{
				if (m_overflowMode == null)
					m_overflowMode = new ApplicableValueTextOverflowModes();
				return m_overflowMode;
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

		public ApplicableValue<System.Boolean> RichText
		{
			get
			{
				if (m_richText == null)
					m_richText = new ApplicableValueBoolean();
				return m_richText;
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

		public ApplicableValue<TMPro.TMP_StyleSheet> StyleSheet
		{
			get
			{
				if (m_styleSheet == null)
					m_styleSheet = new ApplicableValueTMP_StyleSheet();
				return m_styleSheet;
			}
		}

		public ApplicableValue<System.String> Text
		{
			get
			{
				if (m_text == null)
					m_text = new ApplicableValueString();
				return m_text;
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

		public ApplicableValue<TMPro.TextWrappingModes> TextWrappingMode
		{
			get
			{
				if (m_textWrappingMode == null)
					m_textWrappingMode = new ApplicableValueTextWrappingModes();
				return m_textWrappingMode;
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

	}
}
