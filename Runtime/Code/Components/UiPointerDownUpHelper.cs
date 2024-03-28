using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace GuiToolkit
{
	/// <summary>
	/// A helper for components, which reside on a bottommost canvas game object
	/// and still have to listen for pointer down/up on a higher level
	/// </summary>
	public class UiPointerDownUpHelper : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
	{
		public UnityAction OnPointerDownAction;
		public UnityAction OnPointerUpAction;

		public void OnPointerDown( PointerEventData eventData )
		{
			OnPointerDownAction?.Invoke();
		}

		public void OnPointerUp( PointerEventData eventData )
		{
			OnPointerUpAction?.Invoke();
		}
	}
}