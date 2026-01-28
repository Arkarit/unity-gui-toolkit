using UnityEngine;

namespace GuiToolkit
{

	public sealed class UnityInputProxy : IInputProxy
	{
		public bool GetKey( KeyCode _keyCode ) => Input.GetKey(_keyCode);
		public bool GetKeyDown( KeyCode _keyCode ) => Input.GetKeyDown(_keyCode);
		public bool GetKeyUp( KeyCode _keyCode ) => Input.GetKeyUp(_keyCode);
	}
}