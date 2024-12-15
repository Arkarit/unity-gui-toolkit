// Auto-generated, please do not change!
using System;
using UnityEngine;
using GuiToolkit;
using GuiToolkit.Style;

namespace GuiToolkit.Style
{
	[Serializable]
	public class UiStyleLayoutElement : UiAbstractStyle<UnityEngine.UI.LayoutElement>
	{
		public UiStyleLayoutElement(UiStyleConfig _styleConfig, string _name)
		{
			StyleConfig = _styleConfig;
			Name = _name;
		}

		private class ApplicableValueBoolean : ApplicableValue<System.Boolean> {}
		private class ApplicableValueSingle : ApplicableValue<System.Single> {}
		private class ApplicableValueInt32 : ApplicableValue<System.Int32> {}

		protected override ApplicableValueBase[] GetValueList()
		{
			return new ApplicableValueBase[]
			{
				Enabled,
				FlexibleHeight,
				FlexibleWidth,
				IgnoreLayout,
				LayoutPriority,
				MinHeight,
				MinWidth,
				PreferredHeight,
				PreferredWidth,
			};
		}

		[SerializeReference] private ApplicableValueBoolean m_enabled = new();
		[SerializeReference] private ApplicableValueSingle m_flexibleHeight = new();
		[SerializeReference] private ApplicableValueSingle m_flexibleWidth = new();
		[SerializeReference] private ApplicableValueBoolean m_ignoreLayout = new();
		[SerializeReference] private ApplicableValueInt32 m_layoutPriority = new();
		[SerializeReference] private ApplicableValueSingle m_minHeight = new();
		[SerializeReference] private ApplicableValueSingle m_minWidth = new();
		[SerializeReference] private ApplicableValueSingle m_preferredHeight = new();
		[SerializeReference] private ApplicableValueSingle m_preferredWidth = new();

		public ApplicableValue<System.Boolean> Enabled
		{
			get
			{
				if (m_enabled == null)
					m_enabled = new ApplicableValueBoolean();
				return m_enabled;
			}
		}

		public ApplicableValue<System.Single> FlexibleHeight
		{
			get
			{
				if (m_flexibleHeight == null)
					m_flexibleHeight = new ApplicableValueSingle();
				return m_flexibleHeight;
			}
		}

		public ApplicableValue<System.Single> FlexibleWidth
		{
			get
			{
				if (m_flexibleWidth == null)
					m_flexibleWidth = new ApplicableValueSingle();
				return m_flexibleWidth;
			}
		}

		public ApplicableValue<System.Boolean> IgnoreLayout
		{
			get
			{
				if (m_ignoreLayout == null)
					m_ignoreLayout = new ApplicableValueBoolean();
				return m_ignoreLayout;
			}
		}

		public ApplicableValue<System.Int32> LayoutPriority
		{
			get
			{
				if (m_layoutPriority == null)
					m_layoutPriority = new ApplicableValueInt32();
				return m_layoutPriority;
			}
		}

		public ApplicableValue<System.Single> MinHeight
		{
			get
			{
				if (m_minHeight == null)
					m_minHeight = new ApplicableValueSingle();
				return m_minHeight;
			}
		}

		public ApplicableValue<System.Single> MinWidth
		{
			get
			{
				if (m_minWidth == null)
					m_minWidth = new ApplicableValueSingle();
				return m_minWidth;
			}
		}

		public ApplicableValue<System.Single> PreferredHeight
		{
			get
			{
				if (m_preferredHeight == null)
					m_preferredHeight = new ApplicableValueSingle();
				return m_preferredHeight;
			}
		}

		public ApplicableValue<System.Single> PreferredWidth
		{
			get
			{
				if (m_preferredWidth == null)
					m_preferredWidth = new ApplicableValueSingle();
				return m_preferredWidth;
			}
		}

	}
}
