using UnityEditor;

namespace GuiToolkit.Editor
{
	[InitializeOnLoad]
	public static class UiToolkitConfigurationFirstTimeInit
	{
		private static UiToolkitConfigurationWindow s_window;

		static UiToolkitConfigurationFirstTimeInit()
		{
			UiToolkitConfiguration.WhenReady(_ =>
			{
				s_window = UiToolkitConfigurationWindow.GetWindow();
				if (s_window.FirstTimeInit)
				{
					s_window.Show();
				}
			});
		}
	}
}