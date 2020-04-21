using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GuiToolkit
{
	[Serializable]
	public class KeyBindings : System.Collections.Generic.IEnumerable<KeyValuePair<string,KeyCode>>
	{
		private Dictionary<string, KeyCode> m_keyCodeByString = new Dictionary<string, KeyCode>();
		private Dictionary<KeyCode, string> m_stringByKeyCode = new Dictionary<KeyCode,string>();

		private Dictionary<string, KeyCode> m_savedKeyCodeByString;
		private Dictionary<KeyCode, string> m_savedStringByKeyCode;

		public delegate bool ShouldKeyCodeBeRemoved(string _keyName, KeyCode _newKeyCode);

#if UNITY_EDITOR
		private bool m_initialized;
#endif
		public void Initialize(List<KeyValuePair<string,KeyCode>> _bindings)
		{
#if UNITY_EDITOR
			if (m_initialized)
			{
				Debug.LogWarning("KeyBindings are initialized multiple times. That does no harm, but is it intentional?");
			}
#endif
			m_keyCodeByString.Clear();
			m_stringByKeyCode.Clear();

			foreach(var kv in _bindings)
			{
				int keyCodeInt = (int)(object)kv.Value;
				int userBindingInt = PlayerPrefs.GetInt(kv.Key, keyCodeInt);
				Debug.Assert(!m_keyCodeByString.ContainsKey(kv.Key), "Double key code entry not allowed");
				KeyCode userBinding = (KeyCode)(object) userBindingInt;
				m_keyCodeByString.Add(kv.Key, userBinding);
				if (kv.Value != KeyCode.None)
					m_stringByKeyCode.Add(userBinding, kv.Key);
			}
#if UNITY_EDITOR
			m_initialized = true;
#endif
		}

		public KeyCode GetKeyCode(string _key)
		{
			CheckInitialized();
			KeyCode result;
			if (!m_keyCodeByString.TryGetValue(_key, out result))
				return KeyCode.None;
			return result;
		}

		public string GetKeyName(KeyCode _keyCode)
		{
			CheckInitialized();

			if (_keyCode == KeyCode.None)
			{
				Debug.LogWarning("You can not ask KeyBindings for the name of KeyCode.None, since it can be assigned to multiple key names.");
				return null;
			}

			string result;
			if (!m_stringByKeyCode.TryGetValue(_keyCode, out result))
				return null;
			return result;
		}

		public string GetKeyName(Event _event)
		{
			return GetKeyName(EventToKeyCode(_event));
		}

		public KeyCode this[string _key]
		{
			get => GetKeyCode(_key);
		}

		public string this[KeyCode _keyCode]
		{
			get => GetKeyName(_keyCode);
		}

		public string this[Event _event]
		{
			get => GetKeyName(_event);
		}

		public void BeginChangeBindings()
		{
			m_savedKeyCodeByString = new Dictionary<string, KeyCode>(m_keyCodeByString);
			m_savedStringByKeyCode = new Dictionary<KeyCode, string>(m_stringByKeyCode);
		}

		public void EndChangeBindings( bool _commit )
		{
			if (!_commit)
			{
				m_keyCodeByString = m_savedKeyCodeByString;
				m_stringByKeyCode = m_savedStringByKeyCode;
				m_savedKeyCodeByString = null;
				m_savedStringByKeyCode = null;
				return;
			}

			m_savedKeyCodeByString = null;
			m_savedStringByKeyCode = null;

			foreach( var kv in m_keyCodeByString )
			{
				PlayerPrefs.SetInt(kv.Key, (int)(object)kv.Value );
			}
		}

		public bool ChangeBinding(string _keyName, KeyCode _newKeyCode, ShouldKeyCodeBeRemoved _shouldKeyCodeBeRemoved )
		{
			CheckInitialized();

			// check for no change
			KeyCode oldKeyCode = GetKeyCode(_keyName);
			if (oldKeyCode == _newKeyCode)
				return true;

			// First, check if the key code was used for something else
			if (_newKeyCode != KeyCode.None)
			{
				string abandonedKeyName = GetKeyName(_newKeyCode);
				if (!string.IsNullOrEmpty(abandonedKeyName))
				{
					if (!_shouldKeyCodeBeRemoved(abandonedKeyName, _newKeyCode))
						return false;
					m_keyCodeByString[abandonedKeyName] = KeyCode.None;
					m_stringByKeyCode.Remove(_newKeyCode);
				}
			}

			if (oldKeyCode != KeyCode.None)
				m_stringByKeyCode.Remove(oldKeyCode);
			m_keyCodeByString[_keyName] = _newKeyCode;
			if (_newKeyCode != KeyCode.None)
				m_stringByKeyCode.Add(_newKeyCode, _keyName);

			return true;
		}

		public bool ChangeBinding(string _keyName, KeyCode _newKeyCode )
		{
			return ChangeBinding(_keyName, _newKeyCode, (string _, KeyCode __) => { return true; } );
		}

		// Dear Unity, you have KeyCodes for everything.
		// Why please don't you simply give it to me in an event?
		public static KeyCode EventToKeyCode(Event _event)
		{
			if (_event == null)
				return KeyCode.None;

			if (_event.isKey)
				return _event.keyCode;

			if (_event.isMouse)
			{
				int mouseButtonCode = ((int)(object) KeyCode.Mouse0 + _event.button);
				return (KeyCode)(object) mouseButtonCode;
			}

			return KeyCode.None;
		}


		public IEnumerator<KeyValuePair<string, KeyCode>> GetEnumerator()
		{
			foreach (var kv in m_keyCodeByString)
				yield return kv;
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			foreach (var kv in m_keyCodeByString)
				yield return kv;
		}

		private void CheckInitialized()
		{
#if UNITY_EDITOR
			if (!m_initialized)
			{
				Debug.LogError("Attempt to access key bindings before calling Initialize()");
			}
#endif
		}
	}
}