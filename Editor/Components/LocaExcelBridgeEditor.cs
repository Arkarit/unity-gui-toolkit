using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static GuiToolkit.LocaExcelBridge;

namespace GuiToolkit.Editor
{
	/// <summary>
	/// Custom inspector for <see cref="LocaExcelBridge"/> ScriptableObjects.
	/// Displays configuration fields and provides a "Process" button to trigger <see cref="ILocaProvider.CollectData"/>,
	/// and a "Push to Spreadsheet" button to write local PO translations back to the source.
	/// </summary>
	[CustomEditor(typeof(LocaExcelBridge), true)]
	public class LocaExcelBridgeEditor : UnityEditor.Editor
	{
		private SerializedProperty m_sourceType;
		private SerializedProperty m_excelPath;
		private SerializedProperty m_googleUrl;
		private SerializedProperty m_useGoogleAuth;
		private SerializedProperty m_serviceAccountJsonPath;
		private SerializedProperty m_group;
		private SerializedProperty m_columnDescriptions;
		private SerializedProperty m_startRow;
		private SerializedProperty m_processedLoca;

		/// <summary>
		/// Caches SerializedProperty references for efficient inspector rendering.
		/// </summary>
		protected void OnEnable()
		{
			m_sourceType             = serializedObject.FindProperty("m_sourceType");
			m_excelPath              = serializedObject.FindProperty("m_excelPath");
			m_googleUrl              = serializedObject.FindProperty("m_googleUrl");
			m_useGoogleAuth          = serializedObject.FindProperty("m_useGoogleAuth");
			m_serviceAccountJsonPath = serializedObject.FindProperty("m_serviceAccountJsonPath");
			m_group                  = serializedObject.FindProperty("m_group");
			m_columnDescriptions     = serializedObject.FindProperty("m_columnDescriptions");
			m_startRow               = serializedObject.FindProperty("m_startRow");
			m_processedLoca          = serializedObject.FindProperty("m_processedLoca");
		}

		/// <summary>
		/// Renders the custom inspector GUI.
		/// Shows source type, Excel path or Google URL (with optional auth fields), group,
		/// column descriptions, start row, a "Process" button, Gettext–Sheets sync buttons,
		/// a "Push to Spreadsheet" button, and the resulting ProcessedLoca output.
		/// </summary>
		public override void OnInspectorGUI()
		{
			var bridge           = (LocaExcelBridge)target;
			var thisLocaProvider = (ILocaProvider)target;
			serializedObject.Update();

			EditorGUILayout.PropertyField(m_sourceType);
			var sourceType = (SourceType)m_sourceType.intValue;

			switch (sourceType)
			{
				case SourceType.Local:
					EditorGUILayout.PropertyField(m_excelPath);
					break;
				case SourceType.GoogleDocs:
					EditorGUILayout.PropertyField(m_googleUrl);
					EditorGUILayout.PropertyField(m_useGoogleAuth);
					if (m_useGoogleAuth.boolValue)
						EditorGUILayout.PropertyField(m_serviceAccountJsonPath);
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			EditorGUILayout.PropertyField(m_group);
			EditorGUILayout.PropertyField(m_columnDescriptions);
			EditorGUILayout.PropertyField(m_startRow);

			// -- Columns -------------------------------------------------------
			EditorGUILayout.Space(EditorGUIUtility.singleLineHeight);
			EditorGUILayout.LabelField("Columns", EditorStyles.boldLabel);

			if (GUILayout.Button("Create by PO"))
				LocaGettextSheetsSyncer.SyncColumnsFromPo(bridge);

			EditorGUILayout.HelpBox(
				"Builds column configuration from the PO files on disk for this group.",
				MessageType.Info);

			// -- Process -------------------------------------------------------
			EditorGUILayout.Space(10);
			if (GUILayout.Button("Process"))
				thisLocaProvider.CollectData();

			// -- Sync (Gettext ↔ Sheets) ----------------------------------------
			EditorGUILayout.Space(EditorGUIUtility.singleLineHeight);
			EditorGUILayout.LabelField("Sync", EditorStyles.boldLabel);

			serializedObject.ApplyModifiedProperties();

			bool syncEnabled = sourceType == SourceType.GoogleDocs && bridge.CanPush;
			using (new EditorGUI.DisabledScope(!syncEnabled))
			{
				if (GUILayout.Button("Pull from Sheets"))
					LocaGettextSheetsSyncer.PullFromSheets(bridge);
			}
			EditorGUILayout.HelpBox(
				"Downloads translations from the sheet into local PO files. Existing translations are not overwritten.",
				syncEnabled ? MessageType.Info : MessageType.Warning);

			using (new EditorGUI.DisabledScope(!syncEnabled))
			{
				if (GUILayout.Button("Push new keys"))
					LocaGettextSheetsSyncer.PushToSheets(bridge);
			}
			EditorGUILayout.HelpBox(
				"Appends keys from PO files that are not yet in the sheet. Never overwrites existing cells.",
				syncEnabled ? MessageType.Info : MessageType.Warning);

			if (!syncEnabled)
			{
				string hint = sourceType == SourceType.GoogleDocs
					? "Enable authentication and provide a service account JSON path to use sync."
					: "Sync requires GoogleDocs source type with authentication configured.";
				EditorGUILayout.HelpBox(hint, MessageType.Info);
			}

			// -- Push (legacy) -------------------------------------------------
			EditorGUILayout.Space(EditorGUIUtility.singleLineHeight);
			EditorGUILayout.LabelField("Push", EditorStyles.boldLabel);

			using (new EditorGUI.DisabledScope(!bridge.CanPush))
			{
				if (GUILayout.Button("Push to Spreadsheet"))
				{
					bool confirmed = EditorUtility.DisplayDialog(
						"Push to Spreadsheet",
						"This will overwrite spreadsheet data with local translations.\nContinue?",
						"Push", "Cancel");

					if (confirmed)
						bridge.PushToSpreadsheet();
				}
			}

			if (!bridge.CanPush)
			{
				string hint = sourceType == SourceType.GoogleDocs
					? "Enable authentication and provide a service account JSON path to push."
					: "Configure a local Excel path to export a CSV.";
				EditorGUILayout.HelpBox(hint, MessageType.Info);
			}
			else
			{
				EditorGUILayout.HelpBox(
					"This will overwrite spreadsheet data with local translations.",
					MessageType.Warning);
			}

			// -- Output --------------------------------------------------------
			EditorGUILayout.Space(EditorGUIUtility.singleLineHeight);

			serializedObject.Update();
			EditorGUILayout.PropertyField(m_processedLoca);
			serializedObject.ApplyModifiedProperties();
		}
	}
}
