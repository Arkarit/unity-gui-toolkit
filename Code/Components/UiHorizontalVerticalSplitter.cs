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
	public class UiHorizontalVerticalSplitter : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerDownHandler //, IPointerClickHandler //, IPointerUpHandler
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

		public void OnBeginDrag( PointerEventData _eventData )
		{
			Debug.Log("OnBeginDrag");
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
				MonoBehaviour mb = (m_isHorizontal == ETriState.True) ? (MonoBehaviour) m_slider : (MonoBehaviour) m_scrollRect;
				SetHandlers(mb);
			}

			Debug.Log("OnDrag");

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
			Debug.Log("OnEndDrag");
			if (m_endDragHandler != null)
				m_endDragHandler.OnEndDrag(_eventData);
		}

		public void OnPointerClick( PointerEventData _eventData )
		{
			Debug.Log("OnPointerClick");
		}

		public void OnPointerDown( PointerEventData _eventData )
		{
			Debug.Log("OnPointerDown");
			StartCoroutine(DelayedOnPointerDown(_eventData.Clone()));
		}

		public void OnPointerUp( PointerEventData _eventData )
		{
			Debug.Log("OnPointerUp");
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

			Debug.Log($"delta: {delta}");
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