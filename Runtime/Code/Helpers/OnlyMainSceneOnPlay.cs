#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GuiToolkit
{
	[Serializable]
	public class EditorScenes
	{
		public List<string> Scenes = new List<string>();
		public bool MainSceneWasLoaded;
	}

	[InitializeOnLoad]
	public static class OnlyMainSceneOnPlay
	{

		static OnlyMainSceneOnPlay()
		{
			EditorApplication.playModeStateChanged += HandleScenes;
		}

		private static string TempFileName { get { return Application.temporaryCachePath + "/openScenes.txt"; } }


		private static void HandleScenes( PlayModeStateChange _state )
		{
			if (_state == PlayModeStateChange.EnteredPlayMode || _state == PlayModeStateChange.ExitingPlayMode)
				return;

			if (!BuildSettingsUtility.HasMainScene())
			{
				Debug.LogError("Automatic scene loading/unloading does not work when no main scene is defined. Please add at least one enabled scene to your build settings");
				return;
			}

			UiToolkitConfiguration settings = UiToolkitConfiguration.EditorLoad();

			if (!settings.m_loadMainSceneOnPlay)
				return;

			try
			{
				if (_state == PlayModeStateChange.ExitingEditMode)
				{

					EditorScenes editorScenes = new EditorScenes();
					editorScenes.MainSceneWasLoaded = LoadMainSceneIfNecessary();

					if (!UiEditorUtility.CloseAllEditorScenesExceptMain(out editorScenes.Scenes))
					{
						EditorApplication.isPlaying = false;
						return;
					}

					string s = JsonUtility.ToJson(editorScenes);
					File.WriteAllText(TempFileName, s);
				}

				if (_state == PlayModeStateChange.EnteredEditMode)
				{
					Scene mainScene = BuildSettingsUtility.GetMainScene();

					string s = File.ReadAllText(TempFileName);
					if (string.IsNullOrEmpty(s))
						return;

					EditorScenes editorScenes = JsonUtility.FromJson<EditorScenes>(s);
					foreach (string sceneName in editorScenes.Scenes)
						EditorSceneManager.OpenScene(UiToolkitConfiguration.GetProjectScenePath(sceneName), OpenSceneMode.Additive);
					File.Delete(TempFileName);
					if (!editorScenes.MainSceneWasLoaded)
						EditorSceneManager.CloseScene(EditorSceneManager.GetSceneByName(mainScene.name), true);
				}
			}
			catch { }

		}

		private static bool LoadMainSceneIfNecessary()
		{
			Scene mainScene = BuildSettingsUtility.GetMainScene();
			if (mainScene.isLoaded)
				return true;

			string mainScenePath = BuildSettingsUtility.GetMainScenePath();
			EditorSceneManager.OpenScene(mainScenePath, OpenSceneMode.Additive);
			return false;
		}


	}
}
#endif