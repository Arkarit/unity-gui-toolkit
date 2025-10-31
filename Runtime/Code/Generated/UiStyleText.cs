// Auto-generated, please do not change!
using System;
using UnityEngine;
using GuiToolkit;
using GuiToolkit.Style;

namespace GuiToolkit.Style
{
	[Serializable]
	public class UiStyleText : UiAbstractStyle<UnityEngine.UI.Text>
	{
		public UiStyleText(UiStyleConfig _styleConfig, string _name)
		{
			StyleConfig = _styleConfig;
			Name = _name;
		}

		private class ApplicableValueBoolean : ApplicableValue<System.Boolean> {}
		private class ApplicableValueTextAnchor : ApplicableValue<UnityEngine.TextAnchor> {}
		private class ApplicableValueColor : ApplicableValue<UnityEngine.Color> {}
		private class ApplicableValueFont : ApplicableValue<UnityEngine.Font> {}
		private class ApplicableValueInt32 : ApplicableValue<System.Int32> {}
		private class ApplicableValueFontStyle : ApplicableValue<UnityEngine.FontStyle> {}
		private class ApplicableValueHorizontalWrapMode : ApplicableValue<UnityEngine.HorizontalWrapMode> {}
		private class ApplicableValueSingle : ApplicableValue<System.Single> {}
		private class ApplicableValueMaterial : ApplicableValue<UnityEngine.Material> {}
		private class ApplicableValueCullStateChangedEvent : ApplicableValue<UnityEngine.UI.MaskableGraphic.CullStateChangedEvent> {}
		private class ApplicableValueVector4 : ApplicableValue<UnityEngine.Vector4> {}
		private class ApplicableValueString : ApplicableValue<System.String> {}
		private class ApplicableValueVerticalWrapMode : ApplicableValue<UnityEngine.VerticalWrapMode> {}

		protected override ApplicableValueBase[] GetValueList()
		{
			return new ApplicableValueBase[]
			{
				AlignByGeometry,
				Alignment,
				Color,
				Enabled,
				Font,
				FontSize,
				FontStyle,
				HorizontalOverflow,
				IsMaskingGraphic,
				LineSpacing,
				Maskable,
				Material,
				OnCullStateChanged,
				RaycastPadding,
				RaycastTarget,
				ResizeTextForBestFit,
				ResizeTextMaxSize,
				ResizeTextMinSize,
				SupportRichText,
				Text,
				VerticalOverflow,
			};
		}

		[SerializeReference] private ApplicableValueBoolean m_alignByGeometry = new();
		[SerializeReference] private ApplicableValueTextAnchor m_alignment = new();
		[SerializeReference] private ApplicableValueColor m_color = new();
		[SerializeReference] private ApplicableValueBoolean m_enabled = new();
		[SerializeReference] private ApplicableValueFont m_font = new();
		[SerializeReference] private ApplicableValueInt32 m_fontSize = new();
		[SerializeReference] private ApplicableValueFontStyle m_fontStyle = new();
		[SerializeReference] private ApplicableValueHorizontalWrapMode m_horizontalOverflow = new();
		[SerializeReference] private ApplicableValueBoolean m_isMaskingGraphic = new();
		[SerializeReference] private ApplicableValueSingle m_lineSpacing = new();
		[SerializeReference] private ApplicableValueBoolean m_maskable = new();
		[SerializeReference] private ApplicableValueMaterial m_material = new();
		[SerializeReference] private ApplicableValueCullStateChangedEvent m_onCullStateChanged = new();
		[SerializeReference] private ApplicableValueVector4 m_raycastPadding = new();
		[SerializeReference] private ApplicableValueBoolean m_raycastTarget = new();
		[SerializeReference] private ApplicableValueBoolean m_resizeTextForBestFit = new();
		[SerializeReference] private ApplicableValueInt32 m_resizeTextMaxSize = new();
		[SerializeReference] private ApplicableValueInt32 m_resizeTextMinSize = new();
		[SerializeReference] private ApplicableValueBoolean m_supportRichText = new();
		[SerializeReference] private ApplicableValueString m_text = new();
		[SerializeReference] private ApplicableValueVerticalWrapMode m_verticalOverflow = new();

