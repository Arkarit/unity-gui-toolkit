using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace GuiToolkit
{
	public class UiKeyPressRequester : UiView
	{
		[SerializeField]
		private TMP_Text m_title;

		[SerializeField]
		private UiPointerDownUpHelper m_pointerDownUpHelper;

		private UnityAction<KeyCode> m_onEvent;

		protected override void Awake()
		{
			base.Awake();
			m_pointerDownUpHelper.OnPointerUpAction = OnPointerUp;
		}

		private void OnPointerUp()
		{
			Hide();
		}

		public void Requester( UnityAction<KeyCode> _onEvent, string _title = null )
		{
			m_onEvent = _onEvent;
			if (m_title != null && _title != null)
				m_title.text = _title;
			ShowTopmost();
		}

		private void OnGUI()
		{
			Event e = Event.current;

			KeyCode k = KeyBindings.EventToKeyCode(e);
			if (m_onEvent == null || k == KeyCode.None)
				return;

			if (k != KeyCode.Escape)
				m_onEvent.Invoke(k);

			m_onEvent = null;
			if (!KeyBindings.IsMouse(k))
				Hide();
		}

	}
}