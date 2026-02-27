// Note: don't move this file to an Editor folder, since it needs to be available
// for inplace editor code
#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
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
			return (ESceneUnloadChoice)EditorUtility.DisplayDialogComplex("Pending Scene changes", "Some scenes have been changed. How should we continue?", "Save", "Ignore", "Cancel");
		}

		public static ESceneUnloadChoice DoDirtyEditorSceneCloseDialog( string _sceneName )
		{
			return (ESceneUnloadChoice)EditorUtility.DisplayDialogComplex("Pending Scene changes", $"Scene '{_sceneName}' has been changed. How should we continue?", "Save", "Ignore", "Cancel");
		}

		public static bool CheckBeforeCloseEditorScenes()
		{
			Scene mainScene = BuildSettingsUtility.GetMainScene();
			int numScenes = SceneManager.loadedSceneCount;
			bool saveScene = false;

			for (int i = 0; i < numScenes; i++)
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
				UiLog.LogWarning("Failed attempt to unload scene, but it was the only scene loaded");
				return;
			}

			if (_scene.isDirty)
			{
				ESceneUnloadChoice choice = DoDirtyEditorSceneCloseDialog(_scene.name);
				switch (choice)
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

		public static Scene LoadScene(
			string _scenePath,
			OpenSceneMode _loadMode = OpenSceneMode.Single,
			bool _keepEditingPrefab = true,
			Action<Scene> _preLoadAction = null,
			Action<Scene> _postLoadAction = null )
		{
			// Save all dirty assets
			SaveAllDirtyAssets();

			// Check if we're in Prefab Mode
			var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
			string prefabPath = null;

			if (prefabStage != null)
			{
				// Save the currently edited prefab if dirty
				SaveCurrentlyEditedPrefabIfDirty();

				if (_keepEditingPrefab)
				{
					// Store the prefab path to restore later
					prefabPath = prefabStage.assetPath;
				}

				// Explicitly close the prefab scene to avoid "Do you want to save" dialog
				EditorSceneManager.CloseScene(prefabStage.scene, true);
			}

			// Execute pre-load action
			var currentScene = SceneManager.GetActiveScene();
			_preLoadAction?.Invoke(currentScene);

			// Load the scene
			var loadedScene = EditorSceneManager.OpenScene(_scenePath, _loadMode);

			// Restore Prefab Mode if needed
			if (_keepEditingPrefab && prefabPath != null)
			{
				PrefabStageUtility.OpenPrefab(prefabPath);
			}

			// Execute post-load action
			_postLoadAction?.Invoke(loadedScene);
			return loadedScene;
		}

		private static void SaveAllDirtyAssets()
		{
			// Save dirty prefab
			var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
			if (prefabStage != null && prefabStage.scene.isDirty)
			{
				EditorSceneManager.SaveScene(prefabStage.scene);
			}

			// Save all open scenes
			EditorSceneManager.SaveOpenScenes();

			// Save other assets
			AssetDatabase.SaveAssets();
		}

		private static void SaveCurrentlyEditedPrefabIfDirty()
		{
			// Check if we're in Prefab Mode
			var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
			if (prefabStage != null && prefabStage.scene.isDirty)
			{
				// Save the prefab stage
				EditorSceneManager.SaveScene(prefabStage.scene);
				AssetDatabase.SaveAssets(); // Ensure all assets are saved
				UiLog.Log($"Saved dirty prefab: {prefabStage.assetPath}");
			}
		}

	}
}
#endif