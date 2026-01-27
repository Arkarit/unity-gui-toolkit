using UnityEngine;

namespace GuiToolkit
{
	public static class GeneralUtility
	{
		public static bool IsQuitting { get; private set; }
		public static int MainThreadId { get; private set; }

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void Init()
		{
			Application.quitting += () => IsQuitting = true;
			MainThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
		}

		public static bool InMainThread => MainThreadId == System.Threading.Thread.CurrentThread.ManagedThreadId;


		public static bool IsModifierKey( KeyCode _keyCode )
		{
			return _keyCode == KeyCode.LeftShift
			       || _keyCode == KeyCode.RightShift
			       || _keyCode == KeyCode.LeftControl
			       || _keyCode == KeyCode.RightControl
			       || _keyCode == KeyCode.LeftAlt
			       || _keyCode == KeyCode.RightAlt;
		}
	}
}