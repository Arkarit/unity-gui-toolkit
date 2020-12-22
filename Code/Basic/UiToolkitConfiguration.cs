using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GuiToolkit
{
	public class UiToolkitConfiguration : ScriptableObject
	{
		public const string FILENAME = "UiToolkitConfiguration";
		public const string RUNTIME_DIR = "";
		public const string RUNTIME_PATH = FILENAME;
		public const string EDITOR_DIR = "Assets/Resources" + RUNTIME_DIR;
		public const string EDITOR_PATH = EDITOR_DIR + "/" + FILENAME + ".asset";

		public static readonly string HELP_FIRST_TIME =
			  $"It appears that you are using the {StringConstants.TOOLKIT_NAME} for the first time\n"
			+ $"The scriptable object '{EDITOR_PATH}' has been created to store your {StringConstants.TOOLKIT_NAME} settings.\n"
			+ $"Please be sure to check it in to your code versioning system!\n\n"
			+ $"You can always access this window from the menu: '{StringConstants.CONFIGURATION_MENU_NAME}'\n\n"
			+ $"Please check the settings below to create the initial setup for {StringConstants.TOOLKIT_NAME}:"
			;

		public const string HELP_SCENES =
			  "A shortcut to the Unity 'File/Build Settings/Scenes in Build' field\n"
			+ "You can add scenes to the build, enable or disable them or remove from build.\n"
			+ "However, the main purpose of this field is to be a convenient scene loader.\n"
			+ "By setting/clearing the checkmark on 'Loaded in Editor' you can very simply load/unload scenes in the editor additively.\n"
			+ "By setting the checkmark together with the shift key, cou can load a scene exclusively."
			;

		public const string HELP_LOAD_MAIN_SCENE_ON_PLAY =
			  "When enabled, the main scene is loaded when you press play in the editor, and all other scenes are unloaded.\n" 
			+ "After play, the scenes, which were previously loaded, are restored."
			;

		public const string HELP_ADDITIONAL_SCENES_PATH =
			"Additional scene path for scenes, which are not in the scene references list";

		public static readonly string HELP_UI_MAIN =
			  $"Each {StringConstants.TOOLKIT_NAME} needs a UiMain object, which coordinates all Ui tasks.\n"
			+ "With this button you can create it in the active scene."
			;

		public const string HELP_POT_PATH =
			"Project location of the POT file (translation template containing keys, editor only). This is only necessary if you actually use translation.";

		// Note: can not make this access the string constants because of shitty C# / Tooltip rules
		public const string HELP_GENERATED_ASSETS_DIR =
			  "Several assets need to be generated. Choose your directory here for these files.";

		[Tooltip(HELP_SCENES)]
		public SceneReference[] m_sceneReferences;

		[Tooltip(HELP_LOAD_MAIN_SCENE_ON_PLAY)]
		public bool m_loadMainSceneOnPlay = false;

		[Tooltip(HELP_ADDITIONAL_SCENES_PATH)]
		public string m_additionalScenesPath = "Scenes/";

		private readonly Dictionary<string, SceneReference> m_scenesByName = new Dictionary<string, SceneReference>();

		private static UiToolkitConfiguration s_instance;

		public static UiToolkitConfiguration Instance
		{
			get
			{
				if (s_instance == null)
				{
					s_instance = Resources.Load<UiToolkitConfiguration>(RUNTIME_PATH);
					if (s_instance == null)
					{
						Debug.LogError($"UiToolkitMainSettings could not be loaded from path '{RUNTIME_PATH}'");
						s_instance = CreateInstance<UiToolkitConfiguration>();
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

		[Tooltip(HELP_POT_PATH)]
		public string m_potPath;

		[Tooltip(HELP_GENERATED_ASSETS_DIR)]
		[SerializeField]
		protected string m_generatedAssetsDir = "Assets/";

		public string GeneratedAssetsDir
		{
			get
			{
				try
				{
					Directory.CreateDirectory(UiEditorUtility.GetApplicationDataDir() + m_generatedAssetsDir);
				}
				catch( Exception e )
				{
					Debug.LogError($"Could not create generated assets dir '{UiEditorUtility.GetApplicationDataDir() + m_generatedAssetsDir}': {e.Message}");
				}
				return m_generatedAssetsDir;
			}
		}

		public static bool Initialized => AssetDatabase.LoadAssetAtPath<UiToolkitConfiguration>(UiToolkitConfiguration.EDITOR_PATH) != null;

		public static void Initialize()
		{
			if (Initialized)
				return;

			UiToolkitConfiguration settings = CreateInstance<UiToolkitConfiguration>();

			settings.m_sceneReferences = BuildSettingsUtility.GetBuildSceneReferences();
			settings.m_loadMainSceneOnPlay = settings.m_sceneReferences.Length > 0;

			EditorSave(settings);
		}

		public static string GetProjectScenePath(string _sceneName)
		{
			UiToolkitConfiguration settings = EditorLoad();
			settings.InitScenesByName();
			return "Assets/" + settings.GetScenePath(_sceneName) + ".unity";
		}

		public static UiToolkitConfiguration EditorLoad()
		{
			EnsureFolderExists(EDITOR_DIR);
			UiToolkitConfiguration settings = AssetDatabase.LoadAssetAtPath<UiToolkitConfiguration>(UiToolkitConfiguration.EDITOR_PATH);
			if (settings == null)
			{
				settings = CreateInstance<UiToolkitConfiguration>();
				EditorSave(settings);
			}
			return settings;
		}

		public static void EditorSave(UiToolkitConfiguration _settings)
		{
			if (!AssetDatabase.Contains(_settings))
				AssetDatabase.CreateAsset(_settings, EDITOR_PATH);
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