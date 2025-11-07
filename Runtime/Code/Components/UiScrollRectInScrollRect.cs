using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GuiToolkit
{
	/// \brief Routes drag events between an inner ScrollRect and an outer (parent) ScrollRect
	/// based on drag orientation. If the drag orientation matches the inner's main axis,
	/// the inner ScrollRect receives the events; otherwise the parent ScrollRect does.
	public class UiScrollRectInScrollRect : UiAbstractDragRouter
	{
		[Tooltip("The inner ScrollRect (primary). Required.")]
		[SerializeField][Mandatory] private ScrollRect m_innerScrollRect;

		[Tooltip("The outer ScrollRect (secondary). Usually a parent. Will be auto-resolved if null.")]
		[SerializeField][Optional] private ScrollRect m_outerScrollRect;

		protected override bool TryResolveDependencies()
		{
			if (m_innerScrollRect == null)
			{
				return false;
			}

			if (m_outerScrollRect == null)
			{
				ScrollRect[] parents = GetComponentsInParent<ScrollRect>(true);

				for (int i = 0; i < parents.Length; i++)
				{
					if (parents[i] != m_innerScrollRect)
					{
						m_outerScrollRect = parents[i];
						break;
					}
				}
			}

			return m_outerScrollRect != null;
		}

		protected override MonoBehaviour GetPrimaryTarget()
		{
			return m_innerScrollRect;
		}

		protected override MonoBehaviour GetSecondaryTarget()
		{
			return m_outerScrollRect;
		}

		protected override bool IsPrimaryHorizontal()
		{
			bool h = m_innerScrollRect != null && m_innerScrollRect.horizontal;
			bool v = m_innerScrollRect != null && m_innerScrollRect.vertical;

			if (h && !v)
			{
				return true;
			}

			if (!h && v)
			{
				return false;
			}

			// Ambiguous or both/none enabled: default to vertical as secondary bias.
			return false;
		}

		protected override void OnPrimaryPointerDown(PointerEventData _eventData)
		{
			m_innerScrollRect.enabled = false;
		}

		protected override void OnPrimaryPointerUp(PointerEventData _eventData)
		{
			m_innerScrollRect.enabled = true;
		}
	}
}
