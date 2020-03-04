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

		private void OnGUI()
		{
			SerializedObject serializedObject = new SerializedObject(this);
			SerializedProperty settingsProp = serializedObject.FindProperty("m_settings");

			UiSettings settings = settingsProp.objectReferenceValue as UiSettings;
			if (settings == null)
			{
				settings = UiSettings.EditorLoad();
				settingsProp.objectReferenceValue = settings;
			}

			serializedObject.ApplyModifiedProperties();

			serializedObject = new SerializedObject(settings);
			serializedObject.DisplayProperties();
			serializedObject.ApplyModifiedProperties();
		}

		[MenuItem("UI Toolkit/Settings")]
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