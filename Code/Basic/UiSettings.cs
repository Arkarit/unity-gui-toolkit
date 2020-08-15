using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GuiToolkit
{
	//[CreateAssetMenu]
	public class UiSettings : ScriptableObject
	{
		public const string SETTINGS_FILE = "UiSettings";
		public const string SETTINGS_RUNTIME_DIR = "";
		public const string SETTINGS_RUNTIME_PATH = SETTINGS_FILE;
		public const string SETTINGS_EDITOR_DIR = "Assets/Resources" + SETTINGS_RUNTIME_DIR;
		public const string SETTINGS_EDITOR_PATH = SETTINGS_EDITOR_DIR + "/" + SETTINGS_FILE + ".asset";

		public static readonly string SETTINGS_HELP_FIRST_TIME =
			  $"It appears that you are using the {StringConstants.TOOLKIT_NAME} for the first time\n"
			+ $"The scriptable object '{SETTINGS_EDITOR_PATH}' has been created to store your {StringConstants.TOOLKIT_NAME} settings.\n"
			+ $"Please be sure to check it in to your code versioning system!\n\n"
			+ $"You can always access this window from the menu: '{StringConstants.SETTINGS_MENU_NAME}'\n\n"
			+ $"Please check the settings below to create the initial setup for {StringConstants.TOOLKIT_NAME}:"
			;

		public const string SETTINGS_HELP_SCENES =
			  "A shortcut to the Unity 'File/Build Settings/Scenes in Build' field\n"
			+ "You can add scenes to the build, enable or disable them or remove from build.\n"
			+ "However, the main purpose of this field is to be a convenient scene loader.\n"
			+ "By setting/clearing the checkmark on 'Loaded in Editor' you can very simply load/unload scenes in the editor additively.\n"
			+ "By setting the checkmark together with the shift key, cou can load a scene exclusively."
			;

		public const string SETTINGS_HELP_LOAD_MAIN_SCENE_ON_PLAY =
			  "When enabled, the main scene is loaded when you press play in the editor, and all other scenes are unloaded.\n" 
			+ "After play, the scenes, which were previously loaded, are restored."
			;

		public const string SETTINGS_HELP_ADDITIONAL_SCENES_PATH =
			"Additional scene path for scenes, which are not in the scene references list";

		[Tooltip(SETTINGS_HELP_SCENES)]
		public SceneReference[] m_sceneReferences;

		[Tooltip(SETTINGS_HELP_LOAD_MAIN_SCENE_ON_PLAY)]
		public bool m_loadMainSceneOnPlay = false;

		[Tooltip(SETTINGS_HELP_ADDITIONAL_SCENES_PATH)]
		public string m_additionalScenesPath = "Scenes/";

		private readonly Dictionary<string, SceneReference> m_scenesByName = new Dictionary<string, SceneReference>();

		private static UiSettings s_instance;

		public static UiSettings Instance
		{
			get
			{
				if (s_instance == null)
				{
					s_instance = Resources.Load<UiSettings>(SETTINGS_RUNTIME_PATH);
					if (s_instance == null)
					{
						Debug.LogError($"UiSettings could not be loaded from path '{SETTINGS_RUNTIME_PATH}'");
						s_instance = CreateInstance<UiSettings>();
					}
				}
				return s_instance;
			}
		}

		private void OnEnable()
		{
			InitScenesByName();
		}

		public string GetScenePath(string _sceneName)
		{
			if (m_scenesByName.ContainsKey(_sceneName))
			{
				string scenePath = m_scenesByName[_sceneName].ScenePath;
				scenePath = scenePath.Substring("Assets/".Length);
				scenePath = Path.ChangeExtension(scenePath, null);
				return scenePath;
			}
			return m_additionalScenesPath + _sceneName;
		}

		private void InitScenesByName()
		{
			m_scenesByName.Clear();

			if (m_sceneReferences == null)
				return;

			foreach (var sceneReference in m_sceneReferences)
			{
				string name = Path.GetFileNameWithoutExtension(sceneReference.ScenePath);
				if (m_scenesByName.ContainsKey(name))
				{
					Debug.LogError($"Found non-unique scene name for '{name}'");
					continue;
				}
				m_scenesByName.Add(name, sceneReference);
			}
		}


#if UNITY_EDITOR

		public static bool Initialized => AssetDatabase.LoadAssetAtPath<UiSettings>(UiSettings.SETTINGS_EDITOR_PATH) != null;

		public static void Initialize()
		{
			if (Initialized)
				return;

			UiSettings settings = CreateInstance<UiSettings>();

			settings.m_sceneReferences = BuildSettingsUtility.GetBuildSceneReferences();
			settings.m_loadMainSceneOnPlay = settings.m_sceneReferences.Length > 0;

			EditorSave(settings);
		}

		public static string GetEditorScenePath(string _sceneName)
		{
			UiSettings settings = EditorLoad();
			settings.InitScenesByName();
			return "Assets/" + settings.GetScenePath(_sceneName) + ".unity";
		}

		public static UiSettings EditorLoad()
		{
			EnsureFolderExists(SETTINGS_EDITOR_DIR);
			UiSettings settings = AssetDatabase.LoadAssetAtPath<UiSettings>(UiSettings.SETTINGS_EDITOR_PATH);
			if (settings == null)
			{
				settings = CreateInstance<UiSettings>();
				EditorSave(settings);
			}
			return settings;
		}

		public static void EditorSave(UiSettings _settings)
		{
			if (!AssetDatabase.Contains(_settings))
				AssetDatabase.CreateAsset(_settings, SETTINGS_EDITOR_PATH);
			AssetDatabase.SaveAssets();
		}

		private static bool EnsureFolderExists( string _unityPath )
		{
			try
			{
				if (!AssetDatabase.IsValidFolder(_unityPath))
				{
					string[] names = _unityPath.Split('/');
					string parentPath = "";
					string folderToCreate;
					if (names.Length == 0)
						return false;
					else
					{
						folderToCreate = names[names.Length - 1];
						if (names.Length > 1)
						{
							parentPath = _unityPath.Substring(0, _unityPath.Length - folderToCreate.Length - 1);
							if (!AssetDatabase.IsValidFolder(parentPath))
								if (!EnsureFolderExists(parentPath))
									return false;
						}
					}

					if (!string.IsNullOrEmpty(folderToCreate))
						AssetDatabase.CreateFolder(parentPath, folderToCreate);
				}
				return true;
			}
			catch
			{
				return false;
			}
		}
#endif
	}
}