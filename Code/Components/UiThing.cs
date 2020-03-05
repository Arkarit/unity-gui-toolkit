using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace GuiToolkit
{
	public class UiThing : MonoBehaviour, IEventSystemHandler
	{
		protected virtual void Awake() { }
		protected virtual void Update() { }
		protected virtual void OnEnable() { }
		protected virtual void OnDisable() { }
	}
}
