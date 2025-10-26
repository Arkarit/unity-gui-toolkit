using UnityEngine;

namespace GuiToolkit
{
	public static class GeneralUtility
	{
		public static bool IsQuitting { get; private set; }

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
		private static void Init()
		{
			Application.quitting += () => IsQuitting = true;
		}
	}
}