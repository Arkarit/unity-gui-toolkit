using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Editor
{
	/// <summary>
	/// Editor tool to save project when editor application loses focus.
	///
	/// A frequent cause of commit errors is that Unity caches all its asset changes as long as possible.
	/// Only if you explicitly save the project, the assets are written to disk.
	/// This small tool avoids this; if you switch to your git client, all changes are automatically written to disk.
	/// </summary>
	
	[InitializeOnLoad]
	public static class SaveProjectOnLoseFocus
	{
		private const string MenuEntry = StringConstants.MENU_HEADER + "Tools/Save Project on Lose Focus";
		private static bool s_enabled = false;

		private static readonly string PrefsKey = UnityEditor.PlayerSettings.productName + "." + nameof(SaveProjectOnLoseFocus) + ".active";
 
		static SaveProjectOnLoseFocus()
		{
			IsEnabled = EditorPrefs.GetBool(PrefsKey);
		}

		[MenuItem(MenuEntry)]
		private static void Toggle()
		{
			IsEnabled = !IsEnabled;
		}

		private static bool IsEnabled
		{
			get => s_enabled;
			set
			{
				if (value == s_enabled)
					return;

				s_enabled = value;

				Menu.SetChecked(MenuEntry, s_enabled);

				if (s_enabled)
					EditorApplication.focusChanged += OnFocusChanged;
				else
					EditorApplication.focusChanged -= OnFocusChanged;

				EditorPrefs.SetBool(PrefsKey, s_enabled);
			}
		}

		private static void OnFocusChanged(bool hasFocus)
		{
			if (!hasFocus) 
				return;

			Debug.Log("Lost focus, saving assets");
			AssetDatabase.SaveAssets();
		}
	}
}