// Auto-generated, please do not change!
using System;
using UnityEngine;
using GuiToolkit;
using GuiToolkit.Style;

namespace GuiToolkit.Style
{
	[Serializable]
	public class UiStyleUiAngleGradient : UiAbstractStyle<GuiToolkit.UiAngleGradient>
	{
		public UiStyleUiAngleGradient(UiStyleConfig _styleConfig, string _name)
		{
			StyleConfig = _styleConfig;
			Name = _name;
		}

		private class ApplicableValueColor : ApplicableValue<UnityEngine.Color> {}
		private class ApplicableValueSingle : ApplicableValue<System.Single> {}
		private class ApplicableValueBoolean : ApplicableValue<System.Boolean> {}

		protected override ApplicableValueBase[] GetValueList()
		{
			return new ApplicableValueBase[]
			{
				ColorBase,
				ColorHighlight,
				Count,
				Enabled,
				Rotation,
				Sharpness,
				Speed,
			};
		}

		[SerializeReference] private ApplicableValueColor m_ColorBase = new();
		[SerializeReference] private ApplicableValueColor m_ColorHighlight = new();
		[SerializeReference] private ApplicableValueSingle m_Count = new();
		[SerializeReference] private ApplicableValueBoolean m_enabled = new();
		[SerializeReference] private ApplicableValueSingle m_Rotation = new();
		[SerializeReference] private ApplicableValueSingle m_Sharpness = new();
		[SerializeReference] private ApplicableValueSingle m_Speed = new();

		public ApplicableValue<UnityEngine.Color> ColorBase
		{
			get
			{
				if (m_ColorBase == null)
					m_ColorBase = new ApplicableValueColor();
				return m_ColorBase;
			}
		}

		public ApplicableValue<UnityEngine.Color> ColorHighlight
		{
			get
			{
				if (m_ColorHighlight == null)
					m_ColorHighlight = new ApplicableValueColor();
				return m_ColorHighlight;
			}
		}

		public ApplicableValue<System.Single> Count
		{
			get
			{
				if (m_Count == null)
					m_Count = new ApplicableValueSingle();
				return m_Count;
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

		public ApplicableValue<System.Single> Rotation
		{
			get
			{
				if (m_Rotation == null)
					m_Rotation = new ApplicableValueSingle();
				return m_Rotation;
			}
		}

		public ApplicableValue<System.Single> Sharpness
		{
			get
			{
				if (m_Sharpness == null)
					m_Sharpness = new ApplicableValueSingle();
				return m_Sharpness;
			}
		}

		public ApplicableValue<System.Single> Speed
		{
			get
			{
				if (m_Speed == null)
					m_Speed = new ApplicableValueSingle();
				return m_Speed;
			}
		}

	}
}
