using UnityEditor;

namespace GuiToolkit.Editor
{
	[InitializeOnLoad]
	[EditorAware]
	public static class UiToolkitConfigurationFirstTimeInit
	{
		private static UiToolkitConfigurationWindow s_window;

		static UiToolkitConfigurationFirstTimeInit()
		{
			AssetReadyGate.WhenReady(() =>
			{
				EditorApplication.update += CreateWindow;
			});
		}

		private static void CreateWindow()
		{
			EditorApplication.update -= CreateWindow;
			if (AssetReadyGate.AssetExists(UiToolkitConfiguration.AssetPath))
				return;

			s_window = UiToolkitConfigurationWindow.GetWindow();
			s_window.Show();
		}
	}
}