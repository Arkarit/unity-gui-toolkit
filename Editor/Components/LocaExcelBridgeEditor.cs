using System;
using System.Collections.Generic;
using System.IO;
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
		private SerializedProperty m_standalone;
		private SerializedProperty m_columnDescriptions;
		private SerializedProperty m_startRow;
		private SerializedProperty m_processedLoca;

		// Cached result from FindObsoleteInSheets; null = not checked yet.
		private List<(int rowIndex0, string key)> m_obsoleteCache = null;

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
			m_standalone             = serializedObject.FindProperty("m_standalone");
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
			EditorGUILayout.PropertyField(m_standalone);
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

			// -- Process / Clear / Reset -------------------------------------------
			EditorGUILayout.Space(10);
			if (GUILayout.Button("Process"))
				thisLocaProvider.CollectData();

			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button("Clear"))
			{
				bridge.EdClear();
				serializedObject.Update();
			}
			if (GUILayout.Button("Reset"))
			{
				if (EditorUtility.DisplayDialog(
						"Reset LocaExcelBridge",
						"This will permanently clear ALL configuration and translation data on this asset.\nContinue?",
						"Reset", "Cancel"))
				{
					bridge.EdReset();
					serializedObject.Update();
				}
			}
			EditorGUILayout.EndHorizontal();

			// -- Sync (Gettext ↔ Sheets) ----------------------------------------
			EditorGUILayout.Space(EditorGUIUtility.singleLineHeight);
			EditorGUILayout.LabelField("Sync", EditorStyles.boldLabel);

			serializedObject.ApplyModifiedProperties();

			// Standalone tables (asset-level opt-in via m_standalone) have no PO backing — the sheet
			// is the only source of truth. The inspector hides PO-related buttons for them and routes
			// Pull from Sheets to Process(). Only meaningful for GoogleDocs source.
			bool isStandalone = sourceType == SourceType.GoogleDocs && bridge.EdStandalone;

			// Standalone sync needs only a URL — Pull (via CollectData) and Backup both work against
			// public Sheets without auth. PO-backed sync still requires full Push credentials.
			bool syncEnabled = isStandalone
				? !string.IsNullOrEmpty(bridge.EdGoogleUrl)
				: (sourceType == SourceType.GoogleDocs && bridge.CanPush);

			using (new EditorGUI.DisabledScope(!syncEnabled))
			{
				if (GUILayout.Button("Pull from Sheets"))
				{
					if (isStandalone)
						thisLocaProvider.CollectData();
					else
						LocaGettextSheetsSyncer.PullFromSheets(bridge);
				}
			}
			EditorGUILayout.HelpBox(
				isStandalone
					? "Downloads the current sheet contents into this asset's ProcessedLoca (same as Process)."
					: "Downloads translations from the sheet into local PO files. Sheet values overwrite local translations; empty sheet cells are ignored.",
				syncEnabled ? MessageType.Info : MessageType.Warning);

			if (!isStandalone)
			{
				using (new EditorGUI.DisabledScope(!syncEnabled))
				{
					using (new GUILayout.HorizontalScope())
					{
						if (GUILayout.Button("Push new keys"))
							LocaGettextSheetsSyncer.PushNewKeysToSheets(bridge);
						if (GUILayout.Button("Dry Run", GUILayout.ExpandWidth(false)))
							LocaGettextSheetsSyncer.PushNewKeysToSheets(bridge, _dryRun: true);
					}
				}
				EditorGUILayout.HelpBox(
					"Appends keys from PO files that are not yet in the sheet. Never overwrites existing cells.\nDry Run previews what would be pushed and writes a TSV to ./Temp/.",
					syncEnabled ? MessageType.Info : MessageType.Warning);
			}

			using (new EditorGUI.DisabledScope(!syncEnabled))
			{
				string backupPath   = LocaGettextSheetsSyncer.GetBackupPath(bridge);
				bool   backupExists = backupPath != null && File.Exists(backupPath);

				EditorGUILayout.BeginHorizontal();

				if (GUILayout.Button("Backup Sheets"))
					LocaGettextSheetsSyncer.BackupSheets(bridge);

				using (new EditorGUI.DisabledScope(!backupExists))
				{
					if (GUILayout.Button("Open Backup"))
						System.Diagnostics.Process.Start(backupPath);
				}

				EditorGUILayout.EndHorizontal();
			}
			EditorGUILayout.HelpBox(
				"Downloads the current sheet as a local xlsx backup file (.bak_{name}.xlsx) alongside this asset.",
				syncEnabled ? MessageType.Info : MessageType.Warning);

			// Obsolete key marking — gettext-only.
			if (!isStandalone)
			{
				using (new EditorGUI.DisabledScope(!syncEnabled))
				{
					string countLabel = m_obsoleteCache == null
						? "?"
						: m_obsoleteCache.Count.ToString();
					string markLabel = $"Mark obsolete in Sheets ({countLabel} found)";

					EditorGUILayout.BeginHorizontal();

					if (GUILayout.Button("↺", GUILayout.Width(28)))
					{
						m_obsoleteCache = LocaGettextSheetsSyncer.FindObsoleteInSheets(bridge);
						Repaint();
					}

					bool canMark = syncEnabled && m_obsoleteCache != null && m_obsoleteCache.Count > 0;
					using (new EditorGUI.DisabledScope(!canMark))
					{
						if (GUILayout.Button(markLabel))
						{
							if (m_obsoleteCache == null)
								m_obsoleteCache = LocaGettextSheetsSyncer.FindObsoleteInSheets(bridge);

							if (m_obsoleteCache != null && m_obsoleteCache.Count > 0 &&
								EditorUtility.DisplayDialog(
									"Mark obsolete in Sheets",
									$"Apply pale-red background and 'Obsolete' note to {m_obsoleteCache.Count} key(s) in the sheet?",
									"Mark", "Cancel"))
							{
								LocaGettextSheetsSyncer.MarkObsoleteInSheets(bridge, m_obsoleteCache);
								m_obsoleteCache = null;
							}
						}
					}

					EditorGUILayout.EndHorizontal();
				}
				EditorGUILayout.HelpBox(
					"↺ refreshes the count. Marks sheet rows whose key no longer exists in any PO file " +
					"with a pale-red background and an 'Obsolete' note.",
					syncEnabled ? MessageType.Info : MessageType.Warning);
			}

			if (!syncEnabled)
			{
				string hint;
				if (isStandalone)
					hint = "Set a Google URL to enable sync.";
				else if (sourceType == SourceType.GoogleDocs)
					hint = "Enable authentication and provide a service account JSON path to use sync.";
				else
					hint = "Sync requires GoogleDocs source type with authentication configured.";
				EditorGUILayout.HelpBox(hint, MessageType.Info);
			}

			// -- Push (legacy) -------------------------------------------------
			// Standalone GoogleDocs tables intentionally have no Push button: there is no editable
			// in-Unity key base to push back. For Local source this path is the CSV exporter.
			if (!isStandalone)
			{
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
			}

			// -- Output --------------------------------------------------------
			EditorGUILayout.Space(EditorGUIUtility.singleLineHeight);

			serializedObject.Update();
			EditorGUILayout.PropertyField(m_processedLoca);
			serializedObject.ApplyModifiedProperties();
		}
	}
}
