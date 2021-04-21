using System;
using System.Collections;
using System.Collections.Generic;
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
#if UNITY_IOS || UNITY_ANDROID
				return Screen.width >= Screen.height ? EScreenOrientation.MobileLandscape : EScreenOrientation.MobilePortrait;
#else
				return Screen.width >= Screen.height ? EScreenOrientation.PcLandscape : EScreenOrientation.PcPortrait;
#endif
		}
	}
}