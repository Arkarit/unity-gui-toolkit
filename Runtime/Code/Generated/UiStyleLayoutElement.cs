// Auto-generated, please do not change!
using System;
using System.Collections.Generic;
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

		protected override ApplicableValueBase[] GetValueArray()
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

#if UNITY_EDITOR
		public override List<ValueInfo> GetValueInfos()
		{
			return new List<ValueInfo>()
			{
				new ValueInfo()
				{
					GetterName = "Enabled",
					GetterType = typeof(ApplicableValueBoolean),
					Value = Enabled,
				},
				new ValueInfo()
				{
					GetterName = "FlexibleHeight",
					GetterType = typeof(ApplicableValueSingle),
					Value = FlexibleHeight,
				},
				new ValueInfo()
				{
					GetterName = "FlexibleWidth",
					GetterType = typeof(ApplicableValueSingle),
					Value = FlexibleWidth,
				},
				new ValueInfo()
				{
					GetterName = "IgnoreLayout",
					GetterType = typeof(ApplicableValueBoolean),
					Value = IgnoreLayout,
				},
				new ValueInfo()
				{
					GetterName = "LayoutPriority",
					GetterType = typeof(ApplicableValueInt32),
					Value = LayoutPriority,
				},
				new ValueInfo()
				{
					GetterName = "MinHeight",
					GetterType = typeof(ApplicableValueSingle),
					Value = MinHeight,
				},
				new ValueInfo()
				{
					GetterName = "MinWidth",
					GetterType = typeof(ApplicableValueSingle),
					Value = MinWidth,
				},
				new ValueInfo()
				{
					GetterName = "PreferredHeight",
					GetterType = typeof(ApplicableValueSingle),
					Value = PreferredHeight,
				},
				new ValueInfo()
				{
					GetterName = "PreferredWidth",
					GetterType = typeof(ApplicableValueSingle),
					Value = PreferredWidth,
				},
			};
		}
#endif

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
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_enabled == null)
						m_enabled = new ApplicableValueBoolean();
				#endif
				return m_enabled;
			}
		}

		public ApplicableValue<System.Single> FlexibleHeight
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_flexibleHeight == null)
						m_flexibleHeight = new ApplicableValueSingle();
				#endif
				return m_flexibleHeight;
			}
		}

		public ApplicableValue<System.Single> FlexibleWidth
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_flexibleWidth == null)
						m_flexibleWidth = new ApplicableValueSingle();
				#endif
				return m_flexibleWidth;
			}
		}

		public ApplicableValue<System.Boolean> IgnoreLayout
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_ignoreLayout == null)
						m_ignoreLayout = new ApplicableValueBoolean();
				#endif
				return m_ignoreLayout;
			}
		}

		public ApplicableValue<System.Int32> LayoutPriority
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_layoutPriority == null)
						m_layoutPriority = new ApplicableValueInt32();
				#endif
				return m_layoutPriority;
			}
		}

		public ApplicableValue<System.Single> MinHeight
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_minHeight == null)
						m_minHeight = new ApplicableValueSingle();
				#endif
				return m_minHeight;
			}
		}

		public ApplicableValue<System.Single> MinWidth
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_minWidth == null)
						m_minWidth = new ApplicableValueSingle();
				#endif
				return m_minWidth;
			}
		}

		public ApplicableValue<System.Single> PreferredHeight
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_preferredHeight == null)
						m_preferredHeight = new ApplicableValueSingle();
				#endif
				return m_preferredHeight;
			}
		}

		public ApplicableValue<System.Single> PreferredWidth
		{
			get
			{
				#if UNITY_EDITOR
					if (!Application.isPlaying && m_preferredWidth == null)
						m_preferredWidth = new ApplicableValueSingle();
				#endif
				return m_preferredWidth;
			}
		}

	}
}
