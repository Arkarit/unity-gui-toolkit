using System.Collections.Generic;
using UnityEngine;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GuiToolkit.Layout
{
	/// \brief Complete replacement for the shitty Unity layout system.
	/// 
	/// Benefits:
	/// <UL>
	/// <LI>Super-flexible. There's only one layout class, which can fulfill the roles of horizontal, vertical and grid layout</LI>
	/// <LI>Unity layouts are not only shitty by design, they even are buggy in terms of ordering their children under some circumstances. UiLayout fixes this. (disableable)
	/// <LI>Flexible grid layout cell sizes</LI>
	/// <LI>If a grid layout row/column is not completely filled, it can be centered (eventually!)</LI>
	/// <LI>Elements can determine the width/height of a complete table column/row</LI>
	/// <LI>Much more understandable UI terminology</LI>
	/// <LI>Because of the points above, much less nested layouts necessary</LI>
	/// </UL>
	/// 

	[ExecuteAlways]
	public class UiLayout : UiLayoutElement
	{
		public enum ChildrenAlignmentPolicy
		{
			Minimum,		
			Center,			
			Maximum,		
			FitToChildrenContent,
			StretchChildren,
		}

		private const DrivenTransformProperties DRIVEN_TRANSFORM_PROPERTIES =
			  DrivenTransformProperties.AnchoredPosition
			| DrivenTransformProperties.SizeDelta;

		[SerializeField]
		protected RectOffset m_padding = new RectOffset();

		[SerializeField]
		protected int m_numColumns = 0;

		[SerializeField]
		protected int m_numRows = 0;

		[SerializeField]
		protected bool m_rightToLeft;

		[SerializeField]
		protected bool m_bottomToTop;

		[SerializeField]
		protected bool m_fillVerticalFirst;

		[SerializeField]
		protected ChildrenAlignmentPolicy m_childrenAlignmentHorizontal;

		[SerializeField]
		protected ChildrenAlignmentPolicy m_childrenAlignmentVertical;

		[SerializeField]
		protected bool m_errorCompatibility;

		private static readonly List<UiLayoutElement> s_layoutElements = new List<UiLayoutElement>();

		private DrivenRectTransformTracker m_tracker;
		private bool m_dirty = true;

		private int m_actualColumns;
		private int m_actualRows;

		private class CellInfo
		{
			public UiLayoutElement LayoutElement;
			public Rect CellRect;
			public Rect ElementRect;
		}

		private CellInfo[,] m_cellInfos;

		private bool m_fixedColumns;

		private Vector2 m_overallSize;

		// Debug getters
#if UNITY_EDITOR
		public int ActualColumns => m_actualColumns;
		public int ActualRows => m_actualRows;
#endif

		public override float PreferredWidth => m_width.PreferredSize;
		public override float PreferredHeight => m_height.PreferredSize;

		public void SetDirty()
		{
			m_dirty = true;
		}

		// note: we use update for now. Later, we need to add a dirtying system
		private void Update()
		{
			if (!m_dirty)
				return;

			UpdateLayout();
		}

		private void OnTransformChildrenChanged()
		{
			SetDirty();	
		}

		private void UpdateLayout()
		{
			transform.GetComponentsInChildren(s_layoutElements, false, false);
			int numElements = s_layoutElements.Count;

			SetActualColumnsAndRows();
			FillCellInfos();
			AdjustCellInfoSizes();
			SetCellInfoPositionsAndOverallSize();
			AlignCellInfosIfNecessary();
			StretchCellInfosIfNecessary();
			ApplyCellInfos();

			// Place supernatant elements off-screen. We neither want to mess with game object activeness nor
			// with scale, to not hinder the user to use these important properties.
			for (int i = m_actualRows * m_actualColumns; i < numElements; i++)
			{
				s_layoutElements[i].RectTransform.anchoredPosition = new Vector2(-100000, 100000);
			}

			m_tracker.Clear();
			foreach (var child in s_layoutElements)
				m_tracker.Add(this, child.RectTransform, DRIVEN_TRANSFORM_PROPERTIES);

			// m_dirty = false;
		}

		private void OnValidate()
		{
			if (m_numColumns < 0)
				m_numColumns = 0;
			if (m_numRows < 0)
				m_numRows = 0;
		}

		private bool SetActualColumnsAndRows()
		{
			m_fixedColumns = true;

			m_actualColumns = 0;
			m_actualRows = 0;
			if (s_layoutElements.Empty())
				return false;

			if (m_numRows < 0 || m_numColumns < 0)
				return false;

			if (m_numColumns == 0)
			{
				if (m_numRows == 0)
				{
					m_actualColumns = s_layoutElements.Count;
					m_actualRows = 1;
					return true;
				}

				m_fixedColumns = false;
				m_actualRows = m_numRows;
				m_actualColumns = s_layoutElements.Count / m_actualRows;
				if (s_layoutElements.Count % m_actualRows > 0)
					m_actualColumns++;
				return true;
			}

			m_actualColumns = m_numColumns;

			if (m_numRows == 0)
			{
				m_actualRows = s_layoutElements.Count / m_actualColumns;
				if (s_layoutElements.Count % m_actualColumns > 0)
					m_actualRows++;
				return true;
			}

			m_actualRows = m_numRows;
			return true;
		}

		private void FillCellInfos()
		{
			Log.Layout("FillCellInfos");

			m_cellInfos = new CellInfo[m_actualColumns, m_actualRows];

			for (int rowIdx=0; rowIdx<m_actualRows; rowIdx++)
			{
				for (int columnIdx = 0; columnIdx<m_actualColumns; columnIdx++)
				{
					int elemIdx = GetElemIdx(columnIdx, rowIdx);
					if (elemIdx == -1)
						continue;

					UiLayoutElement elem = s_layoutElements[elemIdx];

					CellInfo cellInfo = new CellInfo
					{
						LayoutElement = elem,
						CellRect = new Rect(0,0, elem.PreferredWidth, elem.PreferredHeight)
					};

					m_cellInfos[columnIdx, rowIdx] = cellInfo;
				}
			}
		}

		private void AdjustCellInfoSizes()
		{
			float[] columnMasterWidth = new float[m_actualColumns];
			for (int i=0; i<m_actualColumns; i++)
				columnMasterWidth[i] = -1;

			float[] rowMasterHeight = new float[m_actualRows];
			for (int i=0; i<m_actualRows; i++)
				rowMasterHeight[i] = -1;

			for (int rowIdx=0; rowIdx<m_actualRows; rowIdx++)
			{
				for (int columnIdx = 0; columnIdx<m_actualColumns; columnIdx++)
				{
					CellInfo cellInfo = m_cellInfos[columnIdx, rowIdx];
					if (cellInfo == null)
						continue;

					if (cellInfo.LayoutElement.HeightPolicy.IsMaster)
						rowMasterHeight[rowIdx] = Mathf.Max(rowMasterHeight[rowIdx], cellInfo.CellRect.height);

					if (cellInfo.LayoutElement.WidthPolicy.IsMaster)
						columnMasterWidth[columnIdx] = Mathf.Max(columnMasterWidth[columnIdx], cellInfo.CellRect.width);
				}
			}

			for (int rowIdx=0; rowIdx<m_actualRows; rowIdx++)
			{
				for (int columnIdx = 0; columnIdx<m_actualColumns; columnIdx++)
				{
					CellInfo cellInfo = m_cellInfos[columnIdx, rowIdx];
					if (cellInfo == null)
						continue;

					Vector2 size = cellInfo.CellRect.size;

					if (rowMasterHeight[rowIdx] >= 0)
						size.y = rowMasterHeight[rowIdx];

					if (columnMasterWidth[columnIdx] >= 0)
						size.x = columnMasterWidth[columnIdx];

					cellInfo.CellRect.size = size;
				}
			}
		}

		private void SetCellInfoPositionsAndOverallSize()
		{
			m_overallSize = new Vector2(-1,-1);

			float[] yPos = new float[m_actualColumns];

			for (int rowIdx=0; rowIdx<m_actualRows; rowIdx++)
			{
				float xPos = 0;
				for (int columnIdx = 0; columnIdx<m_actualColumns; columnIdx++)
				{
					CellInfo cellInfo = m_cellInfos[columnIdx, rowIdx];
					if (cellInfo == null)
						continue;

					cellInfo.CellRect.x += xPos;
					cellInfo.CellRect.y -= yPos[columnIdx];

					xPos += cellInfo.CellRect.width;
					yPos[columnIdx] += cellInfo.CellRect.height;

					m_overallSize.x = Mathf.Max(m_overallSize.x, xPos);
					m_overallSize.y = Mathf.Max(m_overallSize.y, yPos[columnIdx]);
				}
			}

			if (!m_rightToLeft && !m_bottomToTop)
				return;

			for (int rowIdx=0; rowIdx<m_actualRows; rowIdx++)
			{
				for (int columnIdx = 0; columnIdx<m_actualColumns; columnIdx++)
				{
					CellInfo cellInfo = m_cellInfos[columnIdx, rowIdx];
					if (cellInfo == null)
						continue;

					if (m_rightToLeft)
						cellInfo.CellRect.x = m_overallSize.x - cellInfo.CellRect.x - cellInfo.CellRect.width;
					if (m_bottomToTop)
						cellInfo.CellRect.y = -(m_overallSize.y + cellInfo.CellRect.y - cellInfo.CellRect.height);
				}
			}

		}

		private void AlignCellInfosIfNecessary()
		{
			bool alignHorizontal = m_childrenAlignmentHorizontal <= ChildrenAlignmentPolicy.Maximum;
			bool alignVertical = m_childrenAlignmentVertical <= ChildrenAlignmentPolicy.Maximum;
			if (!alignHorizontal && !alignVertical)
				return;

			Rect thisRect = RectTransform.rect;
			thisRect = m_padding.Remove(thisRect);

			Vector2 ratio = m_overallSize / thisRect.width;

			for (int rowIdx=0; rowIdx<m_actualRows; rowIdx++)
			{
				for (int columnIdx = 0; columnIdx<m_actualColumns; columnIdx++)
				{
					CellInfo cellInfo = m_cellInfos[columnIdx, rowIdx];
					if (cellInfo == null)
						continue;

					if (alignHorizontal)
					{
						float x = cellInfo.CellRect.x;
						float w = cellInfo.CellRect.width;
						AlignAxis(m_childrenAlignmentHorizontal, 1, m_overallSize.x, thisRect.width, ref x, ref w);
						cellInfo.CellRect.x = x + m_padding.left;
						cellInfo.CellRect.width = w;
					}

					if (alignVertical)
					{
						float y = cellInfo.CellRect.y;
						float h = cellInfo.CellRect.height;
						AlignAxis(m_childrenAlignmentVertical, -1, m_overallSize.y, thisRect.height, ref y, ref h);
						cellInfo.CellRect.y = y - m_padding.top;
						cellInfo.CellRect.height = h;
					}
				}
			}
		}

		private void AlignAxis(ChildrenAlignmentPolicy _policy, float _sgn, float _innerSize, float _outerSize, ref float _pos, ref float _size )
		{
			switch( _policy )
			{
				case ChildrenAlignmentPolicy.Center:
					{
						float offset = _outerSize - _innerSize;
						_pos += offset / 2 * _sgn;
					}
					break;
				case ChildrenAlignmentPolicy.Maximum:
					{
						float offset = _outerSize - _innerSize;
						_pos += offset * _sgn;
					}
					break;
				default:
					return;
			}
		}

		private void SetStretchForAxis( EAxis2D _axis )
		{
			void AddFullAndStretchableSpace( int _idxA, int _idxB, ref float _fullSpaceIs, ref float _stretchableSpaceIs )
			{
				if (_axis == EAxis2D.Horizontal)
					UiMathUtility.Swap(ref _idxA, ref _idxB);

				CellInfo cellInfo = m_cellInfos[_idxA, _idxB];
				if (cellInfo == null)
					return;
				float space = cellInfo.CellRect.GetAxisSize(_axis);
				_fullSpaceIs += space;
				if (cellInfo.LayoutElement.GetTransformPolicy(_axis).IsFlexible)
					_stretchableSpaceIs += space;
			}
			void SetStretch( int _idxA, int _idxB, float _factor, ref float _axisPos )
			{
				if (_axis == EAxis2D.Horizontal)
					UiMathUtility.Swap(ref _idxA, ref _idxB);

				CellInfo cellInfo = m_cellInfos[_idxA, _idxB];
				if (cellInfo == null)
					return;

				if (cellInfo.LayoutElement.GetTransformPolicy(_axis).IsFlexible)
					cellInfo.CellRect.SetAxisSize(_axis, cellInfo.CellRect.GetAxisSize(_axis) * _factor);

				float sgn = _axis == EAxis2D.Horizontal ? 1 : -1;
				cellInfo.CellRect.SetAxisPosition(_axis, _axisPos * sgn);
				_axisPos += cellInfo.CellRect.GetAxisSize(_axis);
			}

			Rect thisRect = RectTransform.rect;
			thisRect = m_padding.Remove(thisRect);

			int sizeA = _axis == EAxis2D.Horizontal ? m_actualRows : m_actualColumns;
			int sizeB = _axis == EAxis2D.Horizontal ? m_actualColumns : m_actualRows;

			for (int idxA = 0; idxA < sizeA; idxA++)
			{
				float stretchableSpaceIs = 0;
				float fullSpaceIs = 0;

				for (int idxB = 0; idxB < sizeB; idxB++)
					AddFullAndStretchableSpace(idxA, idxB, ref fullSpaceIs, ref stretchableSpaceIs);

				float fixedSpaceIs = fullSpaceIs - stretchableSpaceIs;
				float fullSpaceShould = thisRect.GetAxisSize(_axis);
				float factor = (fullSpaceShould - fixedSpaceIs) / (fullSpaceIs - fixedSpaceIs);

				float axisPos = m_padding.left;
				if (stretchableSpaceIs > 0)
				{
					for (int idxB = 0; idxB < m_actualColumns; idxB++)
						SetStretch(idxA, idxB, factor, ref axisPos);
				}
			}
		}

		private void StretchCellInfosIfNecessary()
		{
			bool stretchHorizontal = m_childrenAlignmentHorizontal == ChildrenAlignmentPolicy.StretchChildren;
			bool stretchVertical = m_childrenAlignmentVertical == ChildrenAlignmentPolicy.StretchChildren;
			if (!stretchHorizontal && !stretchVertical)
				return;

			if (stretchHorizontal)
				SetStretchForAxis(EAxis2D.Horizontal);
			if (stretchVertical)
				SetStretchForAxis(EAxis2D.Vertical);
		}

		private void ApplyCellInfos()
		{
			for (int rowIdx=0; rowIdx<m_actualRows; rowIdx++)
			{
				for (int columnIdx = 0; columnIdx<m_actualColumns; columnIdx++)
				{
					CellInfo cellInfo = m_cellInfos[columnIdx, rowIdx];
					if (cellInfo == null)
						continue;
					var rt = cellInfo.LayoutElement.RectTransform;
Log.Layout($"y on apply:{cellInfo.CellRect.position.y}");
					rt.anchoredPosition = cellInfo.CellRect.position;
					rt.sizeDelta = cellInfo.CellRect.size;
				}
			}
		}

		private int GetElemIdx(int _columnIdx, int _rowIdx)
		{
			int result;

			if (m_fillVerticalFirst)
			{
				int rest = 0;

				if (!m_errorCompatibility && m_fixedColumns)
				{
					if (_columnIdx + _rowIdx * m_actualColumns >= s_layoutElements.Count )
						return -1;

					int fullColumns = m_actualColumns - (m_actualColumns * m_actualRows - s_layoutElements.Count);
					if (_columnIdx > fullColumns)
						rest = _columnIdx - fullColumns;
				}

				result = _rowIdx + _columnIdx * m_actualRows - rest;
			}
			else
			{
				result = _columnIdx + _rowIdx * m_actualColumns;
			}

			if (result >= s_layoutElements.Count)
				return -1;

			Log.Layout($"columnIdx:{_columnIdx} rowIdx:{_rowIdx} result:{result} gameObject:{s_layoutElements[result].name}");

			return result;
		}
	}

#if UNITY_EDITOR
	[CustomEditor(typeof(UiLayout))]
	public class UiLayoutEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			DrawDefaultInspector();
			UiLayout thisUiLayout = target as UiLayout;

			EditorGUILayout.Space();
			EditorGUILayout.LabelField($"m_actualRows:" + thisUiLayout.ActualRows );
			EditorGUILayout.LabelField($"m_actualColumns:" + thisUiLayout.ActualColumns );
		}
	}
#endif
}