using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace GuiToolkit
{
	public class UiThing : MonoBehaviour, IEventSystemHandler
	{
		protected virtual bool ReceiveEventsWhenDisabled => true;

		protected virtual void Awake()
		{
			if (ReceiveEventsWhenDisabled)
				AddEventListeners();
		}

		protected virtual void OnDestroy()
		{
			if (ReceiveEventsWhenDisabled)
				RemoveEventListeners();
		}

		protected virtual void OnEnable()
		{
			if (!ReceiveEventsWhenDisabled)
				AddEventListeners();
		}

		protected virtual void OnDisable()
		{
			if (!ReceiveEventsWhenDisabled)
				RemoveEventListeners();
		}

		// Reserved
		protected virtual void Update() { }

		// Custom virtuals
		protected virtual void AddEventListeners() { }
		protected virtual void RemoveEventListeners() { }

	}
}
