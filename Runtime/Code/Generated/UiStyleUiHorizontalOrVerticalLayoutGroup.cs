// Auto-generated, please do not change!
using System;
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

		protected override ApplicableValueBase[] GetValueList()
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
