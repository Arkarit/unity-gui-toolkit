using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace GuiToolkit
{
	public class UiKeyBindingsEntry : MonoBehaviour
	{
		[SerializeField]
		protected TMP_Text m_keyName;

		[SerializeField]
		protected TMP_Text m_keyCode;

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