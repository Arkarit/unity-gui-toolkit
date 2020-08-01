using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace GuiToolkit
{
	public class UiKeyBindingsEntry : UiThing
	{
		[SerializeField]
		protected TMP_Text m_txtKeyName;

		[SerializeField]
		protected TMP_Text m_txtKeyCode;

		[SerializeField]
		protected UiButton m_button;

		protected override void OnEnable()
		{
			base.OnEnable();
			m_button.OnClick.AddListener( OnClick );
		}

		protected override void OnDisable()
		{
			m_button.OnClick.RemoveListener( OnClick );
			base.OnDisable();
		}

		private void OnClick()
		{
			UiMain.Instance.KeyPressRequester(OnKeyPressed);
		}

		private void OnKeyPressed( KeyCode _keyCode )
		{
			if (_keyCode != KeyCode.None)
			{
				m_txtKeyCode.text = _keyCode.ToString();
				UiMain.Instance.KeyBindings.ChangeBinding(m_txtKeyName.text, _keyCode);
				UiKeyBindingsList.EvRefreshList.Invoke();
			}
		}

		public void Initialize( string _keyName, KeyCode _keyCode )
		{
			m_txtKeyName.text = _keyName;
			m_txtKeyCode.text = _keyCode.ToString();
			m_txtKeyCode.alpha = _keyCode == KeyCode.None ? 0.4f : 1.0f;
		}

		public void Initialize( KeyValuePair<string, KeyCode> _kv)
		{
			Initialize(_kv.Key, _kv.Value);
		}
	}
}