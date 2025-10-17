using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System;
using GuiToolkit.AssetHandling;
using GuiToolkit.Style;
using UnityEngine.Serialization;
using UnityEngine.UI;

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
			+ $"The scriptable object '{nameof(UiToolkitConfiguration)}' has been created to store your {StringConstants.TOOLKIT_NAME} settings.\n"
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

		public const string HELP_LOAD_VIEW_IN_EVERY_SCENE =
			  "When a scene is loaded in editor, which is not the main scene, a view can be automatically loaded (e.g. a HUD).\n" 
			+ "This is useful for editing scenes; you can start the scene directly and still have your HUD."
			;

		public const string HELP_LOAD_VIEW_IN_EVERY_SCENE_EXCEPT_UI_MAIN_EXISTS =
			  "The loaded UiView needs a UiMain, which is automatically created in the loaded scene.\n" 
			+ "If checked, this UiMain is NOT created and the UiView is NOT created, if an UiMain already exists\n"
			+ "This is useful to avoid duplication in your main scene (e.g. HUD)"
			;

		public const string HELP_LOAD_VIEW_IN_EVERY_SCENE_UI_MAIN_PREFAB =
			  "The prefab for the automatically created UiMain"
			;

		public const string HELP_LOAD_VIEW_IN_EVERY_SCENE_UI_VIEW_PREFAB =
			  "Your UiView to load"
			;


		public const string HELP_ADDITIONAL_SCENES_PATH =
			"Additional scene path for scenes, which are not in the scene references list";
		
		public const string HELP_PREFAB_VARIANTS_PATH =
			"Path for prefab variants of the builtin prefabs";

		public const string HELP_STYLE_CONFIG =
			"Style config for toolkit. You can replace it by your own style config.";

		public const string HELP_STYLE_CONFIG_RESOLUTION_DEPENDENT =
			"Resolution dependent style config for toolkit. You can replace it by your own style config.";

		public const string HELP_DEBUG_LOCA =
			"This switches loca debugging on or off";

		public const string HELP_GLOBAL_CANVAS_SCALER_TEMPLATE =
			"An optional global canvas scaler template, which is applied to every UiView";

		public static readonly string HELP_UI_MAIN =
			  $"Each {StringConstants.TOOLKIT_NAME} needs a UiMain object, which coordinates all Ui tasks.\n"
			+ "With this button you can create it in the active scene."
			;

		public const string HELP_POT_PATH =
			"Project location of the POT file (translation template containing keys, editor only). This is only necessary if you actually use translation.";

		public const string HELP_GENERATED_ASSETS_DIR =
			  "Several assets need to be generated. Choose your directory here for these files.";

		public const string HELP_ASSET_PROVIDER_FACTORY = "An Optional Asset Provider Factory in case you want to use Addressables.";
		
		public const string HELP_TRANSITION_OVERLAY = "A transition overlay, which can cover the screen during level changes etc.";


		/// \endcond

		/// Scene references.
		[Tooltip(HELP_SCENES)]
		[SerializeField] private SceneReference[] m_sceneReferences;

		[Tooltip(HELP_LOAD_VIEW_IN_EVERY_SCENE)]
		[SerializeField] private bool m_loadViewInEveryScene = false;
		[Tooltip(HELP_LOAD_VIEW_IN_EVERY_SCENE_EXCEPT_UI_MAIN_EXISTS)]
		[SerializeField] private bool m_exceptUiMainExists = true;
		[Tooltip(HELP_LOAD_VIEW_IN_EVERY_SCENE_UI_MAIN_PREFAB)]
		[SerializeField, Mandatory] private UiMain m_uiMainPrefab;
		[Tooltip(HELP_LOAD_VIEW_IN_EVERY_SCENE_UI_VIEW_PREFAB)]
		[SerializeField, Mandatory] private UiView m_uiViewPrefab;

		[Tooltip(HELP_ADDITIONAL_SCENES_PATH)]
		[SerializeField] private string m_additionalScenesPath = "Assets/Scenes/";

		[Tooltip(HELP_PREFAB_VARIANTS_PATH)]
		[SerializeField] private string m_prefabVariantsPath = "Assets/Prefabs/PrefabVariants/";

		[Tooltip(HELP_STYLE_CONFIG)]
		[FormerlySerializedAs("m_styleConfig")]
		[SerializeField] private UiMainStyleConfig m_uiMainStyleConfig;

		[Tooltip(HELP_STYLE_CONFIG_RESOLUTION_DEPENDENT)]
		[SerializeField] private UiOrientationDependentStyleConfig m_uiOrientationDependentStyleConfig;
		
		[Tooltip(HELP_DEBUG_LOCA)]
		[SerializeField] private bool m_debugLoca = false;
		
		[Tooltip(HELP_GLOBAL_CANVAS_SCALER_TEMPLATE)]
		[SerializeField, Optional] private CanvasScaler m_globalCanvasScalerTemplate = null;
		
		[Tooltip(HELP_ASSET_PROVIDER_FACTORY)]
		[SerializeField, Optional] private AbstractAssetProviderFactory[] m_assetProviderFactories = new AbstractAssetProviderFactory[0];
		
		[Tooltip(HELP_TRANSITION_OVERLAY)]
		[SerializeField, Optional] private UiTransitionOverlay m_transitionOverlay = null;

		private readonly Dictionary<string, SceneReference> m_scenesByName = new Dictionary<string, SceneReference>();
		private string m_rootDir;


		private void OnEnable()
		{
			InitScenesByName();
			if (m_uiMainStyleConfig != null)
				UiMainStyleConfig.Instance = m_uiMainStyleConfig;
			if (m_uiOrientationDependentStyleConfig != null)
				UiOrientationDependentStyleConfig.Instance = m_uiOrientationDependentStyleConfig;
		}

		public UiMainStyleConfig UiMainStyleConfig => m_uiMainStyleConfig;
		public UiOrientationDependentStyleConfig UiOrientationDependentStyleConfig => m_uiOrientationDependentStyleConfig;
		public CanvasScaler GlobalCanvasScalerTemplate => m_globalCanvasScalerTemplate;
		public bool DebugLoca => m_debugLoca;
		public bool LoadViewInEveryScene => m_loadViewInEveryScene;
		public UiMain UiMainPrefab => m_uiMainPrefab;
		public UiView UiViewPrefab => m_uiViewPrefab;
		public bool ExceptUiMainExists => m_exceptUiMainExists;
		public AbstractAssetProviderFactory[] AssetProviderFactories => m_assetProviderFactories;
		public UiTransitionOverlay TransitionOverlay => m_transitionOverlay;
		
	
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
					UiLog.LogError($"Found non-unique scene name for '{name}'");
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
					UiLog.LogError($"Could not create generated assets dir '{EditorFileUtility.GetApplicationDataDir() + m_generatedAssetsDir}': {e.Message}");
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

		public override void OnEditorCreatedAsset()
		{
			m_sceneReferences = BuildSettingsUtility.GetBuildSceneReferences();
		}

		public string GetProjectScenePath(string _sceneName)
		{
			InitScenesByName();
			return "Assets/" + GetScenePath(_sceneName) + ".unity";
		}
		
		public string GetUiToolkitRootProjectDir()
		{
			if (string.IsNullOrEmpty(m_rootDir))
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