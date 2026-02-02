using UnityEngine;

namespace GuiToolkit
{
	public interface IInputProxy
	{
		Vector3 MousePosition { get; }

		bool GetKey( KeyCode _keyCode );
		bool GetKeyDown( KeyCode _keyCode );
		bool GetKeyUp( KeyCode _keyCode );
	}
}