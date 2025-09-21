using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GuiToolkit
{
	[Serializable]
	public class EditorScenes
	{
		public List<string> Scenes;
		public bool MainSceneWasLoaded;
	}

	[InitializeOnLoad]
	[EditorAware]
	public static class OnlyMainSceneOnPlay
	{
		static OnlyMainSceneOnPlay()
		{
			EditorApplication.playModeStateChanged += HandleScenes;
		}

		private static string TempFileName => Path.Combine(Application.temporaryCachePath, "openScenes.txt");

		// Matches Unity's InitTestScene with optional GUID/hex suffix
		private static readonly Regex s_initTestSceneRegEx = new Regex(
			pattern: @"^InitTestScene(?:$|[ \-_]?[0-9A-Fa-f]{8}(?:-[0-9A-Fa-f]{4}){3}-[0-9A-Fa-f]{12})(?:\.unity)?$",
			options: RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase
		);

		public static bool IsPlayModeTestRun()
		{
			int numScenes = SceneManager.loadedSceneCount;
			for (int i = 0; i < numScenes; i++)
			{
				var scene = SceneManager.GetSceneAt(i);
				if (s_initTestSceneRegEx.IsMatch(scene.name))
					return true;
			}

			return false;
		}

		private static void HandleScenes( PlayModeStateChange _state )
		{
			// Don't do anything if not ready yet
			if (!AssetReadyGate.Ready)
				return;
			
			// Do not interfere with PlayMode tests
			if (IsPlayModeTestRun())
				return;

			if (!BuildSettingsUtility.HasMainScene())
			{
				UiLog.LogError("Automatic scene loading requires a main scene in Build Settings.");
				return;
			}

			if (!UiToolkitConfiguration.Instance.LoadMainSceneOnPlay)
				return;

			try
			{
				switch (_state)
				{
					case PlayModeStateChange.ExitingEditMode:
						{
							var editorScenes = new EditorScenes
							{
								MainSceneWasLoaded = LoadMainSceneIfNecessary()
							};

							if (!EditorSceneUtility.CloseAllEditorScenesExceptMain(out editorScenes.Scenes))
							{
								EditorApplication.isPlaying = false;
								return;
							}

							var json = JsonUtility.ToJson(editorScenes);
							File.WriteAllText(TempFileName, json);
							break;
						}

					case PlayModeStateChange.EnteredEditMode:
						{
							// Restore previously open editor scenes
							if (!File.Exists(TempFileName))
								return;

							var json = File.ReadAllText(TempFileName);
							if (string.IsNullOrEmpty(json))
								return;

							var editorScenes = JsonUtility.FromJson<EditorScenes>(json);
							File.Delete(TempFileName);

							foreach (var sceneName in editorScenes.Scenes)
							{
								var path = UiToolkitConfiguration.Instance.GetProjectScenePath(sceneName);
								if (!string.IsNullOrEmpty(path))
									EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
							}

							if (!editorScenes.MainSceneWasLoaded)
							{
								var mainScene = BuildSettingsUtility.GetMainScene();
								var s = EditorSceneManager.GetSceneByName(mainScene.name);
								if (s.IsValid())
									EditorSceneManager.CloseScene(s, removeScene: true);
							}

							break;
						}
				}
			}
			catch (Exception ex)
			{
				// do not swallow errors silently; tests and devs need signal
				Debug.LogException(ex);
			}
		}

		private static bool LoadMainSceneIfNecessary()
		{
			var mainScene = BuildSettingsUtility.GetMainScene();
			if (mainScene.isLoaded)
				return true;

			var mainScenePath = BuildSettingsUtility.GetMainScenePath();
			EditorSceneManager.OpenScene(mainScenePath, OpenSceneMode.Additive);
			return false;
		}
	}
}
