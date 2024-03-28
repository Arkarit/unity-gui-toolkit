#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace GuiToolkit
{
	public static class UiUtility
	{
		private static readonly int FIRST_MOUSE_KEY = (int)(object)KeyCode.Mouse0;

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

		public static bool IsMouse(KeyCode _keyCode)
		{
			return _keyCode >= KeyCode.Mouse0 && _keyCode <= KeyCode.Mouse6;
		}

		public static EScreenOrientation GetCurrentScreenOrientation()
		{
			return ScreenWidth() >= ScreenHeight() ? EScreenOrientation.Landscape : EScreenOrientation.Portrait;
		}

		// Unity is so incredibly shitty... they don't even get returning the screen resolution right :-o~
		// https://forum.unity.com/threads/screen-width-is-wrong-in-editor-mode.94572/
		// http://muzykov.com/unity-get-game-view-resolution/
		public static int ScreenWidth()
		{
#if UNITY_EDITOR
			if (Application.isPlaying)
				return Screen.width;

			Vector2 v = Handles.GetMainGameViewSize();
			return (int) v.x;
#else
			return Screen.width;
#endif
		}

		public static int ScreenHeight()
		{
#if UNITY_EDITOR
			if (Application.isPlaying)
				return Screen.height;

			Vector2 v = Handles.GetMainGameViewSize();
			return (int) v.y;
#else
			return Screen.height;
#endif
		}
	}
}