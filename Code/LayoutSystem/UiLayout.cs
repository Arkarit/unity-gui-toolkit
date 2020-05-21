using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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
		public enum PlaceChildrenPolicy
		{
			None,					// Children are placed on the axis according their sizes.
			SpaceChildrenEvenly,	// Children are spaced evenly on the axis.
			StretchChildren,		// Children are stretched to match the extents of the layout (if they are marked as 'Master' or 'Flexible'
		}

		public enum HullPolicy
		{
			None,					// Layout extents don't change
			FitContentSize,			// Layout fits the children content size (plus padding/spacing) on the axis
		}

		private const DrivenTransformProperties DRIVEN_TRANSFORM_PROPERTIES =
			  DrivenTransformProperties.AnchoredPosition
			| DrivenTransformProperties.SizeDelta;


		[SerializeField]
		protected int m_numColumns = 0;

		[SerializeField]
		protected int m_numRows = 0;

		[SerializeField]
		protected bool m_errorCompatibility;

		[SerializeField]
		protected GridLayoutGroup.Corner m_startCorner = GridLayoutGroup.Corner.UpperLeft;

		[SerializeField]
		protected GridLayoutGroup.Axis m_startAxis = GridLayoutGroup.Axis.Horizontal;

		private static readonly List<UiLayoutElement> s_layoutElements = new List<UiLayoutElement>();

		private DrivenRectTransformTracker m_tracker;
		private bool m_dirty = true;

		private int m_actualColumns;
		private int m_actualRows;

		private struct CellInfo
		{
			public UiLayoutElement LayoutElement;
			public Rect Rect;

			public bool Invalid => LayoutElement == null;

			public float GetSize(bool _isHorizontal)
			{
				return _isHorizontal ? Rect.width : Rect.height;
			}

			public void SetSize(bool _isHorizontal, float _val)
			{
				if (_isHorizontal)
					Rect.width = _val;
				else
					Rect.height = _val;
			}

			public float GetPosition(bool _isHorizontal)
			{
				return _isHorizontal ? Rect.x : Rect.y;
			}

			public void SetPosition(bool _isHorizontal, float _val)
			{
				if (_isHorizontal)
					Rect.x = _val;
				else
					Rect.y = _val;
			}
		}

		private CellInfo[,] m_cellInfos;

		private bool m_fixedColumns;

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
			SetCellInfoPositions();
			AlignAndApplyCellInfos();

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
						Rect = new Rect(0,0, elem.PreferredWidth, elem.PreferredHeight)
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
					if (cellInfo.Invalid)
						continue;

					if (cellInfo.LayoutElement.HeightPolicy.IsMaster)
						rowMasterHeight[rowIdx] = Mathf.Max(rowMasterHeight[rowIdx], cellInfo.Rect.height);

					if (cellInfo.LayoutElement.WidthPolicy.IsMaster)
						columnMasterWidth[columnIdx] = Mathf.Max(columnMasterWidth[columnIdx], cellInfo.Rect.width);
				}
			}

			for (int rowIdx=0; rowIdx<m_actualRows; rowIdx++)
			{
				for (int columnIdx = 0; columnIdx<m_actualColumns; columnIdx++)
				{
					CellInfo cellInfo = m_cellInfos[columnIdx, rowIdx];
					if (cellInfo.Invalid)
						continue;

					Vector2 size = cellInfo.Rect.size;

					if (rowMasterHeight[rowIdx] >= 0)
						size.y = rowMasterHeight[rowIdx];

					if (columnMasterWidth[columnIdx] >= 0)
						size.x = columnMasterWidth[columnIdx];

					cellInfo.Rect.size = size;

					m_cellInfos[columnIdx, rowIdx] = cellInfo;
				}
			}
		}

		private void SetCellInfoPositions()
		{
			Vector2 overallSize = new Vector2(-1,-1);

			float[] yPos = new float[m_actualColumns];

			for (int rowIdx=0; rowIdx<m_actualRows; rowIdx++)
			{
				float xPos = 0;
				for (int columnIdx = 0; columnIdx<m_actualColumns; columnIdx++)
				{
					CellInfo cellInfo = m_cellInfos[columnIdx, rowIdx];
					if (cellInfo.Invalid)
						continue;

					cellInfo.Rect.x += xPos;
					cellInfo.Rect.y -= yPos[columnIdx];

					m_cellInfos[columnIdx, rowIdx] = cellInfo;

					xPos += cellInfo.Rect.width;
					yPos[columnIdx] += cellInfo.Rect.height;

					overallSize.x = Mathf.Max(overallSize.x, xPos);
					overallSize.y = Mathf.Max(overallSize.y, yPos[columnIdx]);
				}
			}

			bool swapHorizontal = m_actualColumns > 1 && m_startCorner == GridLayoutGroup.Corner.UpperRight || m_startCorner == GridLayoutGroup.Corner.LowerRight;
			bool swapVertical = m_actualRows > 1 && m_startCorner == GridLayoutGroup.Corner.LowerLeft || m_startCorner == GridLayoutGroup.Corner.LowerRight;

			if (!swapHorizontal && !swapVertical)
				return;

			for (int rowIdx=0; rowIdx<m_actualRows; rowIdx++)
			{
				for (int columnIdx = 0; columnIdx<m_actualColumns; columnIdx++)
				{
					CellInfo cellInfo = m_cellInfos[columnIdx, rowIdx];
					if (cellInfo.Invalid)
						continue;

					if (swapHorizontal)
						cellInfo.Rect.x = overallSize.x - cellInfo.Rect.x - cellInfo.Rect.width;
					if (swapVertical)
						cellInfo.Rect.y = -(overallSize.y + cellInfo.Rect.y - cellInfo.Rect.height);

					m_cellInfos[columnIdx, rowIdx] = cellInfo;
				}
			}

		}

		private void AlignAndApplyCellInfos()
		{
			for (int rowIdx=0; rowIdx<m_actualRows; rowIdx++)
			{
				for (int columnIdx = 0; columnIdx<m_actualColumns; columnIdx++)
				{
					CellInfo cellInfo = m_cellInfos[columnIdx, rowIdx];
					if (cellInfo.Invalid)
						continue;
					var rt = cellInfo.LayoutElement.RectTransform;
					rt.anchoredPosition = cellInfo.Rect.position;
					rt.sizeDelta = cellInfo.Rect.size;
				}
			}
		}

		private int GetElemIdx(int _columnIdx, int _rowIdx)
		{
			int result;

			if (m_startAxis == GridLayoutGroup.Axis.Vertical)
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