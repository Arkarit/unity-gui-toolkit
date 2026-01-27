using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;

namespace GuiToolkit
{
	public class UiKeyPressRequester : UiView
	{
		[FormerlySerializedAs("m_title")]
		[SerializeField]
		private TMP_Text m_textFieldTitle;

		[SerializeField]
		private TMP_Text m_textFieldNotSupportedWarning;

		[SerializeField]
		private UiPointerDownUpHelper m_pointerDownUpHelper;

		[SerializeField]
		private UiSimpleAnimationBase m_wiggleAnimation;

		[SerializeField]
		private UiSimpleAnimationBase m_warningAnimation;

		public virtual string TitleMouseAndKeyboard => _("Press a Key\nOr Mouse button!");
		public virtual string TitleMouse => _("Press a Mouse button!");
		public virtual string TitleKeyboard => _("Press a Key!");
		public virtual string TitleWhitelist => _("Press one of the keys\n{0}");
		public virtual string TitleBlacklist => _("Press any key except\n{0}");
		public virtual string TextNotSupported => _("Key '{0}' not supported");

		private UnityAction<KeyBinding> m_onEvent;
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

		protected virtual string GetTitle( string _title )
		{
			if (!string.IsNullOrEmpty(_title))
				return _title;

			if (m_playerSettingOptions == null)
				return TitleMouseAndKeyboard;

			var filterList = m_playerSettingOptions.KeyCodeFilterList;
			var isWhiteList = m_playerSettingOptions.KeyCodeFilterListIsWhitelist;

			if (filterList != null)
			{
				if (filterList == PlayerSettingOptions.KeyCodeNoMouseList)
					return isWhiteList ? TitleMouse : TitleKeyboard;

				var formatStr = isWhiteList ? TitleWhitelist : TitleBlacklist;
				var sb = new StringBuilder();

				var orStr = _("or");
				var andStr = _("and");

				for (int i = 0; i < filterList.Count; i++)
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
						sb.Append($" {(isWhiteList ? orStr : andStr)} ");
					}
				}

				return string.Format(formatStr, sb);
			}

			return TitleMouseAndKeyboard;
		}

		private void SetTitle( string _title )
		{
			if (m_textFieldTitle == null)
				return;

			m_textFieldTitle.text = GetTitle(_title);
		}

		public void Requester( UnityAction<KeyBinding> _onEvent, PlayerSettingOptions _options, string _title )
		{
			m_playerSettingOptions = _options;
			SetTitle(_title);
			m_onEvent = _onEvent;
			ShowTopmost();
		}

		private void OnGUI()
		{
			Event e = Event.current;

			KeyCode keyCode = UiUtility.EventToKeyCode(e, true);
			if (m_onEvent == null || keyCode == KeyCode.None || GeneralUtility.IsModifierKey(keyCode))
				return;

			KeyBinding.EModifiers modifiers = GetCurrentModifiers();
			KeyBinding keyBinding = new KeyBinding(keyCode, modifiers);

			if (m_playerSettingOptions != null && m_playerSettingOptions.KeyPolicy == EKeyPolicy.SingleKey)
				keyBinding = new KeyBinding(keyCode);

			bool isSuppressed = IsSuppressed(keyBinding);
			if (isSuppressed)
			{
				UiLog.LogInternal($"Key '{FormatKeyBindingForLog(keyBinding)}' not supported");

				if (m_wiggleAnimation)
					m_wiggleAnimation.Play();

				if (m_textFieldNotSupportedWarning)
					m_textFieldNotSupportedWarning.text =
						string.Format(TextNotSupported, FormatKeyBindingForUi(keyBinding));

				if (m_warningAnimation)
					m_warningAnimation.Play();

				m_isClosable = false;
				return;
			}

			if (m_wiggleAnimation)
				m_wiggleAnimation.Reset();

			if (keyCode != KeyCode.Escape)
				m_onEvent.Invoke(keyBinding);

			m_onEvent = null;

			if (!UiUtility.IsMouse(keyCode))
				Hide();
		}

		private bool IsSuppressed( KeyBinding _keyBinding )
		{
			if (m_playerSettingOptions == null)
				return false;

			if (_keyBinding.KeyCode == KeyCode.Escape)
				return false;

			List<KeyCode> filterList = m_playerSettingOptions.KeyCodeFilterList;
			if (filterList == null)
				return false;

			bool isWhiteList = m_playerSettingOptions.KeyCodeFilterListIsWhitelist;

			bool containsKeyCode = filterList.Contains(_keyBinding.KeyCode);
			return isWhiteList ? !containsKeyCode : containsKeyCode;
		}

		private static KeyBinding.EModifiers GetCurrentModifiers()
		{
			KeyBinding.EModifiers mods = KeyBinding.EModifiers.None;

			if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
				mods |= KeyBinding.EModifiers.Shift;

			if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
				mods |= KeyBinding.EModifiers.Ctrl;

			if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))
				mods |= KeyBinding.EModifiers.Alt;

			return mods;
		}

		private string FormatKeyBindingForUi( KeyBinding _keyBinding )
		{
			if (_keyBinding.KeyCode == KeyCode.None)
				return _("None");

			var sb = new StringBuilder();

			if ((_keyBinding.Modifiers & KeyBinding.EModifiers.Ctrl) != 0)
				sb.Append(_("Ctrl")).Append("+");

			if ((_keyBinding.Modifiers & KeyBinding.EModifiers.Shift) != 0)
				sb.Append(_("Shift")).Append("+");

			if ((_keyBinding.Modifiers & KeyBinding.EModifiers.Alt) != 0)
				sb.Append(_("Alt")).Append("+");

			sb.Append(_(_keyBinding.KeyCode.ToString()));
			return sb.ToString();
		}

		private string FormatKeyBindingForLog( KeyBinding _keyBinding )
		{
			// Keep it simple: no localization, no allocations beyond ToString use.
			if (_keyBinding.Modifiers == KeyBinding.EModifiers.None)
				return _keyBinding.KeyCode.ToString();

			return $"{_keyBinding.Modifiers}+{_keyBinding.KeyCode}";
		}
	}
}
