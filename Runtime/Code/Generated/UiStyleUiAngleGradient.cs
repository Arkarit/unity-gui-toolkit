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
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_ColorBase == null)
						m_ColorBase = new ApplicableValueColor();
				#endif
				return m_ColorBase;
			}
		}

		public ApplicableValue<UnityEngine.Color> ColorHighlight
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_ColorHighlight == null)
						m_ColorHighlight = new ApplicableValueColor();
				#endif
				return m_ColorHighlight;
			}
		}

		public ApplicableValue<System.Single> Count
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_Count == null)
						m_Count = new ApplicableValueSingle();
				#endif
				return m_Count;
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

		public ApplicableValue<System.Single> Rotation
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_Rotation == null)
						m_Rotation = new ApplicableValueSingle();
				#endif
				return m_Rotation;
			}
		}

		public ApplicableValue<System.Single> Sharpness
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_Sharpness == null)
						m_Sharpness = new ApplicableValueSingle();
				#endif
				return m_Sharpness;
			}
		}

		public ApplicableValue<System.Single> Speed
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_Speed == null)
						m_Speed = new ApplicableValueSingle();
				#endif
				return m_Speed;
			}
		}

	}
}
