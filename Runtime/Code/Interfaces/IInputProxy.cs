using UnityEngine;

namespace GuiToolkit
{
	public interface IInputProxy
	{
		bool GetKey( KeyCode _keyCode );
		bool GetKeyDown( KeyCode _keyCode );
		bool GetKeyUp( KeyCode _keyCode );
	}
}