using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit
{
	/// <summary>
	/// Complete replacement for the shitty Unity layout "system".
	/// </summary>
	/// 

	[ExecuteAlways]
	public class UiLayout : MonoBehaviour
	{
		private DrivenRectTransformTracker m_tracker;
		private bool m_dirty = true;
		private static readonly List<UiLayoutElement> s_layoutElements = new List<UiLayoutElement>();
		private const DrivenTransformProperties DRIVEN_TRANSFORM_PROPERTIES =
			  DrivenTransformProperties.AnchoredPosition
			| DrivenTransformProperties.SizeDelta;

		[SerializeField]
		int m_numColumns = -1;
		[SerializeField]
		int m_numRows = -1;

		private int m_actualColumns;
		private int m_actualRows;

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
			GetComponentsInChildren(false, s_layoutElements);

			SetActualColumnsAndRows();

			float y=0;
			for (int yIdx=0; yIdx<m_actualRows; yIdx++)
			{
				float x = 0;
				float maxY = 0;
				for (int xIdx = 0; xIdx<m_actualColumns; xIdx++)
				{
					int elemIdx = xIdx +  yIdx * m_actualColumns;
					if (elemIdx >= s_layoutElements.Count)
						goto LoopExit;

					UiLayoutElement elem = s_layoutElements[elemIdx];
					RectTransform rt = elem.RectTransform;
					
					float width = elem.GetWidth();
					float height = elem.GetHeight();

					Vector2 size = new Vector2(width, height);
					rt.sizeDelta = size;
					
					Vector2 position = new Vector2(x, y);
					rt.anchoredPosition = position;

					x += width;
					maxY = Mathf.Max(maxY, height);
				}

				y += maxY;

			}
			LoopExit:

			m_tracker.Clear();
			foreach (var child in s_layoutElements)
				m_tracker.Add(this, child.RectTransform, DRIVEN_TRANSFORM_PROPERTIES);

			// m_dirty = false;
		}

		private void SetActualColumnsAndRows()
		{
			if (m_numColumns < 0)
			{
				m_actualColumns = s_layoutElements.Count;
				m_actualRows = 1;
				return;
			}

			m_actualColumns = m_numColumns;

			if (m_numRows < 0)
			{
				m_actualRows = 1 + s_layoutElements.Count / m_actualColumns;
				return;
			}

			m_actualRows = m_numRows;
		}
	}
}