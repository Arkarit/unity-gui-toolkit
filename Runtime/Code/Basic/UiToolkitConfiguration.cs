using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System;
using GuiToolkit.Style;
using UnityEngine.Serialization;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// \file UiToolkitConfiguration.cs
/// \brief Basic toolkit configuration and definitions.
/// 
/// In this file, all common and basic type definitions of the toolkit are collected.
/// Note that this scriptable object is best edited with the UiToolkitConfigurationWindow.
namespace GuiToolkit
{
	/// \brief Basic toolkit configuration and definitions.
	public class UiToolkitConfiguration : AbstractSingletonScriptableObject<UiToolkitConfiguration>
	{
		/// \cond PRIVATE
		public static readonly string HELP_FIRST_TIME =
			  $"It appears that you are using the {StringConstants.TOOLKIT_NAME} for the first time\n"
			+ $"The scriptable object '{EditorPath}' has been created to store your {StringConstants.TOOLKIT_NAME} settings.\n"
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

		public const string HELP_STYLE_CONFIG =
			"Style config for toolkit. You can replace it by your own style config.";

		public const string HELP_STYLE_CONFIG_RESOLUTION_DEPENDENT =
			"Resolution dependent style config for toolkit. You can replace it by your own style config.";

		public const string HELP_DEBUG_LOCA =
			"This switches loca debugging on or off";

		public static readonly string HELP_UI_MAIN =
			  $"Each {StringConstants.TOOLKIT_NAME} needs a UiMain object, which coordinates all Ui tasks.\n"
			+ "With this button you can create it in the active scene."
			;

		public const string HELP_POT_PATH =
			"Project location of the POT file (translation template containing keys, editor only). This is only necessary if you actually use translation.";

		public const string HELP_GENERATED_ASSETS_DIR =
			  "Several assets need to be generated. Choose your directory here for these files.";

		/// \endcond

		/// Scene references.
		[Tooltip(HELP_SCENES)]
		public SceneReference[] m_sceneReferences;

		[Tooltip(HELP_LOAD_MAIN_SCENE_ON_PLAY)]
		public bool m_loadMainSceneOnPlay = false;

		[Tooltip(HELP_ADDITIONAL_SCENES_PATH)]
		public string m_additionalScenesPath = "Scenes/";

		[Tooltip(HELP_STYLE_CONFIG)]
		[FormerlySerializedAs("m_styleConfig")]
		public UiMainStyleConfig m_uiMainStyleConfig;

		[Tooltip(HELP_STYLE_CONFIG_RESOLUTION_DEPENDENT)]
		public UiResolutionDependentStyleConfig m_uiResolutionDependentStyleConfig;

		[Tooltip(HELP_DEBUG_LOCA)]
		public bool m_debugLoca = false;

		private readonly Dictionary<string, SceneReference> m_scenesByName = new Dictionary<string, SceneReference>();
		private string m_rootDir;


		private void OnEnable()
		{
			InitScenesByName();
		}

		public UiMainStyleConfig UiMainStyleConfig => m_uiMainStyleConfig;
		public UiResolutionDependentStyleConfig UiResolutionDependentStyleConfig => m_uiResolutionDependentStyleConfig;

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
					Directory.CreateDirectory(EditorFileUtility.GetApplicationDataDir() + m_generatedAssetsDir);
				}
				catch( Exception e )
				{
					Debug.LogError($"Could not create generated assets dir '{EditorFileUtility.GetApplicationDataDir() + m_generatedAssetsDir}': {e.Message}");
				}
				return m_generatedAssetsDir;
			}
		}

		public string InternalGeneratedAssetsDir
		{
			get
			{
				return EditorFileUtility.GetApplicationDataDir() + "Assets/External/unity-gui-toolkit/Code/Generated/";
			}
		}

		public override void OnEditorInitialize()
		{
			m_sceneReferences = BuildSettingsUtility.GetBuildSceneReferences();
			m_loadMainSceneOnPlay = m_sceneReferences.Length > 0;
		}

		public static string GetProjectScenePath(string _sceneName)
		{
			UiToolkitConfiguration settings = EditorLoad();
			settings.InitScenesByName();
			return "Assets/" + settings.GetScenePath(_sceneName) + ".unity";
		}
		
		public string GetUiToolkitRootProjectDir()
		{
			if (m_rootDir == null)
			{
				string[] guids = AssetDatabase.FindAssets("unity-gui-toolkit t:folder");
				if (guids.Length >= 1)
				{
					m_rootDir = AssetDatabase.GUIDToAssetPath(guids[0]) + "/";
				}
				else
				{
					m_rootDir = "Packages/de.phoenixgrafik.ui-toolkit/Runtime/";
				}
			}
			return m_rootDir;
		}


#endif
	}
}