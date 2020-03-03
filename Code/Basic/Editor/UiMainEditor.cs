using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GuiToolkit
{

	[CustomEditor(typeof(UiMain))]
	public class UiMainEditor : Editor
	{
		public override void OnInspectorGUI()
		{
			DrawDefaultInspector();

			UiSettings settings = UiSettings.EditorLoad();

			bool playing = Application.isPlaying;

			EditorGUI.BeginDisabledGroup(playing);

			EditorGUILayout.Space();
			GUILayout.Label("General Settings", EditorStyles.boldLabel);
			GUILayout.Label($"Stored in '{UiSettings.SETTINGS_EDITOR_PATH}'", EditorStyles.miniLabel);

			settings.m_scenesPath = EditorGUILayout.TextField("Scenes Path:", settings.m_scenesPath);
			settings.m_unloadAdditionalScenesOnPlay = EditorGUILayout.Toggle("Unload additional scenes on editor play:", settings.m_unloadAdditionalScenesOnPlay);

			if (!playing)
				UiSettings.EditorSave(settings);

			EditorGUI.EndDisabledGroup();
		}
	}
}