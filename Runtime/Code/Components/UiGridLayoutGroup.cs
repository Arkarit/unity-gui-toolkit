using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit
{
	// Improved version of builtin GridLayoutGroup
	public class UiGridLayoutGroup : LayoutGroup
	{
		public enum ECorner
		{
			UpperLeft,
			UpperRight,
			LowerLeft,
			LowerRight,
		}

		public enum EAxis
		{
			Horizontal = 0,
			Vertical = 1
		}

		public enum EGridConstraint
		{
			Flexible = 0,
			FixedColumnCount = 1,
			FixedRowCount = 2
		}

		[SerializeField]
		protected ECorner m_startCorner = ECorner.UpperLeft;
		[SerializeField]
		protected EAxis m_startAxis = EAxis.Horizontal;
		[SerializeField]
		protected Vector2 m_cellSize = new Vector2(100, 100);
		[SerializeField]
		protected Vector2 m_spacing = Vector2.zero;
		[SerializeField]
		protected EGridConstraint m_constraint = EGridConstraint.Flexible;
		[SerializeField]
		protected int m_constraintCount = 2;
		[SerializeField]
		protected bool m_centerPartialFilled = true;

		public ECorner startCorner
		{
			get { return m_startCorner; }
			set { SetProperty(ref m_startCorner, value); }
		}

		public EAxis startAxis
		{
			get { return m_startAxis; }
			set { SetProperty(ref m_startAxis, value); }
		}

		public Vector2 cellSize
		{
			get { return m_cellSize; }
			set { SetProperty(ref m_cellSize, value); }
		}

		public Vector2 spacing
		{
			get { return m_spacing; }
			set { SetProperty(ref m_spacing, value); }
		}

		public EGridConstraint constraint
		{
			get { return m_constraint; }
			set { SetProperty(ref m_constraint, value); }
		}

		public int constraintCount
		{
			get { return m_constraintCount; }
			set { SetProperty(ref m_constraintCount, Mathf.Max(1, value)); }
		}

		public bool centerPartialFilled
		{
			get { return m_centerPartialFilled; }
			set { SetProperty(ref m_centerPartialFilled, value); }
		}

		protected UiGridLayoutGroup()
		{ }

#if UNITY_EDITOR
		protected override void OnValidate()
		{
			base.OnValidate();
			constraintCount = constraintCount;
		}

#endif

		public override void CalculateLayoutInputHorizontal()
		{
			base.CalculateLayoutInputHorizontal();

			int minColumns = 0;
			int preferredColumns = 0;
			if (m_constraint == EGridConstraint.FixedColumnCount)
			{
				minColumns = preferredColumns = m_constraintCount;
			}
			else if (m_constraint == EGridConstraint.FixedRowCount)
			{
				minColumns = preferredColumns = Mathf.CeilToInt(rectChildren.Count / (float)m_constraintCount - 0.001f);
			}
			else
			{
				minColumns = 1;
				preferredColumns = Mathf.CeilToInt(Mathf.Sqrt(rectChildren.Count));
			}

			SetLayoutInputForAxis(
				padding.horizontal + (cellSize.x + spacing.x) * minColumns - spacing.x,
				padding.horizontal + (cellSize.x + spacing.x) * preferredColumns - spacing.x,
				-1, 0);
		}

		public override void CalculateLayoutInputVertical()
		{
			int minRows = 0;
			if (m_constraint == EGridConstraint.FixedColumnCount)
			{
				minRows = Mathf.CeilToInt(rectChildren.Count / (float)m_constraintCount - 0.001f);
			}
			else if (m_constraint == EGridConstraint.FixedRowCount)
			{
				minRows = m_constraintCount;
			}
			else
			{
				float width = rectTransform.rect.width;
				int cellCountX = Mathf.Max(1, Mathf.FloorToInt((width - padding.horizontal + spacing.x + 0.001f) / (cellSize.x + spacing.x)));
				minRows = Mathf.CeilToInt(rectChildren.Count / (float)cellCountX);
			}

			float minSpace = padding.vertical + (cellSize.y + spacing.y) * minRows - spacing.y;
			SetLayoutInputForAxis(minSpace, minSpace, -1, 1);
		}

		public override void SetLayoutHorizontal()
		{
			SetCellsAlongAxis(0);
		}

		public override void SetLayoutVertical()
		{
			SetCellsAlongAxis(1);
		}

		private void SetCellsAlongAxis( int axis )
		{
			// Normally a Layout Controller should only set horizontal values when invoked for the horizontal axis
			// and only vertical values when invoked for the vertical axis.
			// However, in this case we set both the horizontal and vertical position when invoked for the vertical axis.
			// Since we only set the horizontal position and not the size, it shouldn't affect children's layout,
			// and thus shouldn't break the rule that all horizontal layout must be calculated before all vertical layout.

			if (axis == 0)
			{
				// Only set the sizes when invoked for horizontal axis, not the positions.
				for (int i = 0; i < rectChildren.Count; i++)
				{
					RectTransform rect = rectChildren[i];

					m_Tracker.Add(this, rect,
						DrivenTransformProperties.Anchors |
						DrivenTransformProperties.AnchoredPosition |
						DrivenTransformProperties.SizeDelta);

					rect.anchorMin = Vector2.up;
					rect.anchorMax = Vector2.up;
					rect.sizeDelta = cellSize;
				}
				return;
			}

			float width = rectTransform.rect.size.x;
			float height = rectTransform.rect.size.y;

			int cellCountX = 1;
			int cellCountY = 1;
			if (m_constraint == EGridConstraint.FixedColumnCount)
			{
				cellCountX = m_constraintCount;

				if (rectChildren.Count > cellCountX)
					cellCountY = rectChildren.Count / cellCountX + (rectChildren.Count % cellCountX > 0 ? 1 : 0);
			}
			else if (m_constraint == EGridConstraint.FixedRowCount)
			{
				cellCountY = m_constraintCount;

				if (rectChildren.Count > cellCountY)
					cellCountX = rectChildren.Count / cellCountY + (rectChildren.Count % cellCountY > 0 ? 1 : 0);
			}
			else
			{
				if (cellSize.x + spacing.x <= 0)
					cellCountX = int.MaxValue;
				else
					cellCountX = Mathf.Max(1, Mathf.FloorToInt((width - padding.horizontal + spacing.x + 0.001f) / (cellSize.x + spacing.x)));

				if (cellSize.y + spacing.y <= 0)
					cellCountY = int.MaxValue;
				else
					cellCountY = Mathf.Max(1, Mathf.FloorToInt((height - padding.vertical + spacing.y + 0.001f) / (cellSize.y + spacing.y)));
			}

			int cornerX = (int)startCorner % 2;
			int cornerY = (int)startCorner / 2;

			int cellsPerMainAxis, actualCellCountX, actualCellCountY;
			if (startAxis == EAxis.Horizontal)
			{
				cellsPerMainAxis = cellCountX;
				actualCellCountX = Mathf.Clamp(cellCountX, 1, rectChildren.Count);
				actualCellCountY = Mathf.Clamp(cellCountY, 1, Mathf.CeilToInt(rectChildren.Count / (float)cellsPerMainAxis));
			}
			else
			{
				cellsPerMainAxis = cellCountY;
				actualCellCountY = Mathf.Clamp(cellCountY, 1, rectChildren.Count);
				actualCellCountX = Mathf.Clamp(cellCountX, 1, Mathf.CeilToInt(rectChildren.Count / (float)cellsPerMainAxis));
			}

			Vector2 requiredSpace = new Vector2
			(
				actualCellCountX * cellSize.x + (actualCellCountX - 1) * spacing.x,
				actualCellCountY * cellSize.y + (actualCellCountY - 1) * spacing.y
			);
			Vector2 startOffset = new Vector2
			(
				GetStartOffset(0, requiredSpace.x),
				GetStartOffset(1, requiredSpace.y)
			);

			int numChildren = rectChildren.Count;
			int centerIdx = int.MaxValue;
			float centerOffset = 0;

			if ( m_centerPartialFilled )
			{
				int numElemsToBeCentered = numChildren % cellsPerMainAxis;
				if (numElemsToBeCentered > 0)
				{
					centerIdx = numChildren - numElemsToBeCentered;

					if (m_startAxis == EAxis.Horizontal)
						centerOffset = (cellsPerMainAxis - numElemsToBeCentered) * (cellSize[0] + spacing[0]) / 2;
					else
						centerOffset = (cellsPerMainAxis - numElemsToBeCentered) * (cellSize[1] + spacing[1]) / 2;

					if (m_startAxis == EAxis.Horizontal && m_startCorner != ECorner.UpperLeft && m_startCorner != ECorner.LowerLeft)
						centerOffset = -centerOffset;

					if (m_startAxis == EAxis.Vertical && m_startCorner != ECorner.UpperLeft && m_startCorner != ECorner.UpperRight)
						centerOffset = -centerOffset;
				}
			}

			for (int i = 0; i < numChildren; i++)
			{
				int positionX;
				int positionY;
				if (m_startAxis == EAxis.Horizontal)
				{
					positionX = i % cellsPerMainAxis;
					positionY = i / cellsPerMainAxis;
				}
				else
				{
					positionX = i / cellsPerMainAxis;
					positionY = i % cellsPerMainAxis;
				}

				if (cornerX == 1)
					positionX = actualCellCountX - 1 - positionX;
				if (cornerY == 1)
					positionY = actualCellCountY - 1 - positionY;

				float posX = startOffset.x + (cellSize[0] + spacing[0]) * positionX;
				float posY = startOffset.y + (cellSize[1] + spacing[1]) * positionY;

				if (i >= centerIdx)
				{
					if (startAxis == EAxis.Horizontal)
						posX += centerOffset;
					else
						posY += centerOffset;
				}

				SetChildAlongAxis(rectChildren[i], 0, posX, cellSize[0]);
				SetChildAlongAxis(rectChildren[i], 1, posY, cellSize[1]);
			}
		}
	}
}
