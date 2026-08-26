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
		private class ApplicableValueBoolean : ApplicableValue<System.Boolean> {}
		private class ApplicableValueSingle : ApplicableValue<System.Single> {}
		private class ApplicableValueMaterial : ApplicableValue<UnityEngine.Material> {}
		private class ApplicableValueRectOffset : ApplicableValue<UnityEngine.RectOffset> {}
		private class ApplicableValueSprite : ApplicableValue<UnityEngine.Sprite> {}

		protected override ApplicableValueBase[] GetValueList()
		{
			return new ApplicableValueBase[]
			{
				Color,
				CornerSegments,
				Enabled,
				FadeSize,
				FrameSize,
				InvertMask,
				IsMaskingGraphic,
				Maskable,
				Material,
				Padding,
				PixelsPerUnitMultiplier,
				Radius,
				Sprite,
			};
		}

		[SerializeReference] private ApplicableValueColor m_color = new();
		[SerializeReference] private ApplicableValueInt32 m_CornerSegments = new();
		[SerializeReference] private ApplicableValueBoolean m_enabled = new();
		[SerializeReference] private ApplicableValueSingle m_FadeSize = new();
		[SerializeReference] private ApplicableValueSingle m_FrameSize = new();
		[SerializeReference] private ApplicableValueBoolean m_InvertMask = new();
		[SerializeReference] private ApplicableValueBoolean m_isMaskingGraphic = new();
		[SerializeReference] private ApplicableValueBoolean m_maskable = new();
		[SerializeReference] private ApplicableValueMaterial m_material = new();
		[SerializeReference] private ApplicableValueRectOffset m_Padding = new();
		[SerializeReference] private ApplicableValueSingle m_pixelsPerUnitMultiplier = new();
		[SerializeReference] private ApplicableValueSingle m_Radius = new();
		[SerializeReference] private ApplicableValueSprite m_sprite = new();

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

	}
}
