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

		private class ApplicableValueCornerFlags : ApplicableValue<GuiToolkit.CornerFlags> {}
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
				ChamferedCorners,
				Color,
				Corners,
				CornerSegments,
				DisabledMaterial,
				Enabled,
				EnabledInHierarchy,
				FadeColor,
				FadeSize,
				FillAmount,
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
				PositionOffset,
				Radius,
				SizeOffset,
				Sprite,
				UniformSizeOffset,
				UseFixedSize,
				UsePadding,
			};
		}

		[SerializeReference] private ApplicableValueCornerFlags m_ChamferedCorners = new();
		[SerializeReference] private ApplicableValueColor m_color = new();
		[SerializeReference] private ApplicableValueCornerFlags m_Corners = new();
		[SerializeReference] private ApplicableValueInt32 m_CornerSegments = new();
		[SerializeReference] private ApplicableValueMaterial m_DisabledMaterial = new();
		[SerializeReference] private ApplicableValueBoolean m_enabled = new();
		[SerializeReference] private ApplicableValueBoolean m_EnabledInHierarchy = new();
		[SerializeReference] private ApplicableValueColor m_FadeColor = new();
		[SerializeReference] private ApplicableValueSingle m_FadeSize = new();
		[SerializeReference] private ApplicableValueSingle m_fillAmount = new();
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
		[SerializeReference] private ApplicableValueVector2 m_PositionOffset = new();
		[SerializeReference] private ApplicableValueSingle m_Radius = new();
		[SerializeReference] private ApplicableValueVector2 m_SizeOffset = new();
		[SerializeReference] private ApplicableValueSprite m_sprite = new();
		[SerializeReference] private ApplicableValueBoolean m_UniformSizeOffset = new();
		[SerializeReference] private ApplicableValueBoolean m_UseFixedSize = new();
		[SerializeReference] private ApplicableValueBoolean m_UsePadding = new();

		public ApplicableValue<GuiToolkit.CornerFlags> ChamferedCorners
		{
			get
			{
				if (m_ChamferedCorners == null)
					m_ChamferedCorners = new ApplicableValueCornerFlags();
				return m_ChamferedCorners;
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

		public ApplicableValue<GuiToolkit.CornerFlags> Corners
		{
			get
			{
				if (m_Corners == null)
					m_Corners = new ApplicableValueCornerFlags();
				return m_Corners;
			}
		}

		public ApplicableValue<System.Int32> CornerSegments
		{
			get
			{
				if (m_CornerSegments == null)
					m_CornerSegments = new ApplicableValueInt32();
				return m_CornerSegments;
			}
		}

		public ApplicableValue<UnityEngine.Material> DisabledMaterial
		{
			get
			{
				if (m_DisabledMaterial == null)
					m_DisabledMaterial = new ApplicableValueMaterial();
				return m_DisabledMaterial;
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

		public ApplicableValue<System.Boolean> EnabledInHierarchy
		{
			get
			{
				if (m_EnabledInHierarchy == null)
					m_EnabledInHierarchy = new ApplicableValueBoolean();
				return m_EnabledInHierarchy;
			}
		}

		public ApplicableValue<UnityEngine.Color> FadeColor
		{
			get
			{
				if (m_FadeColor == null)
					m_FadeColor = new ApplicableValueColor();
				return m_FadeColor;
			}
		}

		public ApplicableValue<System.Single> FadeSize
		{
			get
			{
				if (m_FadeSize == null)
					m_FadeSize = new ApplicableValueSingle();
				return m_FadeSize;
			}
		}

		public ApplicableValue<System.Single> FillAmount
		{
			get
			{
				if (m_fillAmount == null)
					m_fillAmount = new ApplicableValueSingle();
				return m_fillAmount;
			}
		}

		public ApplicableValue<System.Boolean> FillClockwise
		{
			get
			{
				if (m_fillClockwise == null)
					m_fillClockwise = new ApplicableValueBoolean();
				return m_fillClockwise;
			}
		}

		public ApplicableValue<UnityEngine.UI.Image.FillMethod> FillMethod
		{
			get
			{
				if (m_fillMethod == null)
					m_fillMethod = new ApplicableValueFillMethod();
				return m_fillMethod;
			}
		}

		public ApplicableValue<System.Int32> FillOrigin
		{
			get
			{
				if (m_fillOrigin == null)
					m_fillOrigin = new ApplicableValueInt32();
				return m_fillOrigin;
			}
		}

		public ApplicableValue<UnityEngine.Rect> FixedSize
		{
			get
			{
				if (m_FixedSize == null)
					m_FixedSize = new ApplicableValueRect();
				return m_FixedSize;
			}
		}

		public ApplicableValue<System.Single> FrameSize
		{
			get
			{
				if (m_FrameSize == null)
					m_FrameSize = new ApplicableValueSingle();
				return m_FrameSize;
			}
		}

		public ApplicableValue<GuiToolkit.UiRoundedImage.EdgeGap> GapBottom
		{
			get
			{
				if (m_GapBottom == null)
					m_GapBottom = new ApplicableValueEdgeGap();
				return m_GapBottom;
			}
		}

		public ApplicableValue<GuiToolkit.UiRoundedImage.EdgeGap> GapHorizontal
		{
			get
			{
				if (m_GapHorizontal == null)
					m_GapHorizontal = new ApplicableValueEdgeGap();
				return m_GapHorizontal;
			}
		}

		public ApplicableValue<GuiToolkit.UiRoundedImage.EdgeGap> GapLeft
		{
			get
			{
				if (m_GapLeft == null)
					m_GapLeft = new ApplicableValueEdgeGap();
				return m_GapLeft;
			}
		}

		public ApplicableValue<GuiToolkit.UiRoundedImage.EdgeGap> GapRight
		{
			get
			{
				if (m_GapRight == null)
					m_GapRight = new ApplicableValueEdgeGap();
				return m_GapRight;
			}
		}

		public ApplicableValue<GuiToolkit.UiRoundedImage.EdgeGap> GapTop
		{
			get
			{
				if (m_GapTop == null)
					m_GapTop = new ApplicableValueEdgeGap();
				return m_GapTop;
			}
		}

		public ApplicableValue<GuiToolkit.UiRoundedImage.EGapUnit> GapUnit
		{
			get
			{
				if (m_GapUnit == null)
					m_GapUnit = new ApplicableValueEGapUnit();
				return m_GapUnit;
			}
		}

		public ApplicableValue<GuiToolkit.UiRoundedImage.EdgeGap> GapVertical
		{
			get
			{
				if (m_GapVertical == null)
					m_GapVertical = new ApplicableValueEdgeGap();
				return m_GapVertical;
			}
		}

		public ApplicableValue<System.Boolean> InvertMask
		{
			get
			{
				if (m_InvertMask == null)
					m_InvertMask = new ApplicableValueBoolean();
				return m_InvertMask;
			}
		}

		public ApplicableValue<System.Boolean> IsMaskingGraphic
		{
			get
			{
				if (m_isMaskingGraphic == null)
					m_isMaskingGraphic = new ApplicableValueBoolean();
				return m_isMaskingGraphic;
			}
		}

		public ApplicableValue<System.Boolean> Maskable
		{
			get
			{
				if (m_maskable == null)
					m_maskable = new ApplicableValueBoolean();
				return m_maskable;
			}
		}

		public ApplicableValue<UnityEngine.Material> Material
		{
			get
			{
				if (m_material == null)
					m_material = new ApplicableValueMaterial();
				return m_material;
			}
		}

		public ApplicableValue<UnityEngine.RectOffset> Padding
		{
			get
			{
				if (m_Padding == null)
					m_Padding = new ApplicableValueRectOffset();
				return m_Padding;
			}
		}

		public ApplicableValue<UnityEngine.Vector2> PositionOffset
		{
			get
			{
				if (m_PositionOffset == null)
					m_PositionOffset = new ApplicableValueVector2();
				return m_PositionOffset;
			}
		}

		public ApplicableValue<System.Single> Radius
		{
			get
			{
				if (m_Radius == null)
					m_Radius = new ApplicableValueSingle();
				return m_Radius;
			}
		}

		public ApplicableValue<UnityEngine.Vector2> SizeOffset
		{
			get
			{
				if (m_SizeOffset == null)
					m_SizeOffset = new ApplicableValueVector2();
				return m_SizeOffset;
			}
		}

		public ApplicableValue<UnityEngine.Sprite> Sprite
		{
			get
			{
				if (m_sprite == null)
					m_sprite = new ApplicableValueSprite();
				return m_sprite;
			}
		}

		public ApplicableValue<System.Boolean> UniformSizeOffset
		{
			get
			{
				if (m_UniformSizeOffset == null)
					m_UniformSizeOffset = new ApplicableValueBoolean();
				return m_UniformSizeOffset;
			}
		}

		public ApplicableValue<System.Boolean> UseFixedSize
		{
			get
			{
				if (m_UseFixedSize == null)
					m_UseFixedSize = new ApplicableValueBoolean();
				return m_UseFixedSize;
			}
		}

		public ApplicableValue<System.Boolean> UsePadding
		{
			get
			{
				if (m_UsePadding == null)
					m_UsePadding = new ApplicableValueBoolean();
				return m_UsePadding;
			}
		}

	}
}
