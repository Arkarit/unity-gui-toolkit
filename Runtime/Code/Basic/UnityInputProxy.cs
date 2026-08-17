using UnityEngine;

namespace GuiToolkit
{
#if ENABLE_LEGACY_INPUT_MANAGER

	/// <summary>
	/// <see cref="IInputProxy"/> reading the legacy <see cref="Input"/> class.
	/// </summary>
	/// <remarks>
	/// Compiled only while the legacy input manager is active (Active Input Handling "Input Manager
	/// (Old)" or "Both"). Without that guard every read here throws instead of returning false, once
	/// per frame from <c>UiSound.Update</c> — so the guard is what turns a flood of exceptions into a
	/// build-time absence. Do not construct it directly; ask <see cref="InputProxyFactory"/>, which
	/// knows which backend a project actually has.
	/// </remarks>
	public sealed class UnityInputProxy : IInputProxy
	{
		public Vector3 MousePosition => Input.mousePosition;
		public bool GetKey( KeyCode _keyCode ) => Input.GetKey(_keyCode);
		public bool GetKeyDown( KeyCode _keyCode ) => Input.GetKeyDown(_keyCode);
		public bool GetKeyUp( KeyCode _keyCode ) => Input.GetKeyUp(_keyCode);
	}

#endif
}
