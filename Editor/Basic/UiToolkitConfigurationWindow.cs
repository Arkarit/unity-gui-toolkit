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
				EditorGUILayout.HelpBox(UiToolkitConfiguration.HELP_ADDITIONAL_SCENES_PATH, MessageType.Info);
			}
			EditorGUILayout.PropertyField(m_serializedSettingsObject.FindProperty("m_additionalScenesPath"), true);

			if (m_firstTimeInit)
			{
				GUILayout.Space(EditorUiUtility.LARGE_SPACE_HEIGHT);
				EditorGUILayout.HelpBox(UiToolkitConfiguration.HELP_STYLE_CONFIG, MessageType.Info);
			}

			HandleStyleConfig();


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

		private void HandleStyleConfig()
		{
			var styleConfigProp = m_serializedSettingsObject.FindProperty("m_styleConfig");
			var currentStyleConfig = styleConfigProp.objectReferenceValue as UiMainStyleConfig;
			if (currentStyleConfig == null)
			{
				currentStyleConfig = FindStyleConfig();
				styleConfigProp.objectReferenceValue = currentStyleConfig;
			}

			bool isDefault = IsDefaultConfig(currentStyleConfig);
			if (isDefault)
			{
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.PropertyField(styleConfigProp);
				if (GUILayout.Button("Clone", GUILayout.Width(100)))
					CloneStyleConfig(ref currentStyleConfig);
				EditorGUILayout.EndHorizontal();
				return;
			}

			EditorGUILayout.PropertyField(styleConfigProp);
		}

		private void CloneStyleConfig(ref UiMainStyleConfig currentStyleConfig)
		{
			string resourceDir = "Assets/Resources";
			EditorFileUtility.EnsureFolderExists(resourceDir);
			var newConfigPath = $"{resourceDir}/{nameof(UiMainStyleConfig)}.asset";
			if (File.Exists(EditorFileUtility.GetNativePath(newConfigPath)))
				if (!EditorUtility.DisplayDialog("Overwrite Configuration?", $"A config file at '{newConfigPath}' already exists. Should it be overwritten? (Not undoable)", "OK", "Cancel"))
					return;

			currentStyleConfig = Instantiate(currentStyleConfig);
			AssetDatabase.CreateAsset(currentStyleConfig, newConfigPath);
			var styleConfigProp = m_serializedSettingsObject.FindProperty("m_styleConfig");
			styleConfigProp.objectReferenceValue = currentStyleConfig;
			m_serializedSettingsObject.ApplyModifiedProperties();
			AssetDatabase.SaveAssets();
		}

		private bool IsDefaultConfig(UiMainStyleConfig currentStyleConfig)
		{
			if (currentStyleConfig == null)
				return false;

			var path = AssetDatabase.GetAssetPath(currentStyleConfig);
			return path.StartsWith(UiToolkitConfiguration.Instance.GetUiToolkitRootProjectDir());
		}


		private UiMainStyleConfig FindStyleConfig() => EditorAssetUtility.FindScriptableObject<UiMainStyleConfig>();

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