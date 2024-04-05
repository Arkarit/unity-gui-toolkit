// Note: don't move this file to an Editor folder, since it needs to be available
// for inplace editor code
#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GuiToolkit
{
	public static class EditorSceneUtility
	{
		public enum ESceneUnloadChoice
		{
			Save,
			Ignore,
			Cancel,
		}

		public static ESceneUnloadChoice DoDirtyEditorScenesCloseDialog()
		{
			return (ESceneUnloadChoice) EditorUtility.DisplayDialogComplex("Pending Scene changes", "Some scenes have been changed. How should we continue?", "Save", "Ignore", "Cancel" );
		}

		public static ESceneUnloadChoice DoDirtyEditorSceneCloseDialog(string _sceneName)
		{
			return (ESceneUnloadChoice) EditorUtility.DisplayDialogComplex("Pending Scene changes", $"Scene '{_sceneName}' has been changed. How should we continue?", "Save", "Ignore", "Cancel");
		}

		public static bool CheckBeforeCloseEditorScenes()
		{
			Scene mainScene = BuildSettingsUtility.GetMainScene();
			int numScenes = SceneManager.loadedSceneCount;
			bool saveScene = false;

			for (int i=0; i<numScenes; i++)
			{
				Scene scene = EditorSceneManager.GetSceneAt(i);
				if (scene != mainScene && scene.isDirty)
				{

					if (!saveScene)
					{
						ESceneUnloadChoice choice = DoDirtyEditorScenesCloseDialog();
						switch (choice)
						{
							case ESceneUnloadChoice.Ignore:
								return true;
							case ESceneUnloadChoice.Cancel:
								return false;
							case ESceneUnloadChoice.Save:
								saveScene = true;
								break;
							default:
								Debug.Assert(false);
								return false;
						}

						EditorSceneManager.SaveScene(scene);
					}
				}
			}
			return true;
		}

		public static bool CloseAllEditorScenesExcept( Scene _scene, out List<string> _sceneNames )
		{
			_sceneNames = new List<string>();

			int numScenes = SceneManager.loadedSceneCount;
			for (int i = 0; i < numScenes; i++)
			{
				Scene scene = EditorSceneManager.GetSceneAt(i);
				if (scene != _scene)
					_sceneNames.Add(scene.name);
			}

			if (!CheckBeforeCloseEditorScenes())
				return false;

			foreach (var sceneName in _sceneNames)
				EditorSceneManager.CloseScene(EditorSceneManager.GetSceneByName(sceneName), true);

			return true;
		}

		public static bool CloseAllEditorScenesExcept( Scene _scene )
		{
			return CloseAllEditorScenesExcept(_scene, out List<string> _);
		}

		public static bool CloseAllEditorScenesExceptMain( out List<string> _sceneNames )
		{
			Scene mainScene = BuildSettingsUtility.GetMainScene();
			return CloseAllEditorScenesExcept(mainScene, out _sceneNames);
		}

		public static void CloseEditorScene( Scene _scene )
		{
			if (!_scene.isLoaded)
				return;

			if (SceneManager.loadedSceneCount == 1)
			{
				Debug.LogWarning("Failed attempt to unload scene, but it was the only scene loaded");
				return;
			}

			if (_scene.isDirty)
			{
				ESceneUnloadChoice choice = DoDirtyEditorSceneCloseDialog( _scene.name );
				switch( choice )
				{
					case ESceneUnloadChoice.Ignore:
						break;
					case ESceneUnloadChoice.Cancel:
						return;
					case ESceneUnloadChoice.Save:
						EditorSceneManager.SaveScene(_scene);
						break;
					default:
						Debug.Assert(false);
						break;
				}
			}

			EditorSceneManager.CloseScene(_scene, true);
		}
	}
}
#endif