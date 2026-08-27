// Auto-generated, please do not change!
using System;
using UnityEngine;
using GuiToolkit;
using GuiToolkit.Style;

namespace GuiToolkit.Style
{
	[Serializable]
	public class UiStyleUiRoundedImage : UiAbstractStyle<GuiToolkit.UiRoundedImage>
	{
		public UiStyleUiRoundedImage(UiStyleConfig _styleConfig, string _name)
		{
			StyleConfig = _styleConfig;
			Name = _name;
		}

		private class ApplicableValueColor : ApplicableValue<UnityEngine.Color> {}
		private class ApplicableValueInt32 : ApplicableValue<System.Int32> {}
		private class ApplicableValueMaterial : ApplicableValue<UnityEngine.Material> {}
		private class ApplicableValueBoolean : ApplicableValue<System.Boolean> {}
		private class ApplicableValueSingle : ApplicableValue<System.Single> {}
		private class ApplicableValueFillMethod : ApplicableValue<UnityEngine.UI.Image.FillMethod> {}
		private class ApplicableValueRect : ApplicableValue<UnityEngine.Rect> {}
		private class ApplicableValueEdgeGap : ApplicableValue<GuiToolkit.UiRoundedImage.EdgeGap> {}
		private class ApplicableValueEGapUnit : ApplicableValue<GuiToolkit.UiRoundedImage.EGapUnit> {}
		private class ApplicableValueRectOffset : ApplicableValue<UnityEngine.RectOffset> {}
		private class ApplicableValueVector2 : ApplicableValue<UnityEngine.Vector2> {}
		private class ApplicableValueSprite : ApplicableValue<UnityEngine.Sprite> {}

		protected override ApplicableValueBase[] GetValueList()
		{
			return new ApplicableValueBase[]
			{
				Color,
				CornerSegments,
				DisabledMaterial,
				Enabled,
				EnabledInHierarchy,
				FadeColor,
				FadeSize,
				FillAmount,
				FillCenter,
				FillClockwise,
				FillMethod,
				FillOrigin,
				FixedSize,
				FrameSize,
				GapBottom,
				GapHorizontal,
				GapLeft,
				GapRight,
				GapTop,
				GapUnit,
				GapVertical,
				InvertMask,
				IsMaskingGraphic,
				Maskable,
				Material,
				Padding,
				PixelsPerUnitMultiplier,
				PositionOffset,
				Radius,
				SizeOffset,
				Sprite,
				UniformSizeOffset,
				UseFixedSize,
				UsePadding,
			};
		}

