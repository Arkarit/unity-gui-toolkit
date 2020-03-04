#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
/// <summary>
/// Various BuildSettings interactions
/// </summary>
namespace GuiToolkit
{
	static public class BuildUtils
	{
		public const int SCENE_NOT_IN_BUILD = -1;
		public const int SCENE_DISABLED = -2;

		// time in seconds that we have to wait before we query again when IsReadOnly() is called.
		public static float minCheckWait = 3;

		static float lastTimeChecked = 0;
		static bool cachedReadonlyVal = true;

		/// <summary>
		/// A small container for tracking scene data BuildSettings
		/// </summary>
		public struct BuildScene
		{
			public int buildIndex;
			public GUID assetGUID;
			public string assetPath;
			public EditorBuildSettingsScene scene;
			public bool loadedInEditor;
			public bool enabled;
		}

		/// <summary>
		/// Check if the build settings asset is readonly.
		/// Caches value and only queries state a max of every 'minCheckWait' seconds.
		/// </summary>
		static public bool IsReadOnly()
		{
			float curTime = Time.realtimeSinceStartup;
			float timeSinceLastCheck = curTime - lastTimeChecked;

			if (timeSinceLastCheck > minCheckWait)
			{
				lastTimeChecked = curTime;
				cachedReadonlyVal = QueryBuildSettingsStatus();
			}

			return cachedReadonlyVal;
		}

		/// <summary>
		/// A blocking call to the Version Control system to see if the build settings asset is readonly.
		/// Use BuildSettingsIsReadOnly for version that caches the value for better responsivenes.
		/// </summary>
		static private bool QueryBuildSettingsStatus()
		{
			// If no version control provider, assume not readonly
			if (UnityEditor.VersionControl.Provider.enabled == false)
				return false;

			// If we cannot checkout, then assume we are not readonly
			if (UnityEditor.VersionControl.Provider.hasCheckoutSupport == false)
				return false;

			//// If offline (and are using a version control provider that requires checkout) we cannot edit.
			//if (UnityEditor.VersionControl.Provider.onlineState == UnityEditor.VersionControl.OnlineState.Offline)
			//    return true;

			// Try to get status for file
			var status = UnityEditor.VersionControl.Provider.Status("ProjectSettings/EditorBuildSettings.asset", false);
			status.Wait();

			// If no status listed we can edit
			if (status.assetList == null || status.assetList.Count != 1)
				return true;

			// If is checked out, we can edit
			if (status.assetList[0].IsState(UnityEditor.VersionControl.Asset.States.CheckedOutLocal))
				return false;

			return true;
		}

		/// <summary>
		/// For a given Scene Asset object reference, extract its build settings data, including buildIndex.
		/// </summary>
		static public BuildScene GetBuildScene( UnityEngine.Object sceneObject )
		{
			BuildScene entry = new BuildScene()
			{
				buildIndex = SCENE_NOT_IN_BUILD,
				assetGUID = new GUID(string.Empty)
			};

			if (sceneObject as SceneAsset == null)
				return entry;

			entry.assetPath = AssetDatabase.GetAssetPath(sceneObject);
			entry.assetGUID = new GUID(AssetDatabase.AssetPathToGUID(entry.assetPath));
			Scene scene = EditorSceneManager.GetSceneByPath(entry.assetPath);
			entry.loadedInEditor = scene != null && scene.isLoaded;
			entry.enabled = !Application.isPlaying && (!entry.loadedInEditor || EditorSceneManager.sceneCount > 1);

			for (int index = 0, sceneIndex = 0; index < EditorBuildSettings.scenes.Length; ++index)
			{
				if (entry.assetGUID.Equals(EditorBuildSettings.scenes[index].guid))
				{
					entry.scene = EditorBuildSettings.scenes[index];
					entry.buildIndex = entry.scene.enabled ? sceneIndex : SCENE_DISABLED;
					return entry;
				}

				EditorBuildSettingsScene sc = EditorBuildSettings.scenes[index];
				if (sc.enabled)
					sceneIndex++;
			}
			return entry;
		}

		/// <summary>
		/// Enable/Disable a given scene in the buildSettings
		/// </summary>
		static public void SetBuildSceneState( BuildScene buildScene, bool enabled )
		{
			bool modified = false;
			EditorBuildSettingsScene[] scenesToModify = EditorBuildSettings.scenes;
			foreach (var curScene in scenesToModify)
			{
				if (curScene.guid.Equals(buildScene.assetGUID))
				{
					curScene.enabled = enabled;
					modified = true;
					break;
				}
			}
			if (modified)
				EditorBuildSettings.scenes = scenesToModify;
		}

