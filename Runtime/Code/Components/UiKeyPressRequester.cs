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
		
		[SerializeField]
		private UiSimpleAnimationBase m_keyPressNotAllowedAnimation;

		private UnityAction<KeyCode> m_onEvent;
		private PlayerSettingOptions m_playerSettingOptions;
		
		private bool m_isClosable = true;

		protected override void Awake()
		{
			base.Awake();
			m_pointerDownUpHelper.OnPointerUpAction = OnPointerUp;
		}

		private void OnPointerUp()
		{
			if (m_isClosable)
				Hide();
			
			m_isClosable = true;
		}

		public void Requester( UnityAction<KeyCode> _onEvent, PlayerSettingOptions _options, string _title )
		{
			m_playerSettingOptions = _options;
			m_onEvent = _onEvent;
			
			//TODO title by Player settings
			if (m_title != null && _title != null)
				m_title.text = _title;
			
			ShowTopmost();
		}

		private void OnGUI()
		{
			Event e = Event.current;

			KeyCode k = UiUtility.EventToKeyCode(e, true);
			if (m_onEvent == null || k == KeyCode.None)
				return;

			if (m_playerSettingOptions != null 
			    && m_playerSettingOptions.KeyCodeBlacklist.Contains(k)
			    && k != KeyCode.Escape)
			{
				//TODO GUI Message "Not supported for key"
				Debug.Log($"Key '{_(k.ToString())}' not supported");
				if (m_keyPressNotAllowedAnimation)
					m_keyPressNotAllowedAnimation.Play();
				
				m_isClosable = false;
				return;
			}
			
			if (m_keyPressNotAllowedAnimation)
				m_keyPressNotAllowedAnimation.Reset();
		
			if (k != KeyCode.Escape)
				m_onEvent.Invoke(k);

			m_onEvent = null;
			if (!UiUtility.IsMouse(k))
				Hide();
		}

	}
}