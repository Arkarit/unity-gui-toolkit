using System;
using System.Text;
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

		private string TitleMouseAndKeyboard => _("Press a Key\nOr Mouse button!");
		private string TitleMouse => _("Press a Mouse button!");
		private string TitleKeyboard => _("Press a Key!");
		private string TitleWhitelist => _("Press one of the keys\n{0}");
		private string TitleBlacklist => _("Press any key except\n{0}");
		
		protected virtual string GetTitle(string _title)
		{
			if (!string.IsNullOrEmpty(_title))
				return _title;
			
			if (m_playerSettingOptions == null)
				return TitleMouseAndKeyboard;
			
			var filterList = m_playerSettingOptions.KeyCodeFilterList;
			var isWhiteList = m_playerSettingOptions.KeyCodeFilterListIsWhitelist;
			
			if ( filterList != null)
			{
				if (filterList == PlayerSettingOptions.KeyCodeNoMouseList)
					return isWhiteList ? TitleMouse : TitleKeyboard;
				
				var formatStr = isWhiteList ? TitleWhitelist : TitleBlacklist;
				var sb = new StringBuilder();
				
				//FIXME Interpolated strings are not detected in LocaProcessor
				// https://github.com/Arkarit/unity-gui-toolkit/issues/6
				// When closed, these strings could be written inline
				var orStr = _("or");
				var andStr = _("and");
				
				for(int i=0; i<filterList.Count; i++)
				{
					sb.Append(_(filterList[i].ToString()));

					//FIXME Hope this doesn't make trouble in loca, which rules are sometimes pretty weird
					// Is there a language with different counting rules?
					if (i < filterList.Count - 2)
					{
						sb.Append(", ");
						continue;
					}
					if (i < filterList.Count - 1)
					{

						sb.Append( $" {(isWhiteList ? orStr : andStr)} ");
					}
				}
				
				return string.Format(formatStr, sb);
			}
			
			return TitleMouseAndKeyboard;
		}
		
		private void SetTitle(string _title)
		{
			if (m_title == null)
				return;
			
			m_title.text = GetTitle(_title);
		}
		
		public void Requester( UnityAction<KeyCode> _onEvent, PlayerSettingOptions _options, string _title )
		{
			m_playerSettingOptions = _options;
			SetTitle(_title);
			m_onEvent = _onEvent;
			ShowTopmost();
		}

		private void OnGUI()
		{
			Event e = Event.current;

			KeyCode k = UiUtility.EventToKeyCode(e, true);
			if (m_onEvent == null || k == KeyCode.None)
				return;

			if (m_playerSettingOptions != null 
			    && m_playerSettingOptions.KeyCodeFilterList.Contains(k)
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