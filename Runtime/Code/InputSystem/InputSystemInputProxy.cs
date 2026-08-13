using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace GuiToolkit
{
	/// <summary>
	/// <see cref="IInputProxy"/> backed by the Input System package.
	/// </summary>
	/// <remarks>
	/// Lives in its own assembly so the toolkit keeps compiling in projects without
	/// com.unity.inputsystem: the assembly definition constrains itself to a define that only exists
	/// when that package is present, so it is simply left out otherwise.
	///
	/// <see cref="KeyCode"/> is the toolkit's currency — PlayerSettings key bindings are stored as
	/// KeyCode — so the mapping onto the new <see cref="Key"/> enum lives here. Contiguous ranges are
	/// computed, the rest is a table. An unmapped KeyCode resolves to null and reads as "not pressed",
	/// never as an exception, which is the whole point of this class existing.
	/// </remarks>
	public sealed class InputSystemInputProxy : IInputProxy
	{
		private static readonly Dictionary<KeyCode, Key> s_keyByKeyCode = new()
		{
			{ KeyCode.Space, Key.Space },
			{ KeyCode.Return, Key.Enter },
			{ KeyCode.KeypadEnter, Key.NumpadEnter },
			{ KeyCode.Tab, Key.Tab },
			{ KeyCode.Escape, Key.Escape },
			{ KeyCode.Backspace, Key.Backspace },
			{ KeyCode.Delete, Key.Delete },
			{ KeyCode.Insert, Key.Insert },
			{ KeyCode.Home, Key.Home },
			{ KeyCode.End, Key.End },
			{ KeyCode.PageUp, Key.PageUp },
			{ KeyCode.PageDown, Key.PageDown },
			{ KeyCode.UpArrow, Key.UpArrow },
			{ KeyCode.DownArrow, Key.DownArrow },
			{ KeyCode.LeftArrow, Key.LeftArrow },
			{ KeyCode.RightArrow, Key.RightArrow },
			{ KeyCode.LeftShift, Key.LeftShift },
			{ KeyCode.RightShift, Key.RightShift },
			{ KeyCode.LeftControl, Key.LeftCtrl },
			{ KeyCode.RightControl, Key.RightCtrl },
			{ KeyCode.LeftAlt, Key.LeftAlt },
			{ KeyCode.RightAlt, Key.RightAlt },
			{ KeyCode.LeftCommand, Key.LeftMeta },
			{ KeyCode.RightCommand, Key.RightMeta },
			{ KeyCode.LeftWindows, Key.LeftWindows },
			{ KeyCode.RightWindows, Key.RightWindows },
			{ KeyCode.CapsLock, Key.CapsLock },
			{ KeyCode.Numlock, Key.NumLock },
			{ KeyCode.ScrollLock, Key.ScrollLock },
			{ KeyCode.Print, Key.PrintScreen },
			{ KeyCode.Pause, Key.Pause },
			{ KeyCode.Menu, Key.ContextMenu },
			{ KeyCode.BackQuote, Key.Backquote },
			{ KeyCode.Quote, Key.Quote },
			{ KeyCode.Semicolon, Key.Semicolon },
			{ KeyCode.Comma, Key.Comma },
			{ KeyCode.Period, Key.Period },
			{ KeyCode.Slash, Key.Slash },
			{ KeyCode.Backslash, Key.Backslash },
			{ KeyCode.LeftBracket, Key.LeftBracket },
			{ KeyCode.RightBracket, Key.RightBracket },
			{ KeyCode.Minus, Key.Minus },
			{ KeyCode.Equals, Key.Equals },
			{ KeyCode.KeypadDivide, Key.NumpadDivide },
			{ KeyCode.KeypadMultiply, Key.NumpadMultiply },
			{ KeyCode.KeypadPlus, Key.NumpadPlus },
			{ KeyCode.KeypadMinus, Key.NumpadMinus },
			{ KeyCode.KeypadPeriod, Key.NumpadPeriod },
			{ KeyCode.KeypadEquals, Key.NumpadEquals },
		};

		/// <summary>
		/// Installs this proxy as the toolkit's default.
		/// </summary>
		/// <remarks>
		/// SubsystemRegistration is the earliest runtime hook there is, so this lands before any scene
		/// loads and therefore before anything can ask for a proxy. Guarded by ENABLE_INPUT_SYSTEM
		/// rather than by the assembly's own constraint: the package can be installed while Active Input
		/// Handling is still "Input Manager (Old)", and then the legacy proxy is the correct answer.
		/// With "Both", either works and the new backend is preferred.
		///
		/// InitializeOnLoadMethod as well, because RuntimeInitializeOnLoadMethod does not fire in Edit
		/// Mode: without it an editor-side read falls through to <see cref="NullInputProxy"/> and reports
		/// a problem that does not exist. Measured, not assumed.
		/// </remarks>
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
#if UNITY_EDITOR
		[UnityEditor.InitializeOnLoadMethod]
#endif
		private static void Register()
		{
#if ENABLE_INPUT_SYSTEM
			InputProxyFactory.Register(() => new InputSystemInputProxy());
#endif
		}

		public Vector3 MousePosition
		{
			get
			{
				var mouse = Mouse.current;
				return mouse != null ? (Vector3)mouse.position.ReadValue() : Vector3.zero;
			}
		}

		public bool GetKey( KeyCode _keyCode )
		{
			var button = Resolve(_keyCode);
			return button != null && button.isPressed;
		}

		public bool GetKeyDown( KeyCode _keyCode )
		{
			var button = Resolve(_keyCode);
			return button != null && button.wasPressedThisFrame;
		}

		public bool GetKeyUp( KeyCode _keyCode )
		{
			var button = Resolve(_keyCode);
			return button != null && button.wasReleasedThisFrame;
		}

		/// <summary>Mouse buttons and keys are both ButtonControls, so one lookup serves all three reads.</summary>
		private static ButtonControl Resolve( KeyCode _keyCode )
		{
			var mouse = Mouse.current;
			if (mouse != null)
			{
				switch (_keyCode)
				{
					case KeyCode.Mouse0: return mouse.leftButton;
					case KeyCode.Mouse1: return mouse.rightButton;
					case KeyCode.Mouse2: return mouse.middleButton;
					case KeyCode.Mouse3: return mouse.forwardButton;
					case KeyCode.Mouse4: return mouse.backButton;
				}
			}

			var keyboard = Keyboard.current;
			if (keyboard == null)
				return null;

			Key key = ToKey(_keyCode);
			return key == Key.None ? null : keyboard[key];
		}

		private static Key ToKey( KeyCode _keyCode )
		{
			if (_keyCode >= KeyCode.A && _keyCode <= KeyCode.Z)
				return Key.A + (_keyCode - KeyCode.A);

			if (_keyCode == KeyCode.Alpha0)
				return Key.Digit0;
			if (_keyCode >= KeyCode.Alpha1 && _keyCode <= KeyCode.Alpha9)
				return Key.Digit1 + (_keyCode - KeyCode.Alpha1);

			if (_keyCode >= KeyCode.Keypad0 && _keyCode <= KeyCode.Keypad9)
				return Key.Numpad0 + (_keyCode - KeyCode.Keypad0);

			if (_keyCode >= KeyCode.F1 && _keyCode <= KeyCode.F12)
				return Key.F1 + (_keyCode - KeyCode.F1);

			return s_keyByKeyCode.TryGetValue(_keyCode, out Key key) ? key : Key.None;
		}
	}
}
