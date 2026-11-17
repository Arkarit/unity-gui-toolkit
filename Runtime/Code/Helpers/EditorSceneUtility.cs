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

		/// <summary>
		/// The answer the "Pending Scene changes" dialogs give without asking; null asks the user, as always.
		/// </summary>
		/// <remarks>
		/// For automation that changes the editor's state while nobody watches it, e.g. the MCP bridge entering
		/// Play Mode with Only Main Scene On Play on: the dialog is modal, so with nobody to click it the editor
		/// stands still. Whoever sets it clears it again; a domain reload clears it too.
		/// </remarks>
		public static ESceneUnloadChoice? NonInteractiveChoice { get; set; }

		public static ESceneUnloadChoice DoDirtyEditorScenesCloseDialog()
		{
			if (NonInteractiveChoice.HasValue)
				return NonInteractive("Some scenes have been changed");

			return (ESceneUnloadChoice) EditorUtility.DisplayDialogComplex("Pending Scene changes", "Some scenes have been changed. How should we continue?", "Save", "Ignore", "Cancel" );
		}

		public static ESceneUnloadChoice DoDirtyEditorSceneCloseDialog(string _sceneName)
		{
			if (NonInteractiveChoice.HasValue)
				return NonInteractive($"Scene '{_sceneName}' has been changed");

			return (ESceneUnloadChoice) EditorUtility.DisplayDialogComplex("Pending Scene changes", $"Scene '{_sceneName}' has been changed. How should we continue?", "Save", "Ignore", "Cancel");
		}

		private static ESceneUnloadChoice NonInteractive( string _what )
		{
			var choice = NonInteractiveChoice.Value;
			UiLog.Log($"Pending Scene changes: {_what}; answered '{choice}' without asking ({nameof(NonInteractiveChoice)}).");
			return choice;
		}

		/// <summary>
		/// The loaded scenes with unsaved changes, except the main scene: what closing all scenes but the main one
		/// (<see cref="CloseAllEditorScenesExceptMain"/>, Only Main Scene On Play) would ask about.
		/// </summary>
		public static List<string> DirtyScenesExceptMain()
		{
			var result = new List<string>();
			Scene mainScene = BuildSettingsUtility.GetMainScene();
			for (int i = 0; i < SceneManager.loadedSceneCount; i++)
			{
				Scene scene = EditorSceneManager.GetSceneAt(i);
				if (scene != mainScene && scene.isDirty)
					result.Add(scene.path);
			}

			return result;
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
				UiLog.LogWarning("Failed attempt to unload scene, but it was the only scene loaded");
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