		public ApplicableValue<System.Boolean> AlignByGeometry
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_alignByGeometry == null)
						m_alignByGeometry = new ApplicableValueBoolean();
				#endif
				return m_alignByGeometry;
			}
		}

		public ApplicableValue<UnityEngine.TextAnchor> Alignment
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_alignment == null)
						m_alignment = new ApplicableValueTextAnchor();
				#endif
				return m_alignment;
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

		public ApplicableValue<System.Boolean> Enabled
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_enabled == null)
						m_enabled = new ApplicableValueBoolean();
				#endif
				return m_enabled;
			}
		}

		public ApplicableValue<UnityEngine.Font> Font
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_font == null)
						m_font = new ApplicableValueFont();
				#endif
				return m_font;
			}
		}

		public ApplicableValue<System.Int32> FontSize
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_fontSize == null)
						m_fontSize = new ApplicableValueInt32();
				#endif
				return m_fontSize;
			}
		}

		public ApplicableValue<UnityEngine.FontStyle> FontStyle
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_fontStyle == null)
						m_fontStyle = new ApplicableValueFontStyle();
				#endif
				return m_fontStyle;
			}
		}

		public ApplicableValue<UnityEngine.HorizontalWrapMode> HorizontalOverflow
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_horizontalOverflow == null)
						m_horizontalOverflow = new ApplicableValueHorizontalWrapMode();
				#endif
				return m_horizontalOverflow;
			}
		}

		public ApplicableValue<System.Boolean> IsMaskingGraphic
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_isMaskingGraphic == null)
						m_isMaskingGraphic = new ApplicableValueBoolean();
				#endif
				return m_isMaskingGraphic;
			}
		}

		public ApplicableValue<System.Single> LineSpacing
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_lineSpacing == null)
						m_lineSpacing = new ApplicableValueSingle();
				#endif
				return m_lineSpacing;
			}
		}

		public ApplicableValue<System.Boolean> Maskable
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_maskable == null)
						m_maskable = new ApplicableValueBoolean();
				#endif
				return m_maskable;
			}
		}

		public ApplicableValue<UnityEngine.Material> Material
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_material == null)
						m_material = new ApplicableValueMaterial();
				#endif
				return m_material;
			}
		}

		public ApplicableValue<UnityEngine.UI.MaskableGraphic.CullStateChangedEvent> OnCullStateChanged
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_onCullStateChanged == null)
						m_onCullStateChanged = new ApplicableValueCullStateChangedEvent();
				#endif
				return m_onCullStateChanged;
			}
		}

		public ApplicableValue<UnityEngine.Vector4> RaycastPadding
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_raycastPadding == null)
						m_raycastPadding = new ApplicableValueVector4();
				#endif
				return m_raycastPadding;
			}
		}

		public ApplicableValue<System.Boolean> RaycastTarget
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_raycastTarget == null)
						m_raycastTarget = new ApplicableValueBoolean();
				#endif
				return m_raycastTarget;
			}
		}

		public ApplicableValue<System.Boolean> ResizeTextForBestFit
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_resizeTextForBestFit == null)
						m_resizeTextForBestFit = new ApplicableValueBoolean();
				#endif
				return m_resizeTextForBestFit;
			}
		}

		public ApplicableValue<System.Int32> ResizeTextMaxSize
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_resizeTextMaxSize == null)
						m_resizeTextMaxSize = new ApplicableValueInt32();
				#endif
				return m_resizeTextMaxSize;
			}
		}

		public ApplicableValue<System.Int32> ResizeTextMinSize
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_resizeTextMinSize == null)
						m_resizeTextMinSize = new ApplicableValueInt32();
				#endif
				return m_resizeTextMinSize;
			}
		}

		public ApplicableValue<System.Boolean> SupportRichText
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_supportRichText == null)
						m_supportRichText = new ApplicableValueBoolean();
				#endif
				return m_supportRichText;
			}
		}

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

		public ApplicableValue<UnityEngine.VerticalWrapMode> VerticalOverflow
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_verticalOverflow == null)
						m_verticalOverflow = new ApplicableValueVerticalWrapMode();
				#endif
				return m_verticalOverflow;
			}
		}

	}
}
