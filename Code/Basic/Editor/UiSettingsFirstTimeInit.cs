using UnityEditor;
using UnityEngine;

namespace GuiToolkit
{
	[InitializeOnLoad]
	public static class UiSettingsFirstTimeInit
	{
		private static UiSettingsWindow s_window;

		static UiSettingsFirstTimeInit()
		{
			if (UiSettings.Initialized)
				return;

			EditorApplication.update += CreateWindow;
		}

		private static void CreateWindow()
		{
			EditorApplication.update -= CreateWindow;

			s_window = UiSettingsWindow.GetWindow();
			s_window.Show();
		}
	}
}