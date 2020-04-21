using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace GuiToolkit
{
	public class UiKeyBindingsEntry : UiThing
	{
		[SerializeField]
		protected TMP_Text m_keyName;

		[SerializeField]
		protected TMP_Text m_keyCode;

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
				m_keyCode.text = _keyCode.ToString();
		}

		public void Initialize( string _keyName, KeyCode _keyCode)
		{
			m_keyName.text = _keyName;
			m_keyCode.text = _keyCode.ToString();
		}

		public void Initialize( KeyValuePair<string, KeyCode> _kv)
		{
			Initialize(_kv.Key, _kv.Value);
		}
	}
}