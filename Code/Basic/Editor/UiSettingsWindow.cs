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
				EditorGUILayout.HelpBox(StringConstants.SETTINGS_HELP_FIRST_TIME, MessageType.Info);
				GUILayout.Space(UiEditorUtility.LARGE_SPACE_HEIGHT);
			}

			SerializedObject serializedObject = new SerializedObject(this);
			SerializedProperty settingsProp = serializedObject.FindProperty("m_settings");

			UiSettings settings = settingsProp.objectReferenceValue as UiSettings;
			if (settings == null)
			{
				settings = UiSettings.EditorLoad();
				settingsProp.objectReferenceValue = settings;
			}

			serializedObject.ApplyModifiedProperties();
			m_serializedSettingsObject = new SerializedObject(settings);


			GUILayout.BeginVertical();
			scrollPos = GUILayout.BeginScrollView(scrollPos);

			m_serializedSettingsObject.DisplayProperties();
			m_serializedSettingsObject.ApplyModifiedProperties();

			GUILayout.EndScrollView ();
			GUILayout.EndVertical();
		}

		[MenuItem(StringConstants.SETTINGS_MENU_NAME)]
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