		[SerializeReference] private ApplicableValueColor m_color = new();
		[SerializeReference] private ApplicableValueInt32 m_CornerSegments = new();
		[SerializeReference] private ApplicableValueMaterial m_DisabledMaterial = new();
		[SerializeReference] private ApplicableValueBoolean m_enabled = new();
		[SerializeReference] private ApplicableValueBoolean m_EnabledInHierarchy = new();
		[SerializeReference] private ApplicableValueColor m_FadeColor = new();
		[SerializeReference] private ApplicableValueSingle m_FadeSize = new();
		[SerializeReference] private ApplicableValueSingle m_fillAmount = new();
		[SerializeReference] private ApplicableValueBoolean m_fillCenter = new();
		[SerializeReference] private ApplicableValueBoolean m_fillClockwise = new();
		[SerializeReference] private ApplicableValueFillMethod m_fillMethod = new();
		[SerializeReference] private ApplicableValueInt32 m_fillOrigin = new();
		[SerializeReference] private ApplicableValueRect m_FixedSize = new();
		[SerializeReference] private ApplicableValueSingle m_FrameSize = new();
		[SerializeReference] private ApplicableValueEdgeGap m_GapBottom = new();
		[SerializeReference] private ApplicableValueEdgeGap m_GapHorizontal = new();
		[SerializeReference] private ApplicableValueEdgeGap m_GapLeft = new();
		[SerializeReference] private ApplicableValueEdgeGap m_GapRight = new();
		[SerializeReference] private ApplicableValueEdgeGap m_GapTop = new();
		[SerializeReference] private ApplicableValueEGapUnit m_GapUnit = new();
		[SerializeReference] private ApplicableValueEdgeGap m_GapVertical = new();
		[SerializeReference] private ApplicableValueBoolean m_InvertMask = new();
		[SerializeReference] private ApplicableValueBoolean m_isMaskingGraphic = new();
		[SerializeReference] private ApplicableValueBoolean m_maskable = new();
		[SerializeReference] private ApplicableValueMaterial m_material = new();
		[SerializeReference] private ApplicableValueRectOffset m_Padding = new();
		[SerializeReference] private ApplicableValueSingle m_pixelsPerUnitMultiplier = new();
		[SerializeReference] private ApplicableValueVector2 m_PositionOffset = new();
		[SerializeReference] private ApplicableValueSingle m_Radius = new();
		[SerializeReference] private ApplicableValueVector2 m_SizeOffset = new();
		[SerializeReference] private ApplicableValueSprite m_sprite = new();
		[SerializeReference] private ApplicableValueBoolean m_UniformSizeOffset = new();
		[SerializeReference] private ApplicableValueBoolean m_UseFixedSize = new();
		[SerializeReference] private ApplicableValueBoolean m_UsePadding = new();

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

