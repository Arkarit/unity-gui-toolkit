using UnityEditor;

namespace GuiToolkit.Editor
{
	[InitializeOnLoad]
	public static class UiToolkitConfigurationFirstTimeInit
	{
		private static UiToolkitConfigurationWindow s_window;

		static UiToolkitConfigurationFirstTimeInit()
		{
			if (UiToolkitConfiguration.Initialized)
				return;

			EditorApplication.update += CreateWindow;
		}

		private static void CreateWindow()
		{
			EditorApplication.update -= CreateWindow;

			s_window = UiToolkitConfigurationWindow.GetWindow();
			s_window.Show();
		}
	}
}