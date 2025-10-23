#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GuiToolkit.Editor
{
	public static class SceneMenuGeneratorTemplate
	{
/* TEMPLATE */
		private static void OpenScene( string _path, bool _additive )
		{
			if (!_additive)
			{
				if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
					return;

				EditorSceneManager.OpenScene(_path, OpenSceneMode.Single);
				return;
			}

			// Additive: keep current scene(s)
			EditorSceneManager.OpenScene(_path, OpenSceneMode.Additive);
			Scene opened = SceneManager.GetSceneByPath(_path);
			if (opened.IsValid())
			{
				SceneManager.SetActiveScene(opened);
			}
		}
	}
}
#endif
