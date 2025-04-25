// Auto-generated, please do not change!
using System;
using System.Collections.Generic;
using UnityEngine;
using GuiToolkit;
using GuiToolkit.Style;

namespace GuiToolkit.Style
{
	[Serializable]
	public class UiStyleUiHorizontalOrVerticalLayoutGroup : UiAbstractStyle<GuiToolkit.UiHorizontalOrVerticalLayoutGroup>
	{
		public UiStyleUiHorizontalOrVerticalLayoutGroup(UiStyleConfig _styleConfig, string _name)
		{
			StyleConfig = _styleConfig;
			Name = _name;
		}

		private class ApplicableValueTextAnchor : ApplicableValue<UnityEngine.TextAnchor> {}
		private class ApplicableValueBoolean : ApplicableValue<System.Boolean> {}
		private class ApplicableValueRectOffset : ApplicableValue<UnityEngine.RectOffset> {}
		private class ApplicableValueSingle : ApplicableValue<System.Single> {}

		protected override ApplicableValueBase[] GetValueArray()
		{
			return new ApplicableValueBase[]
			{
				ChildAlignment,
				ChildControlHeight,
				ChildControlWidth,
				ChildForceExpandHeight,
				ChildForceExpandWidth,
				ChildScaleHeight,
				ChildScaleWidth,
				Enabled,
				Padding,
				ReverseArrangement,
				ReverseOrder,
				Spacing,
				Vertical,
			};
		}

#if UNITY_EDITOR
		public override ValueInfo[] GetValueInfoArray()
		{
			return new ValueInfo[]
			{
				new ValueInfo()
				{
					GetterName = "ChildAlignment",
					GetterType = typeof(ApplicableValueTextAnchor),
					Value = ChildAlignment,
				},
				new ValueInfo()
				{
					GetterName = "ChildControlHeight",
					GetterType = typeof(ApplicableValueBoolean),
					Value = ChildControlHeight,
				},
				new ValueInfo()
				{
					GetterName = "ChildControlWidth",
					GetterType = typeof(ApplicableValueBoolean),
					Value = ChildControlWidth,
				},
				new ValueInfo()
				{
					GetterName = "ChildForceExpandHeight",
					GetterType = typeof(ApplicableValueBoolean),
					Value = ChildForceExpandHeight,
				},
				new ValueInfo()
				{
					GetterName = "ChildForceExpandWidth",
					GetterType = typeof(ApplicableValueBoolean),
					Value = ChildForceExpandWidth,
				},
				new ValueInfo()
				{
					GetterName = "ChildScaleHeight",
					GetterType = typeof(ApplicableValueBoolean),
					Value = ChildScaleHeight,
				},
				new ValueInfo()
				{
					GetterName = "ChildScaleWidth",
					GetterType = typeof(ApplicableValueBoolean),
					Value = ChildScaleWidth,
				},
				new ValueInfo()
				{
					GetterName = "Enabled",
					GetterType = typeof(ApplicableValueBoolean),
					Value = Enabled,
				},
				new ValueInfo()
				{
					GetterName = "Padding",
					GetterType = typeof(ApplicableValueRectOffset),
					Value = Padding,
				},
				new ValueInfo()
				{
					GetterName = "ReverseArrangement",
					GetterType = typeof(ApplicableValueBoolean),
					Value = ReverseArrangement,
				},
				new ValueInfo()
				{
					GetterName = "ReverseOrder",
					GetterType = typeof(ApplicableValueBoolean),
					Value = ReverseOrder,
				},
				new ValueInfo()
				{
					GetterName = "Spacing",
					GetterType = typeof(ApplicableValueSingle),
					Value = Spacing,
				},
				new ValueInfo()
				{
					GetterName = "Vertical",
					GetterType = typeof(ApplicableValueBoolean),
					Value = Vertical,
				},
			};
		}
#endif

		[SerializeReference] private ApplicableValueTextAnchor m_childAlignment = new();
		[SerializeReference] private ApplicableValueBoolean m_childControlHeight = new();
		[SerializeReference] private ApplicableValueBoolean m_childControlWidth = new();
		[SerializeReference] private ApplicableValueBoolean m_childForceExpandHeight = new();
		[SerializeReference] private ApplicableValueBoolean m_childForceExpandWidth = new();
		[SerializeReference] private ApplicableValueBoolean m_childScaleHeight = new();
		[SerializeReference] private ApplicableValueBoolean m_childScaleWidth = new();
		[SerializeReference] private ApplicableValueBoolean m_enabled = new();
		[SerializeReference] private ApplicableValueRectOffset m_padding = new();
		[SerializeReference] private ApplicableValueBoolean m_reverseArrangement = new();
		[SerializeReference] private ApplicableValueBoolean m_ReverseOrder = new();
		[SerializeReference] private ApplicableValueSingle m_spacing = new();
		[SerializeReference] private ApplicableValueBoolean m_Vertical = new();

		public ApplicableValue<UnityEngine.TextAnchor> ChildAlignment
		{
			get
			{
				if (m_childAlignment == null)
					m_childAlignment = new ApplicableValueTextAnchor();
				return m_childAlignment;
			}
		}

		public ApplicableValue<System.Boolean> ChildControlHeight
		{
			get
			{
				if (m_childControlHeight == null)
					m_childControlHeight = new ApplicableValueBoolean();
				return m_childControlHeight;
			}
		}

		public ApplicableValue<System.Boolean> ChildControlWidth
		{
			get
			{
				if (m_childControlWidth == null)
					m_childControlWidth = new ApplicableValueBoolean();
				return m_childControlWidth;
			}
		}

		public ApplicableValue<System.Boolean> ChildForceExpandHeight
		{
			get
			{
				if (m_childForceExpandHeight == null)
					m_childForceExpandHeight = new ApplicableValueBoolean();
				return m_childForceExpandHeight;
			}
		}

		public ApplicableValue<System.Boolean> ChildForceExpandWidth
		{
			get
			{
				if (m_childForceExpandWidth == null)
					m_childForceExpandWidth = new ApplicableValueBoolean();
				return m_childForceExpandWidth;
			}
		}

		public ApplicableValue<System.Boolean> ChildScaleHeight
		{
			get
			{
				if (m_childScaleHeight == null)
					m_childScaleHeight = new ApplicableValueBoolean();
				return m_childScaleHeight;
			}
		}

		public ApplicableValue<System.Boolean> ChildScaleWidth
		{
			get
			{
				if (m_childScaleWidth == null)
					m_childScaleWidth = new ApplicableValueBoolean();
				return m_childScaleWidth;
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

		public ApplicableValue<UnityEngine.RectOffset> Padding
		{
			get
			{
				if (m_padding == null)
					m_padding = new ApplicableValueRectOffset();
				return m_padding;
			}
		}

		public ApplicableValue<System.Boolean> ReverseArrangement
		{
			get
			{
				if (m_reverseArrangement == null)
					m_reverseArrangement = new ApplicableValueBoolean();
				return m_reverseArrangement;
			}
		}

		public ApplicableValue<System.Boolean> ReverseOrder
		{
			get
			{
				if (m_ReverseOrder == null)
					m_ReverseOrder = new ApplicableValueBoolean();
				return m_ReverseOrder;
			}
		}

		public ApplicableValue<System.Single> Spacing
		{
			get
			{
				if (m_spacing == null)
					m_spacing = new ApplicableValueSingle();
				return m_spacing;
			}
		}

		public ApplicableValue<System.Boolean> Vertical
		{
			get
			{
				if (m_Vertical == null)
					m_Vertical = new ApplicableValueBoolean();
				return m_Vertical;
			}
		}

	}
}
