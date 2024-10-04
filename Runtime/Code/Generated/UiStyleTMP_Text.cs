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
		private class ApplicableValueColor : ApplicableValue<UnityEngine.Color> {}
		private class ApplicableValueSingle : ApplicableValue<System.Single> {}
		private class ApplicableValueVertexGradient : ApplicableValue<TMPro.VertexGradient> {}
		private class ApplicableValueTMP_StyleSheet : ApplicableValue<TMPro.TMP_StyleSheet> {}
		private class ApplicableValueTMP_Style : ApplicableValue<TMPro.TMP_Style> {}
		private class ApplicableValueColor32 : ApplicableValue<UnityEngine.Color32> {}
		private class ApplicableValueFontWeight : ApplicableValue<TMPro.FontWeight> {}
		private class ApplicableValueFontStyles : ApplicableValue<TMPro.FontStyles> {}
		private class ApplicableValueTextAlignmentOptions : ApplicableValue<TMPro.TextAlignmentOptions> {}
		private class ApplicableValueBoolean : ApplicableValue<System.Boolean> {}
		private class ApplicableValueVector4 : ApplicableValue<UnityEngine.Vector4> {}

		protected override ApplicableValueBase[] GetValueList()
		{
			return new ApplicableValueBase[]
			{
				m_text,
				m_font,
				m_color,
				m_alpha,
				m_colorGradient,
				m_styleSheet,
				m_textStyle,
				m_outlineColor,
				m_outlineWidth,
				m_fontSize,
				m_fontWeight,
				m_fontSizeMin,
				m_fontSizeMax,
				m_fontStyle,
				m_alignment,
				m_extraPadding,
				m_margin,
			};
		}

		[SerializeReference] private ApplicableValueString m_text = new();
		[SerializeReference] private ApplicableValueTMP_FontAsset m_font = new();
		[SerializeReference] private ApplicableValueColor m_color = new();
		[SerializeReference] private ApplicableValueSingle m_alpha = new();
		[SerializeReference] private ApplicableValueVertexGradient m_colorGradient = new();
		[SerializeReference] private ApplicableValueTMP_StyleSheet m_styleSheet = new();
		[SerializeReference] private ApplicableValueTMP_Style m_textStyle = new();
		[SerializeReference] private ApplicableValueColor32 m_outlineColor = new();
		[SerializeReference] private ApplicableValueSingle m_outlineWidth = new();
		[SerializeReference] private ApplicableValueSingle m_fontSize = new();
		[SerializeReference] private ApplicableValueFontWeight m_fontWeight = new();
		[SerializeReference] private ApplicableValueSingle m_fontSizeMin = new();
		[SerializeReference] private ApplicableValueSingle m_fontSizeMax = new();
		[SerializeReference] private ApplicableValueFontStyles m_fontStyle = new();
		[SerializeReference] private ApplicableValueTextAlignmentOptions m_alignment = new();
		[SerializeReference] private ApplicableValueBoolean m_extraPadding = new();
		[SerializeReference] private ApplicableValueVector4 m_margin = new();

		public ApplicableValue<System.String> Text => m_text;
		public ApplicableValue<TMPro.TMP_FontAsset> Font => m_font;
		public ApplicableValue<UnityEngine.Color> Color => m_color;
		public ApplicableValue<System.Single> Alpha => m_alpha;
		public ApplicableValue<TMPro.VertexGradient> ColorGradient => m_colorGradient;
		public ApplicableValue<TMPro.TMP_StyleSheet> StyleSheet => m_styleSheet;
		public ApplicableValue<TMPro.TMP_Style> TextStyle => m_textStyle;
		public ApplicableValue<UnityEngine.Color32> OutlineColor => m_outlineColor;
		public ApplicableValue<System.Single> OutlineWidth => m_outlineWidth;
		public ApplicableValue<System.Single> FontSize => m_fontSize;
		public ApplicableValue<TMPro.FontWeight> FontWeight => m_fontWeight;
		public ApplicableValue<System.Single> FontSizeMin => m_fontSizeMin;
		public ApplicableValue<System.Single> FontSizeMax => m_fontSizeMax;
		public ApplicableValue<TMPro.FontStyles> FontStyle => m_fontStyle;
		public ApplicableValue<TMPro.TextAlignmentOptions> Alignment => m_alignment;
		public ApplicableValue<System.Boolean> ExtraPadding => m_extraPadding;
		public ApplicableValue<UnityEngine.Vector4> Margin => m_margin;
	}
}
