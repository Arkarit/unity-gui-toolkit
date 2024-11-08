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
				Text,
				Font,
				Color,
				Alpha,
				ColorGradient,
				StyleSheet,
				TextStyle,
				OutlineColor,
				OutlineWidth,
				FontSize,
				FontWeight,
				FontSizeMin,
				FontSizeMax,
				FontStyle,
				Alignment,
				ExtraPadding,
				Margin,
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

		public ApplicableValue<System.String> Text
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_text == null)
						m_text = new ApplicableValueString();
				#endif
				return m_text;
			}
		}

		public ApplicableValue<TMPro.TMP_FontAsset> Font
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_font == null)
						m_font = new ApplicableValueTMP_FontAsset();
				#endif
				return m_font;
			}
		}

		public ApplicableValue<UnityEngine.Color> Color
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_color == null)
						m_color = new ApplicableValueColor();
				#endif
				return m_color;
			}
		}

		public ApplicableValue<System.Single> Alpha
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_alpha == null)
						m_alpha = new ApplicableValueSingle();
				#endif
				return m_alpha;
			}
		}

		public ApplicableValue<TMPro.VertexGradient> ColorGradient
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_colorGradient == null)
						m_colorGradient = new ApplicableValueVertexGradient();
				#endif
				return m_colorGradient;
			}
		}

		public ApplicableValue<TMPro.TMP_StyleSheet> StyleSheet
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_styleSheet == null)
						m_styleSheet = new ApplicableValueTMP_StyleSheet();
				#endif
				return m_styleSheet;
			}
		}

		public ApplicableValue<TMPro.TMP_Style> TextStyle
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_textStyle == null)
						m_textStyle = new ApplicableValueTMP_Style();
				#endif
				return m_textStyle;
			}
		}

		public ApplicableValue<UnityEngine.Color32> OutlineColor
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_outlineColor == null)
						m_outlineColor = new ApplicableValueColor32();
				#endif
				return m_outlineColor;
			}
		}

		public ApplicableValue<System.Single> OutlineWidth
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_outlineWidth == null)
						m_outlineWidth = new ApplicableValueSingle();
				#endif
				return m_outlineWidth;
			}
		}

		public ApplicableValue<System.Single> FontSize
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_fontSize == null)
						m_fontSize = new ApplicableValueSingle();
				#endif
				return m_fontSize;
			}
		}

		public ApplicableValue<TMPro.FontWeight> FontWeight
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_fontWeight == null)
						m_fontWeight = new ApplicableValueFontWeight();
				#endif
				return m_fontWeight;
			}
		}

		public ApplicableValue<System.Single> FontSizeMin
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_fontSizeMin == null)
						m_fontSizeMin = new ApplicableValueSingle();
				#endif
				return m_fontSizeMin;
			}
		}

		public ApplicableValue<System.Single> FontSizeMax
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_fontSizeMax == null)
						m_fontSizeMax = new ApplicableValueSingle();
				#endif
				return m_fontSizeMax;
			}
		}

		public ApplicableValue<TMPro.FontStyles> FontStyle
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_fontStyle == null)
						m_fontStyle = new ApplicableValueFontStyles();
				#endif
				return m_fontStyle;
			}
		}

		public ApplicableValue<TMPro.TextAlignmentOptions> Alignment
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_alignment == null)
						m_alignment = new ApplicableValueTextAlignmentOptions();
				#endif
				return m_alignment;
			}
		}

		public ApplicableValue<System.Boolean> ExtraPadding
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_extraPadding == null)
						m_extraPadding = new ApplicableValueBoolean();
				#endif
				return m_extraPadding;
			}
		}

		public ApplicableValue<UnityEngine.Vector4> Margin
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_margin == null)
						m_margin = new ApplicableValueVector4();
				#endif
				return m_margin;
			}
		}

	}
}
