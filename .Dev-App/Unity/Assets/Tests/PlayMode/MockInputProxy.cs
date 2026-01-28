using System.Collections.Generic;
using UnityEngine;


namespace GuiToolkit.Tests
{
	public sealed class MockInputProxy : IInputProxy
	{
		private readonly HashSet<KeyCode> m_keys = new();

		public void Press( KeyCode _key ) => m_keys.Add(_key);
		public void Release( KeyCode _key ) => m_keys.Remove(_key);

		public bool GetKey( KeyCode _keyCode ) => m_keys.Contains(_keyCode);
		public bool GetKeyDown( KeyCode _keyCode ) => m_keys.Contains(_keyCode);
		public bool GetKeyUp( KeyCode _keyCode ) => !m_keys.Contains(_keyCode);
	}
}