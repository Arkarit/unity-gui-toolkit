using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit
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
				GUILayout.Space(UiEditorUtility.LARGE_SPACE_HEIGHT);
			}

			SerializedObject serializedObject = new SerializedObject(this);
			SerializedProperty settingsProp = serializedObject.FindProperty("m_settings");

			UiToolkitConfiguration thisSettings = settingsProp.objectReferenceValue as UiToolkitConfiguration;
			if (thisSettings == null)
			{
				thisSettings = UiToolkitConfiguration.EditorLoad();
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
				GUILayout.Space(UiEditorUtility.LARGE_SPACE_HEIGHT);
				EditorGUILayout.HelpBox(UiToolkitConfiguration.HELP_LOAD_MAIN_SCENE_ON_PLAY, MessageType.Info);
			}
			EditorGUILayout.PropertyField(m_serializedSettingsObject.FindProperty("m_loadMainSceneOnPlay"), true);

			if (m_firstTimeInit)
			{
				GUILayout.Space(UiEditorUtility.LARGE_SPACE_HEIGHT);
				EditorGUILayout.HelpBox(UiToolkitConfiguration.HELP_ADDITIONAL_SCENES_PATH, MessageType.Info);
			}
			EditorGUILayout.PropertyField(m_serializedSettingsObject.FindProperty("m_additionalScenesPath"), true);

			if (m_firstTimeInit)
			{
				GUILayout.Space(UiEditorUtility.LARGE_SPACE_HEIGHT);
				EditorGUILayout.HelpBox(UiToolkitConfiguration.HELP_POT_PATH, MessageType.Info);
			}
			EditorGUILayout.PropertyField(m_serializedSettingsObject.FindProperty("m_potPath"), true);

			if (m_firstTimeInit)
			{
				GUILayout.Space(UiEditorUtility.LARGE_SPACE_HEIGHT);
				EditorGUILayout.HelpBox(UiToolkitConfiguration.HELP_GENERATED_ASSETS_DIR, MessageType.Info);
			}
			EditorGUILayout.PropertyField(m_serializedSettingsObject.FindProperty("m_generatedAssetsDir"), true);

			m_serializedSettingsObject.ApplyModifiedProperties();

			if (FindObjectOfType<UiMain>() == null)
			{
				GUILayout.Space(UiEditorUtility.LARGE_SPACE_HEIGHT);
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