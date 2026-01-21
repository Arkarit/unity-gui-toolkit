// Scene change tracker. Original from https://gist.github.com/wappenull/acfd388185c543bfe8fa98f6ab07f317
// See https://forum.unity.com/threads/how-to-know-what-makes-scene-dirty.694390

using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GuiToolkit.Editor
{
	/// <summary>
	/// Editor tool to track live scene changes.
	/// </summary>
	
	[InitializeOnLoad]
	public static class SceneChangeTracker
	{
		private const string LogPrefix = "Scene Change Tracker: ";
		private static bool s_enabled = false;

		private static readonly string PrefsKey = StringConstants.PLAYER_PREFS_PREFIX + nameof(SceneChangeTracker) + ".active";
 
		static SceneChangeTracker()
		{
			IsEnabled = EditorPrefs.GetBool(PrefsKey);
		}

		[MenuItem(StringConstants.SCENE_CHANGE_TRACKER_MENU_NAME, priority = Constants.SCENE_CHANGE_TRACKER_MENU_PRIORITY)]
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

				Menu.SetChecked(StringConstants.SCENE_CHANGE_TRACKER_MENU_NAME, s_enabled);
				if (s_enabled)
				{
					Undo.postprocessModifications += OnPostProcessModifications;
					EditorSceneManager.sceneDirtied += SceneDirtied;
				}
				else
				{
					Undo.postprocessModifications -= OnPostProcessModifications;
					EditorSceneManager.sceneDirtied -= SceneDirtied;
				}

				EditorPrefs.SetBool(PrefsKey, s_enabled);
			}
		}

		private static void SceneDirtied( Scene scene )
		{
			UiLog.LogInternal( $"sceneDirtied on {scene.name}. (See full stacktrace)" );
		}

		private static UndoPropertyModification[] OnPostProcessModifications( UndoPropertyModification[] propertyModifications )
		{
			foreach( UndoPropertyModification mod in propertyModifications )
				UiLog.LogInternal(BuildLogMessage(mod));

			return propertyModifications;
		}

		private static string BuildLogMessage(UndoPropertyModification mod)
		{
			string result = LogPrefix;
			MonoBehaviour monoBehaviour = mod.currentValue.target as MonoBehaviour;
			if (monoBehaviour)
			{
				result += $"'{monoBehaviour.gameObject.name}.{monoBehaviour.GetType().Name}.{mod.currentValue.propertyPath}'";
			}
			else
			{
				result += $"type:'{mod.currentValue.target.GetType().Name}' name:'{mod.currentValue.target.name}'";
			}

			result += $" from {mod.previousValue.value} to {mod.currentValue.value}";
			return result;
		}
	}
}