		/// <summary>
		/// Display Dialog to add a scene to build settings
		/// </summary>
		static public void AddBuildScene( BuildScene buildScene, bool force = false, bool enabled = true )
		{
			if (force == false)
			{
				int selection = EditorUtility.DisplayDialogComplex(
					"Add Scene To Build",
					"You are about to add scene at " + buildScene.assetPath + " To the Build Settings.",
					"Add as Enabled",       // option 0
					"Add as Disabled",      // option 1
					"Cancel (do nothing)"); // option 2

				switch (selection)
				{
					case 0: // enabled
						enabled = true;
						break;
					case 1: // disabled
						enabled = false;
						break;
					default:
					case 2: // cancel
						return;
				}
			}

			EditorBuildSettingsScene newScene = new EditorBuildSettingsScene(buildScene.assetGUID, enabled);
			List<EditorBuildSettingsScene> tempScenes = EditorBuildSettings.scenes.ToList();
			tempScenes.Add(newScene);
			EditorBuildSettings.scenes = tempScenes.ToArray();
		}

		/// <summary>
		/// Display Dialog to remove a scene from build settings (or just disable it)
		/// </summary>
		static public void RemoveBuildScene( BuildScene buildScene, bool force = false )
		{
			bool onlyDisable = false;
			if (force == false)
			{
				int selection = -1;

				string title = "Remove Scene From Build";
				string details = string.Format("You are about to remove the following scene from build settings:\n    {0}\n    buildIndex: {1}\n\n{2}",
								buildScene.assetPath, buildScene.buildIndex,
								"This will modify build settings, but the scene asset will remain untouched.");
				string confirm = "Remove From Build";
				string alt = "Just Disable";
				string cancel = "Cancel (do nothing)";

				if (buildScene.scene.enabled)
				{
					details += "\n\nIf you want, you can also just disable it instead.";
					selection = EditorUtility.DisplayDialogComplex(title, details, confirm, alt, cancel);
				}
				else
				{
					selection = EditorUtility.DisplayDialog(title, details, confirm, cancel) ? 0 : 2;
				}

				switch (selection)
				{
					case 0: // remove
						break;
					case 1: // disable
						onlyDisable = true;
						break;
					default:
					case 2: // cancel
						return;
				}
			}

			// User chose to not remove, only disable the scene
			if (onlyDisable)
			{
				SetBuildSceneState(buildScene, false);
			}
			// User chose to fully remove the scene from build settings
			else
			{
				List<EditorBuildSettingsScene> tempScenes = EditorBuildSettings.scenes.ToList();
				tempScenes.RemoveAll(scene => scene.guid.Equals(buildScene.assetGUID));
				EditorBuildSettings.scenes = tempScenes.ToArray();
			}
		}

		/// <summary>
		/// Open the default Unity Build Settings window
		/// </summary>
		static public void OpenBuildSettings()
		{
			EditorWindow.GetWindow(typeof(BuildPlayerWindow));
		}

		/// <summary>
		/// Checks if a scene with the build index 0 (the scene which is default loaded in build) exists in build settings
		/// </summary>
		/// <returns></returns>
		static public bool HasMainScene()
		{
			for (int index = 0; index < EditorBuildSettings.scenes.Length; ++index)
			{
				var buildScene = EditorBuildSettings.scenes[index];
				if (buildScene.enabled)
					return true;
			}
			return false;
		}

		/// <summary>
		/// Get the scene with the build index 0 (the scene which is default loaded in build)
		/// Prior to calling, please ensure that a main scene exists (with HasMainScene())
		/// </summary>
		/// <returns></returns>
		static public Scene GetMainScene()
		{
			for (int index = 0; index < EditorBuildSettings.scenes.Length; ++index)
			{
				var buildScene = EditorBuildSettings.scenes[index];
				if (buildScene.enabled)
				{
					return EditorSceneManager.GetSceneByPath(buildScene.path);
				}
			}

			return new Scene();
		}

		/// <summary>
		/// Get the scene with the build index 0 (the scene which is default loaded in build)
		/// Prior to calling, please ensure that a main scene exists (with HasMainScene())
		/// </summary>
		/// <returns></returns>
		static public string GetMainScenePath()
		{
			for (int index = 0; index < EditorBuildSettings.scenes.Length; ++index)
			{
				var buildScene = EditorBuildSettings.scenes[index];
				if (buildScene.enabled)
				{
					return buildScene.path;
				}
			}

			return null;
		}
	}
}
#endif