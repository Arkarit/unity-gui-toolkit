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
	}

	[InitializeOnLoad]
	public static class OnlyActiveScene
	{

		static OnlyActiveScene()
		{
			EditorApplication.playModeStateChanged += HandleScenes;
		}

		private static string TempFileName { get { return Application.temporaryCachePath + "/openScenes.txt"; } }

		private static void HandleScenes( PlayModeStateChange _state )
		{
			if (_state == PlayModeStateChange.EnteredPlayMode || _state == PlayModeStateChange.ExitingPlayMode)
				return;

			UiSettings settings = UiSettings.EditorLoad();

			if (!settings.m_unloadAdditionalScenesOnPlay)
				return;

			try
			{
				if (_state == PlayModeStateChange.ExitingEditMode)
				{
					EditorScenes editorScenes = new EditorScenes();

					for (int i = 0; i < EditorSceneManager.loadedSceneCount; i++)
					{
						Scene scene = EditorSceneManager.GetSceneAt(i);
						if (scene != EditorSceneManager.GetActiveScene())
							editorScenes.Scenes.Add(scene.name);
					}

					foreach (string sceneName in editorScenes.Scenes)
						EditorSceneManager.CloseScene(EditorSceneManager.GetSceneByName(sceneName), true);

					string s = JsonUtility.ToJson(editorScenes);
					File.WriteAllText(TempFileName, s);
				}

				if (_state == PlayModeStateChange.EnteredEditMode)
				{
					string s = File.ReadAllText(TempFileName);
					EditorScenes editorScenes = JsonUtility.FromJson<EditorScenes>(s);
					foreach (string sceneName in editorScenes.Scenes)
						EditorSceneManager.OpenScene("Assets/" + settings.m_scenesPath + sceneName + ".unity", OpenSceneMode.Additive);
					File.Delete(TempFileName);
				}
			}
			catch { }

		}

	}
}
#endif