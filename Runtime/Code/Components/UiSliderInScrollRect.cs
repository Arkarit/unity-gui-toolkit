using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GuiToolkit
{
	/// \brief Helper component for horizontal sliders in vertical scroll rects and vice versa.
	/// 
	/// Split version based on UiDragRouterBase.
	public class UiSliderInScrollRect : UiAbstractDragRouter
	{
		[SerializeField][Mandatory] private Slider m_slider;
		[SerializeField][Mandatory] private ScrollRect m_scrollRect;

		protected override bool TryResolveDependencies()
		{
			if (m_slider == null)
				return false;

			if (m_scrollRect == null)
				m_scrollRect = GetComponentInParent<ScrollRect>();

			return m_scrollRect != null;
		}

		protected override MonoBehaviour GetPrimaryTarget()
		{
			return m_slider;
		}

		protected override MonoBehaviour GetSecondaryTarget()
		{
			return m_scrollRect;
		}

		protected override bool IsPrimaryHorizontal()
		{
			var dir = m_slider.direction;

			if (dir == Slider.Direction.LeftToRight)
				return true;

			if (dir == Slider.Direction.RightToLeft)
				return true;

			return false;
		}

		protected override void OnPrimaryPointerDown(PointerEventData _eventData)
		{
			m_slider.OnPointerDown(_eventData);
		}
	}
}
