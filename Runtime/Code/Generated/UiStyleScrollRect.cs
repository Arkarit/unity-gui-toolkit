// Auto-generated, please do not change!
using System;
using UnityEngine;
using GuiToolkit;
using GuiToolkit.Style;

namespace GuiToolkit.Style
{
	[Serializable]
	public class UiStyleScrollRect : UiAbstractStyle<UnityEngine.UI.ScrollRect>
	{
		public UiStyleScrollRect(UiStyleConfig _styleConfig, string _name)
		{
			StyleConfig = _styleConfig;
			Name = _name;
		}

		private class ApplicableValueBoolean : ApplicableValue<System.Boolean> {}

		protected override ApplicableValueBase[] GetValueList()
		{
			return new ApplicableValueBase[]
			{
				Enabled,
				Horizontal,
				Vertical,
			};
		}

		[SerializeReference] private ApplicableValueBoolean m_enabled = new();
		[SerializeReference] private ApplicableValueBoolean m_horizontal = new();
		[SerializeReference] private ApplicableValueBoolean m_vertical = new();

		public ApplicableValue<System.Boolean> Enabled
		{
			get
			{
				if (m_enabled == null)
					m_enabled = new ApplicableValueBoolean();
				return m_enabled;
			}
		}

		public ApplicableValue<System.Boolean> Horizontal
		{
			get
			{
				if (m_horizontal == null)
					m_horizontal = new ApplicableValueBoolean();
				return m_horizontal;
			}
		}

		public ApplicableValue<System.Boolean> Vertical
		{
			get
			{
				if (m_vertical == null)
					m_vertical = new ApplicableValueBoolean();
				return m_vertical;
			}
		}

	}
}