		public ApplicableValue<System.Int32> CornerSegments
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_CornerSegments == null)
						m_CornerSegments = new ApplicableValueInt32();
				#endif
				return m_CornerSegments;
			}
		}

		public ApplicableValue<UnityEngine.Material> DisabledMaterial
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_DisabledMaterial == null)
						m_DisabledMaterial = new ApplicableValueMaterial();
				#endif
				return m_DisabledMaterial;
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

		public ApplicableValue<System.Boolean> EnabledInHierarchy
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_EnabledInHierarchy == null)
						m_EnabledInHierarchy = new ApplicableValueBoolean();
				#endif
				return m_EnabledInHierarchy;
			}
		}

		public ApplicableValue<UnityEngine.Color> FadeColor
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_FadeColor == null)
						m_FadeColor = new ApplicableValueColor();
				#endif
				return m_FadeColor;
			}
		}

		public ApplicableValue<System.Single> FadeSize
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_FadeSize == null)
						m_FadeSize = new ApplicableValueSingle();
				#endif
				return m_FadeSize;
			}
		}

		public ApplicableValue<System.Single> FillAmount
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_fillAmount == null)
						m_fillAmount = new ApplicableValueSingle();
				#endif
				return m_fillAmount;
			}
		}

		public ApplicableValue<System.Boolean> FillCenter
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_fillCenter == null)
						m_fillCenter = new ApplicableValueBoolean();
				#endif
				return m_fillCenter;
			}
		}

		public ApplicableValue<System.Boolean> FillClockwise
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_fillClockwise == null)
						m_fillClockwise = new ApplicableValueBoolean();
				#endif
				return m_fillClockwise;
			}
		}

		public ApplicableValue<UnityEngine.UI.Image.FillMethod> FillMethod
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_fillMethod == null)
						m_fillMethod = new ApplicableValueFillMethod();
				#endif
				return m_fillMethod;
			}
		}

		public ApplicableValue<System.Int32> FillOrigin
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_fillOrigin == null)
						m_fillOrigin = new ApplicableValueInt32();
				#endif
				return m_fillOrigin;
			}
		}

		public ApplicableValue<UnityEngine.Rect> FixedSize
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_FixedSize == null)
						m_FixedSize = new ApplicableValueRect();
				#endif
				return m_FixedSize;
			}
		}

		public ApplicableValue<System.Single> FrameSize
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_FrameSize == null)
						m_FrameSize = new ApplicableValueSingle();
				#endif
				return m_FrameSize;
			}
		}

		public ApplicableValue<GuiToolkit.UiRoundedImage.EdgeGap> GapBottom
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_GapBottom == null)
						m_GapBottom = new ApplicableValueEdgeGap();
				#endif
				return m_GapBottom;
			}
		}

		public ApplicableValue<GuiToolkit.UiRoundedImage.EdgeGap> GapHorizontal
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_GapHorizontal == null)
						m_GapHorizontal = new ApplicableValueEdgeGap();
				#endif
				return m_GapHorizontal;
			}
		}

		public ApplicableValue<GuiToolkit.UiRoundedImage.EdgeGap> GapLeft
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_GapLeft == null)
						m_GapLeft = new ApplicableValueEdgeGap();
				#endif
				return m_GapLeft;
			}
		}

		public ApplicableValue<GuiToolkit.UiRoundedImage.EdgeGap> GapRight
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_GapRight == null)
						m_GapRight = new ApplicableValueEdgeGap();
				#endif
				return m_GapRight;
			}
		}

		public ApplicableValue<GuiToolkit.UiRoundedImage.EdgeGap> GapTop
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_GapTop == null)
						m_GapTop = new ApplicableValueEdgeGap();
				#endif
				return m_GapTop;
			}
		}

		public ApplicableValue<GuiToolkit.UiRoundedImage.EGapUnit> GapUnit
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_GapUnit == null)
						m_GapUnit = new ApplicableValueEGapUnit();
				#endif
				return m_GapUnit;
			}
		}

		public ApplicableValue<GuiToolkit.UiRoundedImage.EdgeGap> GapVertical
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_GapVertical == null)
						m_GapVertical = new ApplicableValueEdgeGap();
				#endif
				return m_GapVertical;
			}
		}

		public ApplicableValue<System.Boolean> InvertMask
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_InvertMask == null)
						m_InvertMask = new ApplicableValueBoolean();
				#endif
				return m_InvertMask;
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

		public ApplicableValue<UnityEngine.RectOffset> Padding
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_Padding == null)
						m_Padding = new ApplicableValueRectOffset();
				#endif
				return m_Padding;
			}
		}

		public ApplicableValue<System.Single> PixelsPerUnitMultiplier
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_pixelsPerUnitMultiplier == null)
						m_pixelsPerUnitMultiplier = new ApplicableValueSingle();
				#endif
				return m_pixelsPerUnitMultiplier;
			}
		}

		public ApplicableValue<UnityEngine.Vector2> PositionOffset
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_PositionOffset == null)
						m_PositionOffset = new ApplicableValueVector2();
				#endif
				return m_PositionOffset;
			}
		}

		public ApplicableValue<System.Single> Radius
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_Radius == null)
						m_Radius = new ApplicableValueSingle();
				#endif
				return m_Radius;
			}
		}

		public ApplicableValue<UnityEngine.Vector2> SizeOffset
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_SizeOffset == null)
						m_SizeOffset = new ApplicableValueVector2();
				#endif
				return m_SizeOffset;
			}
		}

		public ApplicableValue<UnityEngine.Sprite> Sprite
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_sprite == null)
						m_sprite = new ApplicableValueSprite();
				#endif
				return m_sprite;
			}
		}

		public ApplicableValue<System.Boolean> UniformSizeOffset
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_UniformSizeOffset == null)
						m_UniformSizeOffset = new ApplicableValueBoolean();
				#endif
				return m_UniformSizeOffset;
			}
		}

		public ApplicableValue<System.Boolean> UseFixedSize
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_UseFixedSize == null)
						m_UseFixedSize = new ApplicableValueBoolean();
				#endif
				return m_UseFixedSize;
			}
		}

		public ApplicableValue<System.Boolean> UsePadding
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_UsePadding == null)
						m_UsePadding = new ApplicableValueBoolean();
				#endif
				return m_UsePadding;
			}
		}

	}
}
