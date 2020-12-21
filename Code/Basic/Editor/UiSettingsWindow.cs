using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit
{
	public class UiToolkitMainSettingsWindow : EditorWindow
	{
		[SerializeField]
		private UiToolkitMainSettings m_settings;

		private SerializedObject m_serializedSettingsObject;
		private Vector2 scrollPos;

		private bool m_firstTimeInit = false;

		private void OnGUI()
		{

			if (!UiToolkitMainSettings.Initialized)
			{
				m_firstTimeInit = true;
				UiToolkitMainSettings.Initialize();
			}

			if (m_firstTimeInit)
			{
				EditorGUILayout.HelpBox(UiToolkitMainSettings.HELP_FIRST_TIME, MessageType.Info);
				GUILayout.Space(UiEditorUtility.LARGE_SPACE_HEIGHT);
			}

			SerializedObject serializedObject = new SerializedObject(this);
			SerializedProperty settingsProp = serializedObject.FindProperty("m_settings");

			UiToolkitMainSettings thisSettings = settingsProp.objectReferenceValue as UiToolkitMainSettings;
			if (thisSettings == null)
			{
				thisSettings = UiToolkitMainSettings.EditorLoad();
				settingsProp.objectReferenceValue = thisSettings;
			}

			serializedObject.ApplyModifiedProperties();
			m_serializedSettingsObject = new SerializedObject(thisSettings);

			GUILayout.BeginVertical();
			scrollPos = GUILayout.BeginScrollView(scrollPos);

			if (m_firstTimeInit)
			{
				EditorGUILayout.HelpBox(UiToolkitMainSettings.HELP_SCENES, MessageType.Info);
			}
			EditorGUILayout.PropertyField(m_serializedSettingsObject.FindProperty("m_sceneReferences"), true);

			if (m_firstTimeInit)
			{
				GUILayout.Space(UiEditorUtility.LARGE_SPACE_HEIGHT);
				EditorGUILayout.HelpBox(UiToolkitMainSettings.HELP_LOAD_MAIN_SCENE_ON_PLAY, MessageType.Info);
			}
			EditorGUILayout.PropertyField(m_serializedSettingsObject.FindProperty("m_loadMainSceneOnPlay"), true);

			if (m_firstTimeInit)
			{
				GUILayout.Space(UiEditorUtility.LARGE_SPACE_HEIGHT);
				EditorGUILayout.HelpBox(UiToolkitMainSettings.HELP_ADDITIONAL_SCENES_PATH, MessageType.Info);
			}
			EditorGUILayout.PropertyField(m_serializedSettingsObject.FindProperty("m_additionalScenesPath"), true);

			if (m_firstTimeInit)
			{
				GUILayout.Space(UiEditorUtility.LARGE_SPACE_HEIGHT);
				EditorGUILayout.HelpBox(UiToolkitMainSettings.HELP_POT_PATH, MessageType.Info);
			}
			EditorGUILayout.PropertyField(m_serializedSettingsObject.FindProperty("m_potPath"), true);

			if (m_firstTimeInit)
			{
				GUILayout.Space(UiEditorUtility.LARGE_SPACE_HEIGHT);
				EditorGUILayout.HelpBox(UiToolkitMainSettings.HELP_LOCA_PLURALS_DIR, MessageType.Info);
			}
			EditorGUILayout.PropertyField(m_serializedSettingsObject.FindProperty("m_locaPluralsDir"), true);

			m_serializedSettingsObject.ApplyModifiedProperties();

			if (FindObjectOfType<UiMain>() == null)
			{
				GUILayout.Space(UiEditorUtility.LARGE_SPACE_HEIGHT);
				if (m_firstTimeInit)
					EditorGUILayout.HelpBox(UiToolkitMainSettings.HELP_UI_MAIN, MessageType.Info);

				if (GUILayout.Button(new GUIContent("Create UiMain in active scene", UiToolkitMainSettings.HELP_UI_MAIN)))
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
		public static UiToolkitMainSettingsWindow GetWindow()
		{
			var window = GetWindow<UiToolkitMainSettingsWindow>();
			window.titleContent = new GUIContent("Ui Toolkit Main Settings");
			window.Focus();
			window.Repaint();
			return window;
		}
	}
}