// Auto-generated, please do not change!
using System;
using UnityEngine;
using GuiToolkit;
using GuiToolkit.Style;

namespace GuiToolkit.Style
{
	[Serializable]
	public class UiStyleGridLayoutGroup : UiAbstractStyle<UnityEngine.UI.GridLayoutGroup>
	{
		public UiStyleGridLayoutGroup(UiStyleConfig _styleConfig, string _name)
		{
			StyleConfig = _styleConfig;
			Name = _name;
		}

		private class ApplicableValueVector2 : ApplicableValue<UnityEngine.Vector2> {}
		private class ApplicableValueTextAnchor : ApplicableValue<UnityEngine.TextAnchor> {}
		private class ApplicableValueConstraint : ApplicableValue<UnityEngine.UI.GridLayoutGroup.Constraint> {}
		private class ApplicableValueInt32 : ApplicableValue<System.Int32> {}
		private class ApplicableValueBoolean : ApplicableValue<System.Boolean> {}
		private class ApplicableValueRectOffset : ApplicableValue<UnityEngine.RectOffset> {}
		private class ApplicableValueAxis : ApplicableValue<UnityEngine.UI.GridLayoutGroup.Axis> {}
		private class ApplicableValueCorner : ApplicableValue<UnityEngine.UI.GridLayoutGroup.Corner> {}

		protected override ApplicableValueBase[] GetValueList()
		{
			return new ApplicableValueBase[]
			{
				CellSize,
				ChildAlignment,
				Constraint,
				ConstraintCount,
				Enabled,
				Padding,
				Spacing,
				StartAxis,
				StartCorner,
			};
		}

		[SerializeReference] private ApplicableValueVector2 m_cellSize = new();
		[SerializeReference] private ApplicableValueTextAnchor m_childAlignment = new();
		[SerializeReference] private ApplicableValueConstraint m_constraint = new();
		[SerializeReference] private ApplicableValueInt32 m_constraintCount = new();
		[SerializeReference] private ApplicableValueBoolean m_enabled = new();
		[SerializeReference] private ApplicableValueRectOffset m_padding = new();
		[SerializeReference] private ApplicableValueVector2 m_spacing = new();
		[SerializeReference] private ApplicableValueAxis m_startAxis = new();
		[SerializeReference] private ApplicableValueCorner m_startCorner = new();

		public ApplicableValue<UnityEngine.Vector2> CellSize
		{
			get
			{
				if (m_cellSize == null)
					m_cellSize = new ApplicableValueVector2();
				return m_cellSize;
			}
		}

		public ApplicableValue<UnityEngine.TextAnchor> ChildAlignment
		{
			get
			{
				if (m_childAlignment == null)
					m_childAlignment = new ApplicableValueTextAnchor();
				return m_childAlignment;
			}
		}

		public ApplicableValue<UnityEngine.UI.GridLayoutGroup.Constraint> Constraint
		{
			get
			{
				if (m_constraint == null)
					m_constraint = new ApplicableValueConstraint();
				return m_constraint;
			}
		}

		public ApplicableValue<System.Int32> ConstraintCount
		{
			get
			{
				if (m_constraintCount == null)
					m_constraintCount = new ApplicableValueInt32();
				return m_constraintCount;
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

		public ApplicableValue<UnityEngine.Vector2> Spacing
		{
			get
			{
				if (m_spacing == null)
					m_spacing = new ApplicableValueVector2();
				return m_spacing;
			}
		}

		public ApplicableValue<UnityEngine.UI.GridLayoutGroup.Axis> StartAxis
		{
			get
			{
				if (m_startAxis == null)
					m_startAxis = new ApplicableValueAxis();
				return m_startAxis;
			}
		}

		public ApplicableValue<UnityEngine.UI.GridLayoutGroup.Corner> StartCorner
		{
			get
			{
				if (m_startCorner == null)
					m_startCorner = new ApplicableValueCorner();
				return m_startCorner;
			}
		}

	}
}
