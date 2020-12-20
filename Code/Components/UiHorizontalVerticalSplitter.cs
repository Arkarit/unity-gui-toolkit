using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GuiToolkit
{
	public class UiHorizontalVerticalSplitter : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler
	{
		public Slider m_slider;
		public ScrollRect m_scrollRect;

		private PointerEventData m_eventData;
		private ETriState m_isHorizontal;
		private IBeginDragHandler m_beginDragHandler;
		private IDragHandler m_dragHandler;
		private IEndDragHandler m_endDragHandler;
		private bool m_handlersDetermined;

		public void OnBeginDrag( PointerEventData _eventData )
		{
			Debug.Log("OnBeginDrag");
			m_eventData = _eventData.Clone();
			m_handlersDetermined = false;
		}

		public void OnDrag( PointerEventData _eventData )
		{
			InitHorizontal(_eventData);
			if (m_isHorizontal == ETriState.Indeterminate)
				return;

			if (!m_handlersDetermined)
			{
				m_handlersDetermined = true;
				MonoBehaviour mb = (m_isHorizontal == ETriState.True) ? (MonoBehaviour) m_slider : (MonoBehaviour) m_scrollRect;

				if (mb is IBeginDragHandler)
					m_beginDragHandler = (IBeginDragHandler) mb;

				if (mb is IDragHandler)
					m_dragHandler = (IDragHandler) mb;

				if (mb is IEndDragHandler)
					m_endDragHandler = (IEndDragHandler) mb;
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
		}

		public void OnPointerUp( PointerEventData _eventData )
		{
			Debug.Log("OnPointerUp");
		}

		private void InitHorizontal( PointerEventData _eventData )
		{
			if (m_eventData != null)
			{
				m_isHorizontal = EvalHorizontal(m_eventData, _eventData);

				if (m_isHorizontal == ETriState.True && m_slider is IBeginDragHandler)
					((IBeginDragHandler)m_slider).OnBeginDrag(m_eventData);
			}
		}

		private static ETriState EvalHorizontal( PointerEventData _before, PointerEventData _after )
		{
			Vector2 delta = _after.position - _before.position;
			Vector2 deltaAbs = new Vector2(Mathf.Abs(delta.x), Mathf.Abs(delta.y));
Debug.Log($"_before.position:{_before.position} _after.position:{_after.position} deltaAbs:{deltaAbs}");
			if (deltaAbs.magnitude < 1)
				return ETriState.Indeterminate;

			Debug.Log($"delta: {delta}");
			return deltaAbs.x > deltaAbs.y ? ETriState.True : ETriState.False;
		}


	}

}