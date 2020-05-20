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
	/// <summary>
	/// Complete replacement for the shitty Unity layout "system".
	/// </summary>
	/// 

	[ExecuteAlways]
	public class UiLayout : UiLayoutElement
	{
		private DrivenRectTransformTracker m_tracker;
		private bool m_dirty = true;
		private static readonly List<UiLayoutElement> s_layoutElements = new List<UiLayoutElement>();
		private const DrivenTransformProperties DRIVEN_TRANSFORM_PROPERTIES =
			  DrivenTransformProperties.AnchoredPosition
			| DrivenTransformProperties.SizeDelta;


		[SerializeField]
		protected int m_numColumns = 0;

		[SerializeField]
		protected int m_numRows = 0;

		[SerializeField]
		protected GridLayoutGroup.Corner m_startCorner = GridLayoutGroup.Corner.UpperLeft;

		[SerializeField]
		protected GridLayoutGroup.Axis m_startAxis = GridLayoutGroup.Axis.Horizontal;

		private int m_actualColumns;
		private int m_actualRows;

		private Vector2[,] m_cellSizes;

		private bool m_fixedColumns;

		// Debug getters
#if UNITY_EDITOR
		public int ActualColumns => m_actualColumns;
		public int ActualRows => m_actualRows;
#endif

		public override float PreferredWidth => m_width.GetPreferredSize();
		public override float PreferredHeight => m_height.GetPreferredSize();

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
			CalcCellSizes();

			Log.Layout("UpdateLayout()");

			float[] columnY = new float[m_actualColumns];
			for (int rowIdx=0; rowIdx<m_actualRows; rowIdx++)
			{
				float x = 0;
				for (int columnIdx = 0; columnIdx<m_actualColumns; columnIdx++)
				{
					int elemIdx = GetElemIdx(columnIdx, rowIdx);

					Vector2 extents = m_cellSizes[columnIdx, rowIdx];

					if (elemIdx >= 0)
					{
						UiLayoutElement elem = s_layoutElements[elemIdx];
						RectTransform rt = elem.RectTransform;
					
						//float width = elem.Width;
						//float height = elem.Height;

						rt.sizeDelta = extents;
					
						Vector2 position = new Vector2(x, columnY[columnIdx]);

						Log.Layout($"Setting Anchored Position for {elem.gameObject.name}: {position}");

						rt.anchoredPosition = position;
					}

					x += extents.x;
					columnY[columnIdx] -= extents.y;
				}
			}

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

		private void CalcCellSizes()
		{
			Log.Layout("CalcColumnWidthsAndRowHeights()");

			m_cellSizes = new Vector2[m_actualColumns, m_actualRows];

			for (int rowIdx=0; rowIdx<m_actualRows; rowIdx++)
			{
				for (int columnIdx = 0; columnIdx<m_actualColumns; columnIdx++)
				{
					int elemIdx = GetElemIdx(columnIdx, rowIdx);
					if (elemIdx == -1)
						continue;

					UiLayoutElement elem = s_layoutElements[elemIdx];
					
					float width = elem.PreferredWidth;
					float height = elem.PreferredHeight;

					m_cellSizes[columnIdx, rowIdx] = new Vector2(width, height);
				}
			}
		}

		private int GetElemIdx(int _columnIdx, int _rowIdx)
		{
			int result;

			int columnIdx = _columnIdx;
			int rowIdx = _rowIdx;

			// For right to left/bottom to top we simply invert column/row
			if (m_startCorner == GridLayoutGroup.Corner.LowerRight || m_startCorner == GridLayoutGroup.Corner.UpperRight)
				columnIdx = m_actualColumns - columnIdx - 1;
			if (m_startCorner == GridLayoutGroup.Corner.LowerLeft || m_startCorner == GridLayoutGroup.Corner.LowerRight)
				rowIdx = m_actualRows - rowIdx - 1;

			// Add elements to columns first requires a special handling.
			if (m_startAxis == GridLayoutGroup.Axis.Vertical)
			{
				// Fixed columns even more.
				if (m_fixedColumns)
				{
					if (columnIdx + rowIdx * m_actualColumns >= s_layoutElements.Count )
						return -1;

					int fullColumns = m_actualColumns - (m_actualColumns * m_actualRows - s_layoutElements.Count);
					Log.Layout(fullColumns.ToString());
					if (columnIdx > fullColumns)
					{
						result = rowIdx + columnIdx * m_actualRows - (columnIdx - fullColumns);
					}
					else
					{
						result = rowIdx + columnIdx * m_actualRows;
					}
				}
				else
				{
					result = rowIdx + columnIdx * m_actualRows;
				}
			}
			else
			{
				result = columnIdx + rowIdx * m_actualColumns;
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