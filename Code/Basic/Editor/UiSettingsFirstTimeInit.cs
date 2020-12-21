using UnityEditor;
using UnityEngine;

namespace GuiToolkit
{
	[InitializeOnLoad]
	public static class UiSettingsFirstTimeInit
	{
		private static UiToolkitMainSettingsWindow s_window;

		static UiSettingsFirstTimeInit()
		{
			if (UiToolkitMainSettings.Initialized)
				return;

			EditorApplication.update += CreateWindow;
		}

		private static void CreateWindow()
		{
			EditorApplication.update -= CreateWindow;

			s_window = UiToolkitMainSettingsWindow.GetWindow();
			s_window.Show();
		}
	}
}