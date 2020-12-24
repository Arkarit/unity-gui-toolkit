using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;
using System.Collections;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GuiToolkit
{
	/// \brief Helper component for horizontal sliders in vertical scroll rects and vice versa
	/// 
	/// Usually, horizontal sliders in vertical layouts (and vice versa) cause a lot of issues UX-wise.
	/// If you want to scroll the scroll rect up or down, but happen to tap on a slider, you move the slider instead, which is super annoying.
	/// UiSliderInScrollRect helps against this. It should reside on a Graphic covering the whole slider.
	/// It catches all necessary mouse/drag events, evaluates if the drag operation is horizontal or vertical, and forwards the
	/// events to the matching component - either a slider or a scroll rect.
	/// 
	/// Note that this component destroys its own game object if the Slider and ScrollRect member vars haven't been set.
	/// For the ScrollRect member, it tries to find a scroll rect in parent first.
	/// This makes it easy to make it a part of a slider prefab, since otherwise it would always block all mouse/drag actions regarding that slider.
	public class UiSliderInScrollRect : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler
	{
		private const float DRAG_DETECTION_TIME = 0.2f;

		public Slider m_slider;
		public ScrollRect m_scrollRect;

		private PointerEventData m_eventData;
		private ETriState m_isHorizontal;
		private IBeginDragHandler m_beginDragHandler;
		private IDragHandler m_dragHandler;
		private IEndDragHandler m_endDragHandler;
		private IPointerDownHandler m_pointerDownHandler;
		private bool m_handlersDetermined;
		private bool m_wasDragged;

		public void Start()
		{
			if (m_slider == null)
			{
				Destroy(gameObject);
				return;
			}

			if (m_scrollRect == null)
				m_scrollRect = GetComponentInParent<ScrollRect>();

			if (m_scrollRect == null)
				Destroy(gameObject);
		}

		public void OnBeginDrag( PointerEventData _eventData )
		{
			m_eventData = _eventData.Clone();
			m_handlersDetermined = false;
			m_wasDragged = true;
		}

		public void OnDrag( PointerEventData _eventData )
		{
			if (m_eventData != null)
				m_isHorizontal = EvalHorizontal(m_eventData, _eventData);

			if (m_isHorizontal == ETriState.Indeterminate)
				return;

			if (!m_handlersDetermined)
			{
				m_handlersDetermined = true;
				bool isDragHorizontal = m_isHorizontal == ETriState.True;
				bool isSliderHorizontal = m_slider.direction == Slider.Direction.LeftToRight || m_slider.direction == Slider.Direction.RightToLeft;
				bool isSliderUsed = isDragHorizontal == isSliderHorizontal;
				MonoBehaviour mb = isSliderUsed ? (MonoBehaviour) m_slider : (MonoBehaviour) m_scrollRect;
				SetHandlers(mb);
			}

			if (m_beginDragHandler != null)
			{
				m_beginDragHandler.OnBeginDrag(m_eventData);
				m_beginDragHandler = null;
			}

			m_eventData = null;

			if (m_dragHandler != null)
				m_dragHandler.OnDrag(_eventData);
		}

		public void OnEndDrag( PointerEventData _eventData )
		{
			if (m_endDragHandler != null)
				m_endDragHandler.OnEndDrag(_eventData);
		}

		public void OnPointerDown( PointerEventData _eventData )
		{
			StartCoroutine(DelayedOnPointerDown(_eventData.Clone()));
		}

		private void SetHandlers( MonoBehaviour _monoBehaviour )
		{
			m_beginDragHandler = _monoBehaviour is IBeginDragHandler ? (IBeginDragHandler)_monoBehaviour : null;
			m_dragHandler = _monoBehaviour is IDragHandler ? (IDragHandler)_monoBehaviour : null;
			m_endDragHandler = _monoBehaviour is IEndDragHandler ? (IEndDragHandler)_monoBehaviour : null;
			m_pointerDownHandler = _monoBehaviour is IPointerDownHandler ? (IPointerDownHandler)_monoBehaviour : null;
		}

		private static ETriState EvalHorizontal( PointerEventData _before, PointerEventData _after )
		{
			Vector2 delta = _after.position - _before.position;
			Vector2 deltaAbs = new Vector2(Mathf.Abs(delta.x), Mathf.Abs(delta.y));
			if (deltaAbs.magnitude < 1)
				return ETriState.Indeterminate;

			return deltaAbs.x > deltaAbs.y ? ETriState.True : ETriState.False;
		}

		private IEnumerator DelayedOnPointerDown(PointerEventData _eventData)
		{
			yield return new WaitForSecondsRealtime(DRAG_DETECTION_TIME);

			if (!m_wasDragged)
				m_slider.OnPointerDown(_eventData);
			m_wasDragged = false;
		}
	}

}