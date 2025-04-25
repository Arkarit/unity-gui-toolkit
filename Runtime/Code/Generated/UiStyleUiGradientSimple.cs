// Auto-generated, please do not change!
using System;
using System.Collections.Generic;
using UnityEngine;
using GuiToolkit;
using GuiToolkit.Style;

namespace GuiToolkit.Style
{
	[Serializable]
	public class UiStyleUiGradientSimple : UiAbstractStyle<GuiToolkit.UiGradientSimple>
	{
		public UiStyleUiGradientSimple(UiStyleConfig _styleConfig, string _name)
		{
			StyleConfig = _styleConfig;
			Name = _name;
		}

		private class ApplicableValueColor : ApplicableValue<UnityEngine.Color> {}
		private class ApplicableValueBoolean : ApplicableValue<System.Boolean> {}
		private class ApplicableValueEOrientation : ApplicableValue<GuiToolkit.UiGradientSimple.EOrientation> {}

		protected override ApplicableValueBase[] GetValueArray()
		{
			return new ApplicableValueBase[]
			{
				ColorLeftOrTop,
				ColorRightOrBottom,
				Enabled,
				Orientation,
				Swapped,
			};
		}

#if UNITY_EDITOR
		public override List<ValueInfo> GetValueInfos()
		{
			return new List<ValueInfo>()
			{
				new ValueInfo()
				{
					GetterName = "ColorLeftOrTop",
					GetterType = typeof(ApplicableValueColor),
					Value = ColorLeftOrTop,
				},
				new ValueInfo()
				{
					GetterName = "ColorRightOrBottom",
					GetterType = typeof(ApplicableValueColor),
					Value = ColorRightOrBottom,
				},
				new ValueInfo()
				{
					GetterName = "Enabled",
					GetterType = typeof(ApplicableValueBoolean),
					Value = Enabled,
				},
				new ValueInfo()
				{
					GetterName = "Orientation",
					GetterType = typeof(ApplicableValueEOrientation),
					Value = Orientation,
				},
				new ValueInfo()
				{
					GetterName = "Swapped",
					GetterType = typeof(ApplicableValueBoolean),
					Value = Swapped,
				},
			};
		}
#endif

		[SerializeReference] private ApplicableValueColor m_ColorLeftOrTop = new();
		[SerializeReference] private ApplicableValueColor m_ColorRightOrBottom = new();
		[SerializeReference] private ApplicableValueBoolean m_enabled = new();
		[SerializeReference] private ApplicableValueEOrientation m_Orientation = new();
		[SerializeReference] private ApplicableValueBoolean m_Swapped = new();

		public ApplicableValue<UnityEngine.Color> ColorLeftOrTop
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_ColorLeftOrTop == null)
						m_ColorLeftOrTop = new ApplicableValueColor();
				#endif
				return m_ColorLeftOrTop;
			}
		}

		public ApplicableValue<UnityEngine.Color> ColorRightOrBottom
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_ColorRightOrBottom == null)
						m_ColorRightOrBottom = new ApplicableValueColor();
				#endif
				return m_ColorRightOrBottom;
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

		public ApplicableValue<GuiToolkit.UiGradientSimple.EOrientation> Orientation
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_Orientation == null)
						m_Orientation = new ApplicableValueEOrientation();
				#endif
				return m_Orientation;
			}
		}

		public ApplicableValue<System.Boolean> Swapped
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_Swapped == null)
						m_Swapped = new ApplicableValueBoolean();
				#endif
				return m_Swapped;
			}
		}

	}
}
