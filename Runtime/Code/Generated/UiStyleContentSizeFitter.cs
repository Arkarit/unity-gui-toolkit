// Auto-generated, please do not change!
using System;
using System.Collections.Generic;
using UnityEngine;
using GuiToolkit;
using GuiToolkit.Style;

namespace GuiToolkit.Style
{
	[Serializable]
	public class UiStyleContentSizeFitter : UiAbstractStyle<UnityEngine.UI.ContentSizeFitter>
	{
		public UiStyleContentSizeFitter(UiStyleConfig _styleConfig, string _name)
		{
			StyleConfig = _styleConfig;
			Name = _name;
		}

		private class ApplicableValueBoolean : ApplicableValue<System.Boolean> {}
		private class ApplicableValueFitMode : ApplicableValue<UnityEngine.UI.ContentSizeFitter.FitMode> {}

		protected override ApplicableValueBase[] GetValueArray()
		{
			return new ApplicableValueBase[]
			{
				Enabled,
				HorizontalFit,
				VerticalFit,
			};
		}

#if UNITY_EDITOR
		public override ValueInfo[] GetValueInfoArray()
		{
			return new ValueInfo[]
			{
				new ValueInfo()
				{
					GetterName = "Enabled",
					GetterType = typeof(ApplicableValueBoolean),
					Value = Enabled,
				},
				new ValueInfo()
				{
					GetterName = "HorizontalFit",
					GetterType = typeof(ApplicableValueFitMode),
					Value = HorizontalFit,
				},
				new ValueInfo()
				{
					GetterName = "VerticalFit",
					GetterType = typeof(ApplicableValueFitMode),
					Value = VerticalFit,
				},
			};
		}
#endif

		[SerializeReference] private ApplicableValueBoolean m_enabled = new();
		[SerializeReference] private ApplicableValueFitMode m_horizontalFit = new();
		[SerializeReference] private ApplicableValueFitMode m_verticalFit = new();

		public ApplicableValue<System.Boolean> Enabled
		{
			get
			{
				if (m_enabled == null)
					m_enabled = new ApplicableValueBoolean();
				return m_enabled;
			}
		}

		public ApplicableValue<UnityEngine.UI.ContentSizeFitter.FitMode> HorizontalFit
		{
			get
			{
				if (m_horizontalFit == null)
					m_horizontalFit = new ApplicableValueFitMode();
				return m_horizontalFit;
			}
		}

		public ApplicableValue<UnityEngine.UI.ContentSizeFitter.FitMode> VerticalFit
		{
			get
			{
				if (m_verticalFit == null)
					m_verticalFit = new ApplicableValueFitMode();
				return m_verticalFit;
			}
		}

	}
}
