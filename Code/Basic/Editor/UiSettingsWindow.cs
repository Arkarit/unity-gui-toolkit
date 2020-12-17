using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit
{
	public class UiSettingsWindow : EditorWindow
	{
		[SerializeField]
		private UiSettings m_settings;

		private SerializedObject m_serializedSettingsObject;
		private Vector2 scrollPos;

		private bool m_firstTimeInit = false;

		private void OnGUI()
		{

			if (!UiSettings.Initialized)
			{
				m_firstTimeInit = true;
				UiSettings.Initialize();
			}

			if (m_firstTimeInit)
			{
				EditorGUILayout.HelpBox(UiSettings.HELP_FIRST_TIME, MessageType.Info);
				GUILayout.Space(UiEditorUtility.LARGE_SPACE_HEIGHT);
			}

			SerializedObject serializedObject = new SerializedObject(this);
			SerializedProperty settingsProp = serializedObject.FindProperty("m_settings");

			UiSettings thisSettings = settingsProp.objectReferenceValue as UiSettings;
			if (thisSettings == null)
			{
				thisSettings = UiSettings.EditorLoad();
				settingsProp.objectReferenceValue = thisSettings;
			}

			serializedObject.ApplyModifiedProperties();
			m_serializedSettingsObject = new SerializedObject(thisSettings);

			GUILayout.BeginVertical();
			scrollPos = GUILayout.BeginScrollView(scrollPos);

			if (m_firstTimeInit)
			{
				EditorGUILayout.HelpBox(UiSettings.HELP_SCENES, MessageType.Info);
			}
			EditorGUILayout.PropertyField(m_serializedSettingsObject.FindProperty("m_sceneReferences"), true);

			if (m_firstTimeInit)
			{
				GUILayout.Space(UiEditorUtility.LARGE_SPACE_HEIGHT);
				EditorGUILayout.HelpBox(UiSettings.HELP_LOAD_MAIN_SCENE_ON_PLAY, MessageType.Info);
			}
			EditorGUILayout.PropertyField(m_serializedSettingsObject.FindProperty("m_loadMainSceneOnPlay"), true);

			if (m_firstTimeInit)
			{
				GUILayout.Space(UiEditorUtility.LARGE_SPACE_HEIGHT);
				EditorGUILayout.HelpBox(UiSettings.HELP_ADDITIONAL_SCENES_PATH, MessageType.Info);
			}
			EditorGUILayout.PropertyField(m_serializedSettingsObject.FindProperty("m_additionalScenesPath"), true);

			if (m_firstTimeInit)
			{
				GUILayout.Space(UiEditorUtility.LARGE_SPACE_HEIGHT);
				EditorGUILayout.HelpBox(UiSettings.HELP_POT_PATH, MessageType.Info);
			}
			EditorGUILayout.PropertyField(m_serializedSettingsObject.FindProperty("m_potPath"), true);

			m_serializedSettingsObject.ApplyModifiedProperties();

			if (FindObjectOfType<UiMain>() == null)
			{
				GUILayout.Space(UiEditorUtility.LARGE_SPACE_HEIGHT);
				if (m_firstTimeInit)
					EditorGUILayout.HelpBox(UiSettings.HELP_UI_MAIN, MessageType.Info);

				if (GUILayout.Button(new GUIContent("Create UiMain in active scene", UiSettings.HELP_UI_MAIN)))
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

		[MenuItem(StringConstants.SETTINGS_MENU_NAME, priority = Constants.SETTINGS_MENU_PRIORITY)]
		public static UiSettingsWindow GetWindow()
		{
			var window = GetWindow<UiSettingsWindow>();
			window.titleContent = new GUIContent("UI Settings");
			window.Focus();
			window.Repaint();
			return window;
		}
	}
}