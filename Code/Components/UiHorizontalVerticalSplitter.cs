using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GuiToolkit
{
	public class UiHorizontalVerticalSplitter : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler
	{
		public List<Slider> m_passThrough;

		public void OnBeginDrag( PointerEventData _eventData )
		{
			Debug.Log("OnBeginDrag");
			foreach (var mb in m_passThrough)
			{
				if (!(mb is IBeginDragHandler))
					continue;

				IBeginDragHandler handler = (IBeginDragHandler) mb;
				if (handler != null)
					handler.OnBeginDrag(_eventData);
			}
		}

		public void OnDrag( PointerEventData _eventData )
		{
			Debug.Log("OnDrag");
			foreach (var mb in m_passThrough)
			{
				if (!(mb is IDragHandler))
					continue;

				IDragHandler handler = (IDragHandler) mb;
				if (handler != null)
					handler.OnDrag(_eventData);
			}
		}

		public void OnEndDrag( PointerEventData _eventData )
		{
			Debug.Log("OnEndDrag");
			foreach (var mb in m_passThrough)
			{
				if (!(mb is IEndDragHandler))
					continue;

				IEndDragHandler handler = (IEndDragHandler) mb;
				if (handler != null)
					handler.OnEndDrag(_eventData);
			}
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

	}

}