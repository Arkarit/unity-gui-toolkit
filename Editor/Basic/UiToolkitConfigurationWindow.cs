using System.IO;
using GuiToolkit.Style;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Editor
{
	public class UiToolkitConfigurationWindow : EditorWindow
	{
		[SerializeField]
		private UiToolkitConfiguration m_settings;

		private SerializedObject m_serializedSettingsObject;
		private Vector2 scrollPos;

		private bool m_firstTimeInit = false;

		private void OnGUI()
		{

			if (!UiToolkitConfiguration.Initialized)
			{
				m_firstTimeInit = true;
				UiToolkitConfiguration.Initialize();
			}

			if (m_firstTimeInit)
			{
				EditorGUILayout.HelpBox(UiToolkitConfiguration.HELP_FIRST_TIME, MessageType.Info);
				GUILayout.Space(EditorUiUtility.LARGE_SPACE_HEIGHT);
			}

			SerializedObject serializedObject = new SerializedObject(this);
			SerializedProperty settingsProp = serializedObject.FindProperty("m_settings");

			UiToolkitConfiguration thisSettings = settingsProp.objectReferenceValue as UiToolkitConfiguration;
			if (thisSettings == null)
			{
				thisSettings = UiToolkitConfiguration.Instance;
				settingsProp.objectReferenceValue = thisSettings;
			}

			serializedObject.ApplyModifiedProperties();
			m_serializedSettingsObject = new SerializedObject(thisSettings);

			GUILayout.BeginVertical();
			scrollPos = GUILayout.BeginScrollView(scrollPos);

			if (m_firstTimeInit)
			{
				EditorGUILayout.HelpBox(UiToolkitConfiguration.HELP_SCENES, MessageType.Info);
			}
			EditorGUILayout.PropertyField(m_serializedSettingsObject.FindProperty("m_sceneReferences"), true);

			if (m_firstTimeInit)
			{
				GUILayout.Space(EditorUiUtility.LARGE_SPACE_HEIGHT);
				EditorGUILayout.HelpBox(UiToolkitConfiguration.HELP_LOAD_MAIN_SCENE_ON_PLAY, MessageType.Info);
			}
			EditorGUILayout.PropertyField(m_serializedSettingsObject.FindProperty("m_loadMainSceneOnPlay"), true);

			if (m_firstTimeInit)
			{
				GUILayout.Space(EditorUiUtility.LARGE_SPACE_HEIGHT);
				EditorGUILayout.HelpBox(UiToolkitConfiguration.HELP_LOAD_VIEW_IN_EVERY_SCENE, MessageType.Info);
			}
			
			var loadViewInEveryScene = m_serializedSettingsObject.FindProperty("m_loadViewInEveryScene");
			EditorGUILayout.PropertyField(loadViewInEveryScene, true);
			if (loadViewInEveryScene.boolValue)
			{
				EditorGUI.indentLevel++;
				if (m_firstTimeInit)
					EditorGUILayout.HelpBox(UiToolkitConfiguration.HELP_LOAD_VIEW_IN_EVERY_SCENE_EXCEPT_UI_MAIN_EXISTS, MessageType.Info);
				EditorGUILayout.PropertyField(m_serializedSettingsObject.FindProperty("m_exceptUiMainExists"), true);
				if (m_firstTimeInit)
					EditorGUILayout.HelpBox(UiToolkitConfiguration.HELP_LOAD_VIEW_IN_EVERY_SCENE_UI_MAIN_PREFAB, MessageType.Info);
				EditorGUILayout.PropertyField(m_serializedSettingsObject.FindProperty("m_uiMainPrefab"), true);
				if (m_firstTimeInit)
					EditorGUILayout.HelpBox(UiToolkitConfiguration.HELP_LOAD_VIEW_IN_EVERY_SCENE_UI_VIEW_PREFAB, MessageType.Info);
				EditorGUILayout.PropertyField(m_serializedSettingsObject.FindProperty("m_uiViewPrefab"), true);
				EditorGUI.indentLevel--;
			}

			if (m_firstTimeInit)
			{
				GUILayout.Space(EditorUiUtility.LARGE_SPACE_HEIGHT);
				EditorGUILayout.HelpBox(UiToolkitConfiguration.HELP_ADDITIONAL_SCENES_PATH, MessageType.Info);
			}
			EditorGUILayout.PropertyField(m_serializedSettingsObject.FindProperty("m_additionalScenesPath"), true);

			if (m_firstTimeInit)
			{
				GUILayout.Space(EditorUiUtility.LARGE_SPACE_HEIGHT);
				EditorGUILayout.HelpBox(UiToolkitConfiguration.HELP_STYLE_CONFIG, MessageType.Info);
			}

			HandleStyleConfig<UiMainStyleConfig>("m_uiMainStyleConfig");
			HandleStyleConfig<UiOrientationDependentStyleConfig>("m_uiOrientationDependentStyleConfig");

			if (m_firstTimeInit)
			{
				GUILayout.Space(EditorUiUtility.LARGE_SPACE_HEIGHT);
				EditorGUILayout.HelpBox(UiToolkitConfiguration.HELP_POT_PATH, MessageType.Info);
			}
			EditorGUILayout.PropertyField(m_serializedSettingsObject.FindProperty("m_potPath"), true);

			if (m_firstTimeInit)
			{
				GUILayout.Space(EditorUiUtility.LARGE_SPACE_HEIGHT);
				EditorGUILayout.HelpBox(UiToolkitConfiguration.HELP_GENERATED_ASSETS_DIR, MessageType.Info);
			}
			EditorGUILayout.PropertyField(m_serializedSettingsObject.FindProperty("m_generatedAssetsDir"), true);

			GUILayout.Space(EditorUiUtility.LARGE_SPACE_HEIGHT);
			EditorGUILayout.PropertyField(m_serializedSettingsObject.FindProperty("m_debugLoca"));

			GUILayout.Space(EditorUiUtility.LARGE_SPACE_HEIGHT);
			EditorGUILayout.PropertyField(m_serializedSettingsObject.FindProperty("m_globalCanvasScalerTemplate"));

			m_serializedSettingsObject.ApplyModifiedProperties();

			if (FindObjectOfType<UiMain>() == null)
			{
				GUILayout.Space(EditorUiUtility.LARGE_SPACE_HEIGHT);
				if (m_firstTimeInit)
					EditorGUILayout.HelpBox(UiToolkitConfiguration.HELP_UI_MAIN, MessageType.Info);

				if (GUILayout.Button(new GUIContent("Create UiMain in active scene", UiToolkitConfiguration.HELP_UI_MAIN)))
				{
					string[] guids = AssetDatabase.FindAssets("UiMain t:prefab");
					foreach (string guid in guids)
					{
						GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
						if (go == null)
							continue;

						if (go.GetComponent<UiMain>() == null)
							continue;

						PrefabUtility.InstantiatePrefab(go);
						break;
					}
				}
			}

			GUILayout.EndScrollView ();
			GUILayout.EndVertical();
		}

		private void HandleStyleConfig<T>(string _memberName) where T : UiStyleConfig
		{
			var styleConfigProp = m_serializedSettingsObject.FindProperty(_memberName);
			var currentStyleConfig = styleConfigProp.objectReferenceValue as T;
			if (currentStyleConfig == null)
			{
				currentStyleConfig = FindStyleConfig<T>();
				styleConfigProp.objectReferenceValue = currentStyleConfig;
			}

			bool isDefault = IsDefaultConfig(currentStyleConfig);
			if (isDefault)
			{
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.PropertyField(styleConfigProp);
				if (GUILayout.Button("Clone", GUILayout.Width(100)))
					CloneStyleConfig(ref currentStyleConfig, _memberName);
				EditorGUILayout.EndHorizontal();
				return;
			}

			EditorGUILayout.PropertyField(styleConfigProp);
		}

		private void CloneStyleConfig<T>(ref T currentStyleConfig, string _memberName) where T : UiStyleConfig
		{
			string resourceDir = "Assets/Resources";
			EditorFileUtility.EnsureUnityFolderExists(resourceDir);
			var newConfigPath = $"{resourceDir}/{currentStyleConfig.GetType().Name}.asset";
			if (File.Exists(EditorFileUtility.GetNativePath(newConfigPath)))
				if (!EditorUtility.DisplayDialog("Overwrite Configuration?", $"A config file at '{newConfigPath}' already exists. Should it be overwritten? (Not undoable)", "OK", "Cancel"))
					return;

			currentStyleConfig = Instantiate(currentStyleConfig);
			AssetDatabase.CreateAsset(currentStyleConfig, newConfigPath);
			var styleConfigProp = m_serializedSettingsObject.FindProperty(_memberName);
			styleConfigProp.objectReferenceValue = currentStyleConfig;
			m_serializedSettingsObject.ApplyModifiedProperties();
			UiMainStyleConfig.ResetInstance();
			AssetDatabase.SaveAssets();
		}

		private bool IsDefaultConfig<T>(T currentStyleConfig) where T : UiStyleConfig
		{
			if (currentStyleConfig == null)
				return false;

			var path = AssetDatabase.GetAssetPath(currentStyleConfig);
			return path.StartsWith(UiToolkitConfiguration.Instance.GetUiToolkitRootProjectDir());
		}


		private T FindStyleConfig<T>() where T:UiStyleConfig
		{
			return EditorAssetUtility.FindScriptableObject<T>();
		}

		[MenuItem(StringConstants.CONFIGURATION_MENU_NAME, priority = Constants.SETTINGS_MENU_PRIORITY)]
		public static UiToolkitConfigurationWindow GetWindow()
		{
			var window = GetWindow<UiToolkitConfigurationWindow>();
			window.titleContent = new GUIContent(StringConstants.CONFIGURATION_NAME);
			window.Focus();
			window.Repaint();
			return window;
		}
	}
}