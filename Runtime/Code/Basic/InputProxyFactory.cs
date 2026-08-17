using UnityEngine;

namespace GuiToolkit
{
	/// <summary>
	/// Decides which <see cref="IInputProxy"/> the toolkit reads input through, so that no call site
	/// has to know which input backend a project uses.
	/// </summary>
	/// <remarks>
	/// This exists because the toolkit used to construct <see cref="UnityInputProxy"/> directly in two
	/// places, and that proxy reads the legacy <c>Input</c> class. Under Active Input Handling
	/// "Input System Package (New)" — the default of a Unity 6 URP template — every such read THROWS
	/// rather than returning false. Coming from <c>UiSound.Update</c>, that is one exception per frame,
	/// and the symptom is a screen that looks broken with nothing in the log naming the cause.
	///
	/// Resolution order: a proxy registered by someone else wins; otherwise the legacy proxy if the
	/// legacy manager is compiled in; otherwise <see cref="NullInputProxy"/>, which reads as "nothing
	/// pressed" and says once what to do about it.
	///
	/// The Input System integration lives in its own assembly (<c>Runtime/Code/InputSystem</c>) which
	/// compiles only when that package is installed, and registers itself here at
	/// <see cref="RuntimeInitializeLoadType.SubsystemRegistration"/> — before any scene, so before
	/// anything can ask for a proxy.
	/// </remarks>
	public static class InputProxyFactory
	{
		private static System.Func<IInputProxy> s_provider;
		private static IInputProxy s_default;

		/// <summary>
		/// Installs the proxy the toolkit should use by default. Call before the first read; the
		/// Input System integration does this automatically.
		/// </summary>
		public static void Register( System.Func<IInputProxy> _provider )
		{
			s_provider = _provider;
			// Anything handed out earlier was built from the previous answer and is now wrong.
			s_default = null;
		}

		/// <summary>
		/// The shared default proxy. Created on first use, so registration order does not matter.
		/// </summary>
		public static IInputProxy Default => s_default ??= Create();

		/// <summary>Builds a fresh proxy without touching the shared one.</summary>
		public static IInputProxy Create()
		{
			if (s_provider != null)
				return s_provider();

#if ENABLE_LEGACY_INPUT_MANAGER
			return new UnityInputProxy();
#else
			return new NullInputProxy();
#endif
		}
	}

	/// <summary>
	/// Answers "not pressed" to everything, and explains itself once.
	/// </summary>
	/// <remarks>
	/// Used when the legacy input manager is switched off and no Input System integration is present —
	/// which is the case where the toolkit genuinely cannot read input. Reporting that once and then
	/// staying quiet beats either throwing per frame or failing silently.
	/// </remarks>
	public sealed class NullInputProxy : IInputProxy
	{
		private static bool s_reported;

		public NullInputProxy()
		{
			if (s_reported)
				return;

			s_reported = true;
			// Deliberately describes the state rather than naming one cause: the legacy manager being off
			// is certain here, but "the Input System package is missing" is only the most likely reason no
			// proxy registered, and claiming it when the package IS installed sends people the wrong way.
			UiLog.LogError(
				"No input backend available to the UI toolkit, so it reads every key and mouse button as " +
				"'not pressed'. The legacy input manager is switched off (Active Input Handling is " +
				"'Input System Package (New)') and no other proxy has registered. Install " +
				"com.unity.inputsystem — the toolkit then wires itself up — or switch Active Input Handling " +
				"to 'Both', or assign PlayerSettings.Instance.InputProxy yourself.");
		}

		public Vector3 MousePosition => Vector3.zero;
		public bool GetKey( KeyCode _keyCode ) => false;
		public bool GetKeyDown( KeyCode _keyCode ) => false;
		public bool GetKeyUp( KeyCode _keyCode ) => false;
	}
}
