using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace GuiToolkit
{
	public class UiKeyPressRequester : UiViewModal
	{
		[SerializeField]
		private TMP_Text m_title;

		private UnityAction<KeyCode> m_onEvent;

		public void Requester( UnityAction<KeyCode> _onEvent, string _title = null )
		{
			m_onEvent = _onEvent;
			if (m_title != null && _title != null)
				m_title.text = _title;
			Show();
		}

		private void OnGUI()
		{
			Event e = Event.current;

			KeyCode k = KeyBindings.EventToKeyCode(e);
			if (k == KeyCode.None)
				return;

			m_onEvent?.Invoke(k);
			m_onEvent = null;
			Hide();
		}

	}
}