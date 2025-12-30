using UnityEngine;

namespace GuiToolkit
{
	public static class GeneralUtility
	{
		public static bool IsQuitting { get; private set; }
		public static int MainThreadId { get; private set; }

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		private static void Init()
		{
			Application.quitting += () => IsQuitting = true;
			MainThreadId = System.Threading.Thread.CurrentThread.ManagedThreadId;
		}

		public static bool InMainThread => MainThreadId == System.Threading.Thread.CurrentThread.ManagedThreadId;
	}
}