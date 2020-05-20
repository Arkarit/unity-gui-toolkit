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


		[Tooltip("Number of Columns. '0' means 'unlimited'. For a horizontal only layout, enter '0' here. For a vertical only layout, enter '1' here.")]
		[SerializeField]
		int m_numColumns = 0;
		[Tooltip("Number of Rows. '0' means 'unlimited'. For a horizontal only layout, enter '1' here. For a vertical only layout, enter '0' here.")]
		[SerializeField]
		int m_numRows = 0;
		[SerializeField]
		bool m_columnsFirst;
		[SerializeField]
		bool m_rightToLeft;
		[SerializeField]
		bool m_bottomToTop;

		private int m_actualColumns;
		private int m_actualRows;

		private float[] m_columnWidths;
		private float[] m_rowHeights;

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
			CalcColumnWidthsAndRowHeights();

			Log.Layout("UpdateLayout()");

			float y=0;
			for (int rowIdx=0; rowIdx<m_actualRows; rowIdx++)
			{
				float x = 0;
				float maxY = 0;
				for (int columnIdx = 0; columnIdx<m_actualColumns; columnIdx++)
				{
					int elemIdx = GetElemIdx(columnIdx, rowIdx);

					float width = m_columnWidths[columnIdx];
					float height = m_rowHeights[rowIdx];

					if (elemIdx >= 0)
					{
						UiLayoutElement elem = s_layoutElements[elemIdx];
						RectTransform rt = elem.RectTransform;
					
						//float width = elem.Width;
						//float height = elem.Height;

						Vector2 size = new Vector2(width, height);
						rt.sizeDelta = size;
					
						Vector2 position = new Vector2(x, y);

						Log.Layout($"Setting Anchored Position for {elem.gameObject.name}: {position}");

						rt.anchoredPosition = position;
					}

					x += width;
					maxY = Mathf.Max(maxY, height);
				}

				y -= maxY;

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

		private void CalcColumnWidthsAndRowHeights()
		{
			Log.Layout("CalcColumnWidthsAndRowHeights()");

			m_columnWidths = new float[m_actualColumns];
			m_rowHeights = new float[m_actualRows];

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

					m_columnWidths[columnIdx] = Mathf.Max(m_columnWidths[columnIdx], width);
					m_rowHeights[rowIdx] = Mathf.Max(m_rowHeights[rowIdx], height);
				}
			}
		}

		private int GetElemIdx(int _columnIdx, int _rowIdx)
		{
			int result;

			int columnIdx = _columnIdx;
			int rowIdx = _rowIdx;

			if (m_rightToLeft)
				columnIdx = m_actualColumns - columnIdx - 1;
			if (m_bottomToTop)
				rowIdx = m_actualRows - rowIdx - 1;

			if (m_columnsFirst)
			{
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