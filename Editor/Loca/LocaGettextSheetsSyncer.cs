using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using UnityEditor;
using UnityEngine;
using GuiToolkit;

namespace GuiToolkit.Editor
{
	/// <summary>
	/// Provides synchronisation between local Gettext PO files and a Google Sheets spreadsheet
	/// for a <see cref="LocaExcelBridge"/> asset.
	/// <list type="bullet">
	///   <item><see cref="SyncColumnsFromPo"/> — derives column configuration from PO files on disk.</item>
	///   <item><see cref="PullFromSheets"/> — downloads translations from the sheet and merges them into PO files.</item>
	///   <item><see cref="PushNewKeysToSheets"/> — appends keys that exist in PO files but are missing from the sheet.</item>
	/// </list>
	/// </summary>
	[EditorAware]
	public static class LocaGettextSheetsSyncer
	{
		// Fallback only; overridden at runtime by querying spreadsheet metadata.
		private const string DEFAULT_SHEET_NAME = "Sheet1";

		// -----------------------------------------------------------------------
		// Public API
		// -----------------------------------------------------------------------

		/// <summary>
		/// Builds or updates the column configuration of a <see cref="LocaExcelBridge"/> from the PO files
		/// currently present for its group.
		/// Detects languages and plural-form counts from the existing PO data,
		/// keeps existing matching columns intact, and appends any newly discovered ones.
		/// Prompts the user before making changes when columns already exist.
		/// </summary>
		/// <param name="_bridge">The bridge asset whose column list should be updated.</param>
		public static void SyncColumnsFromPo(LocaExcelBridge _bridge)
		{
			if (_bridge == null)
				return;

			string group = _bridge.EdGroup;
			var poFiles = LocaCsvExporter.FindPoFiles(group);

			if (poFiles.Count == 0)
			{
				EditorUtility.DisplayDialog(
					"Sync Columns from PO",
					$"No PO files found for group '{group}'.",
					"OK");
				return;
			}

			var poData = new List<(string lang, PoFile file)>(poFiles.Count);
			foreach (var (lang, _, filePath) in poFiles)
			{
				string content = File.ReadAllText(filePath, Encoding.UTF8);
				poData.Add((lang, PoFile.Parse(content)));
			}

			var newColumns = BuildColumnsFromPoData(poData);

			// Merge with existing columns, keeping existing entries for matches.
			var existingColumns = _bridge.EdColumnDescriptions;

			if (existingColumns.Count > 0)
			{
				var merged    = new List<LocaExcelBridge.InColumnDescription>(existingColumns);
				var newlyAdded = new List<LocaExcelBridge.InColumnDescription>();

				foreach (var newCol in newColumns)
				{
					bool alreadyExists = existingColumns.Any(ec =>
						ec.ColumnType == newCol.ColumnType &&
						string.Equals(ec.LanguageId, newCol.LanguageId, StringComparison.OrdinalIgnoreCase) &&
						ec.PluralForm == newCol.PluralForm);

					if (!alreadyExists)
					{
						merged.Add(newCol);
						newlyAdded.Add(newCol);
					}
				}

				if (newlyAdded.Count == 0)
				{
					EditorUtility.DisplayDialog(
						"Sync Columns from PO",
						"Column configuration is already up to date.",
						"OK");
					return;
				}

				string addedDesc = string.Join(", ", newlyAdded.Select(c => c.Description));
				bool confirmed = EditorUtility.DisplayDialog(
					"Sync Columns from PO",
					$"Found {newlyAdded.Count} new column(s) to append:\n{addedDesc}\n\n" +
					"Existing columns will be kept.\nContinue?",
					"Add Columns", "Cancel");

				if (!confirmed)
					return;

				_bridge.EdSetColumnDescriptions(merged);
			}
			else
			{
				_bridge.EdSetColumnDescriptions(newColumns);
			}

			AssetDatabase.SaveAssets();
			UiLog.Log($"{nameof(LocaGettextSheetsSyncer)}: Updated columns for '{_bridge.name}' ({_bridge.NumColumns} total).");
		}

		/// <summary>
		/// Downloads translations from the Google Sheet and merges them conservatively into local PO files.
		/// Translations that already have a non-empty value in the PO file are never overwritten.
		/// Creates a backup of every modified PO file before writing.
		/// </summary>
		/// <param name="_bridge">The bridge asset to pull translations from.</param>
		/// <param name="_suppressDialogs">
		/// When <c>true</c>, all user-facing dialogs are suppressed and results are written to the
		/// console instead. Use this for headless/build-preprocessor contexts.
		/// </param>
		public static void PullFromSheets(LocaExcelBridge _bridge, bool _suppressDialogs = false)
		{
			if (_bridge == null)
				return;

			if (_bridge.EdSourceType != LocaExcelBridge.SourceType.GoogleDocs)
			{
				const string msg = "Bridge source type must be GoogleDocs.";
				if (_suppressDialogs)
					UiLog.LogError($"{nameof(LocaGettextSheetsSyncer)} [{_bridge.name}]: {msg}");
				else
					EditorUtility.DisplayDialog("Pull from Sheets", msg, "OK");
				return;
			}

			// Collect data from the spreadsheet.
			bool collectDataFailed = false;
			try
			{
				_bridge.CollectData();
			}
			catch (Exception ex)
			{
				collectDataFailed = true;
				UiLog.LogWarning($"{nameof(LocaGettextSheetsSyncer)} [{_bridge.name}]: CollectData failed ({ex.Message}). Falling back to cached data.");
			}

			var processedLoca = _bridge.Localization;
			if (processedLoca?.Entries == null || processedLoca.Entries.Count == 0)
			{
				string msg = collectDataFailed
					? "CollectData failed and no cached data is available.\n\nNote: Live download requires Unity 6000 or newer. Run 'Pull from Sheets' manually in Unity 6 and commit the bridge asset to cache the data."
					: "No entries found after collecting data. Nothing to pull.";
				if (_suppressDialogs)
					UiLog.LogError($"{nameof(LocaGettextSheetsSyncer)} [{_bridge.name}]: {msg}");
				else
					EditorUtility.DisplayDialog("Pull from Sheets – Error", msg, "OK");
				return;
			}

			if (collectDataFailed)
				UiLog.LogWarning($"{nameof(LocaGettextSheetsSyncer)} [{_bridge.name}]: Using {processedLoca.Entries.Count} cached entries (live download unavailable in this Unity version).");

			// Find the key-column descriptor for affix reversal.
			LocaExcelBridge.InColumnDescription keyColDesc = null;
			for (int i = 0; i < _bridge.NumColumns; i++)
			{
				var d = _bridge.GetColumnDescription(i);
				if (d?.ColumnType == LocaExcelBridge.EInColumnType.Key)
				{
					keyColDesc = d;
					break;
				}
			}

			// Collect unique (lang, keyPrefix, keyPostfix) context groups from the column config.
			var langContextKeys = new List<(string lang, string prefix, string postfix)>();
			var langContextSet  = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

			for (int i = 0; i < _bridge.NumColumns; i++)
			{
				var d = _bridge.GetColumnDescription(i);
				if (d?.ColumnType != LocaExcelBridge.EInColumnType.LanguageTranslation) continue;
				if (string.IsNullOrWhiteSpace(d.LanguageId)) continue;

				string lang       = d.LanguageId.Trim().ToLowerInvariant();
				string contextKey = $"{lang}|{d.KeyPrefix}|{d.KeyPostfix}";
				if (langContextSet.Add(contextKey))
					langContextKeys.Add((lang, d.KeyPrefix ?? string.Empty, d.KeyPostfix ?? string.Empty));
			}

			// Load PO files for this bridge group.
			string group   = _bridge.EdGroup;
			var poFiles = LocaCsvExporter.FindPoFiles(group);

			if (poFiles.Count == 0)
			{
				string msg = $"No PO files found for group '{group}'.";
				if (_suppressDialogs)
					UiLog.LogError($"{nameof(LocaGettextSheetsSyncer)} [{_bridge.name}]: {msg}");
				else
					EditorUtility.DisplayDialog("Pull from Sheets", msg, "OK");
				return;
			}

			var langToFilePath = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			var parsedPoFiles  = new Dictionary<string, PoFile>(StringComparer.OrdinalIgnoreCase);
			var poLookups      = new Dictionary<string, Dictionary<string, PoEntry>>(StringComparer.OrdinalIgnoreCase);

			foreach (var (lang, _, filePath) in poFiles)
			{
				string normLang = lang.Trim().ToLowerInvariant();
				if (!langToFilePath.ContainsKey(normLang))
					langToFilePath[normLang] = filePath;

				if (!parsedPoFiles.ContainsKey(filePath))
				{
					string fileContent = File.ReadAllText(filePath, Encoding.UTF8);
					var poFile = PoFile.Parse(fileContent);
					// Ensure the header is present; old or hand-edited files may have an empty one.
					if (string.IsNullOrEmpty(poFile.HeaderMsgStr))
					{
						poFile.HasHeader    = true;
						poFile.HeaderMsgStr = $"Language: {normLang}\\nContent-Type: text/plain; charset=UTF-8\\nContent-Transfer-Encoding: 8bit\\n";
					}
					parsedPoFiles[filePath] = poFile;
					// Build lookup with CRLF-normalized keys so that sheet keys (always \n)
					// match PO keys that were originally imported with \r\n line endings.
					var rawLookup        = poFile.BuildLookup();
					var normalizedLookup = new Dictionary<string, PoEntry>(StringComparer.Ordinal);
					foreach (var kv in rawLookup)
						normalizedLookup[NormalizeCrlf(kv.Key)] = kv.Value;
					poLookups[filePath] = normalizedLookup;
				}
			}

			// Walk every processed entry and update the corresponding PO entries.
			// Entries not found in the PO are collected for creation as new entries.
			var dirtyFiles   = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			int updatedCount  = 0;
			int createdCount  = 0;
			// Track newly created entries (by composedKey) for the POT update — only the first
			// language that creates a key is recorded; the POT is language-independent.
			var newPotEntries = new Dictionary<string, PoEntry>(StringComparer.Ordinal);

			// Build trimmed-key fallback lookups so that trailing whitespace differences
			// between sheet keys and PO keys don't cause false "new entry" creations.
			var trimmedLookups = new Dictionary<string, Dictionary<string, PoEntry>>(StringComparer.OrdinalIgnoreCase);
			foreach (var kv in poLookups)
			{
				var trimmed = new Dictionary<string, PoEntry>(StringComparer.Ordinal);
				foreach (var pair in kv.Value)
				{
					string trimmedKey = pair.Key.Trim();
					if (!trimmed.ContainsKey(trimmedKey))
						trimmed[trimmedKey] = pair.Value;
				}
				trimmedLookups[kv.Key] = trimmed;
			}

			int totalEntries     = 0;
			int skippedNoLang    = 0;
			int skippedNoFile    = 0;
			int exactMatchCount  = 0;
			int trimmedSkipCount = 0;
			int emptyTextCount   = 0;

			foreach (var entry in processedLoca.Entries)
			{
				totalEntries++;

				if (string.IsNullOrEmpty(entry.LanguageId))
				{
					skippedNoLang++;
					continue;
				}

				string lang = entry.LanguageId.Trim().ToLowerInvariant();
				if (!langToFilePath.TryGetValue(lang, out string filePath))
				{
					skippedNoFile++;
					continue;
				}

				var lookup        = poLookups[filePath];
				var trimmedLookup = trimmedLookups[filePath];

				foreach (var (ctxLang, ctxPrefix, ctxPostfix) in langContextKeys)
				{
					if (!string.Equals(ctxLang, lang, StringComparison.OrdinalIgnoreCase))
						continue;

					string baseEffectiveKey = ReverseKeyAffixes(entry.Key, ctxPrefix, ctxPostfix);
					string rawMsgId         = ReverseKeyAffixes(baseEffectiveKey, keyColDesc);
					string msgctxt          = string.IsNullOrEmpty(ctxPrefix) ? null : ctxPrefix;
					// Normalize CRLF so sheet keys (\n) match PO keys that may have \r\n.
					string composedKey      = NormalizeCrlf(string.IsNullOrEmpty(msgctxt)
						? rawMsgId
						: $"{msgctxt}\u0004{rawMsgId}");

					if (lookup.TryGetValue(composedKey, out var poEntry))
					{
						exactMatchCount++;
						// Exact key match — merge the translation.
						if (MergeTranslationIntoPoEntry(entry, poEntry))
						{
							dirtyFiles.Add(filePath);
							updatedCount++;
						}
					}
					else if (trimmedLookup.TryGetValue(composedKey.Trim(), out _))
					{
						trimmedSkipCount++;
						// Key exists with different whitespace — skip to avoid overwriting
						// intentional trailing spaces/newlines in the PO entry.
					}
					else
					{
						// Determine the effective singular text: Text field (PluralForm=-1 columns)
						// or Forms[0] (PluralForm=0 columns used as singular).
						string singularText = entry.Text;
						if (string.IsNullOrEmpty(singularText) && entry.Forms is { Length: > 0 })
							singularText = entry.Forms[0];

						if (!string.IsNullOrEmpty(singularText))
						{
							// Genuinely new key from sheet — create a singular PO entry.
							UiLog.Log($"[PullFromSheets] NEW key: lang={lang} composedKey=[{composedKey}] text=[{singularText}]");
							var newEntry = new PoEntry
							{
								MsgId   = rawMsgId,
								Context = msgctxt,
								MsgStr  = singularText,
							};

							parsedPoFiles[filePath].Entries.Add(newEntry);
							lookup[composedKey] = newEntry;
							dirtyFiles.Add(filePath);
							createdCount++;
							if (!newPotEntries.ContainsKey(composedKey))
								newPotEntries[composedKey] = newEntry;
						}
						else
						{
							emptyTextCount++;
							UiLog.Log($"[PullFromSheets] Unmatched but empty text: lang={lang} composedKey=[{composedKey}]");
						}
					}
				}
			}

			UiLog.Log($"[PullFromSheets] Summary: total={totalEntries} noLang={skippedNoLang} noFile={skippedNoFile} " +
			          $"exactMatch={exactMatchCount} trimmedSkip={trimmedSkipCount} updated={updatedCount} " +
			          $"created={createdCount} emptyText={emptyTextCount}");
			UiLog.Log($"[PullFromSheets] Languages in PO: {string.Join(", ", langToFilePath.Keys)}");
			UiLog.Log($"[PullFromSheets] Lang contexts: {string.Join(", ", langContextKeys.Select(x => $"{x.lang}|{x.prefix}|{x.postfix}"))}");

			if (dirtyFiles.Count == 0)
			{
				string msg = $"No new translations found.\n\n" +
					$"Entries processed: {totalEntries}\n" +
					$"Exact matches: {exactMatchCount}\n" +
					$"Trimmed skips: {trimmedSkipCount}\n" +
					$"Empty text (no match): {emptyTextCount}\n" +
					$"Skipped (no lang): {skippedNoLang}\n" +
					$"Skipped (no PO file): {skippedNoFile}";
				if (_suppressDialogs)
					UiLog.Log($"{nameof(LocaGettextSheetsSyncer)} [{_bridge.name}]: {msg}");
				else
					EditorUtility.DisplayDialog("Pull from Sheets", msg, "OK");
				return;
			}

			// If new keys were created, also add them to the POT template so the next
			// "Process Loca" merge does not mark them obsolete.
			if (createdCount > 0)
			{
				string potPath = LocaPoMerger.GetPotPath(group);
				if (!string.IsNullOrEmpty(potPath) && File.Exists(potPath))
				{
					var pot       = PoFile.Parse(File.ReadAllText(potPath, Encoding.UTF8));
					var potLookup = pot.BuildLookup();
					int potAdded  = 0;

					// Only add entries that were just created by this Pull operation.
				foreach (var kvp in newPotEntries)
				{
					string key = kvp.Key;
					if (potLookup.ContainsKey(NormalizeCrlf(key)))
						continue;

					var src      = kvp.Value;
					var potEntry = new PoEntry
					{
						MsgId       = src.MsgId,
						Context     = src.Context,
						MsgIdPlural = src.MsgIdPlural,
						MsgStr      = string.Empty,
					};

					pot.Entries.Add(potEntry);
					potLookup[key] = potEntry;
					potAdded++;
				}

					if (potAdded > 0)
					{
						PoBackupManager.CreateBackup(potPath);
						File.WriteAllText(potPath, pot.Serialize(), new System.Text.UTF8Encoding(false));
						UiLog.Log($"{nameof(LocaGettextSheetsSyncer)}: Added {potAdded} new key(s) to POT '{potPath}'.");
					}
				}
			}

			foreach (string filePath in dirtyFiles)
			{
				PoBackupManager.CreateBackup(filePath);
				string serialized = parsedPoFiles[filePath].Serialize();
				WritePoFilePair(filePath, serialized);
			}

			AssetDatabase.Refresh();

			string msg2 = createdCount > 0
				? $"Updated {updatedCount} and created {createdCount} translation(s) in {dirtyFiles.Count} PO file(s)."
				: $"Merged {updatedCount} translation(s) from the sheet into {dirtyFiles.Count} PO file(s).";
			if (!_suppressDialogs)
				EditorUtility.DisplayDialog("Pull from Sheets", msg2, "OK");
			UiLog.Log($"{nameof(LocaGettextSheetsSyncer)}: Pulled {updatedCount} updated, {createdCount} new translation(s) into {dirtyFiles.Count} PO file(s).");
		}

		/// <summary>
		/// Pulls the latest translations from <b>all</b> configured <see cref="LocaExcelBridge"/> assets
		/// whose source type is <see cref="LocaExcelBridge.SourceType.GoogleDocs"/>.
		/// Intended for use in build-preprocessor contexts where dialogs must not be shown.
		/// Results are written to the console log.
		/// </summary>
		/// <param name="_suppressDialogs">
		/// When <c>true</c> (the default for build use), all user-facing dialogs are suppressed.
		/// Pass <c>false</c> to show dialogs (same as calling <see cref="PullFromSheets"/> individually).
		/// </param>
		public static void PullAllFromSheets(bool _suppressDialogs = true)
		{
			string[] guids = AssetDatabase.FindAssets("t:LocaExcelBridge");
			if (guids.Length == 0)
			{
				UiLog.Log($"{nameof(LocaGettextSheetsSyncer)}: No LocaExcelBridge assets found — skipping pull.");
				return;
			}

			int pulled = 0;
			foreach (string guid in guids)
			{
				string path = AssetDatabase.GUIDToAssetPath(guid);
				var bridge = AssetDatabase.LoadAssetAtPath<LocaExcelBridge>(path);
				if (bridge == null || bridge.EdSourceType != LocaExcelBridge.SourceType.GoogleDocs)
					continue;

				UiLog.Log($"{nameof(LocaGettextSheetsSyncer)}: Pulling from Sheets for bridge '{bridge.name}' ({path})…");
				PullFromSheets(bridge, _suppressDialogs);
				pulled++;
			}

			UiLog.Log($"{nameof(LocaGettextSheetsSyncer)}: PullAllFromSheets complete — {pulled} bridge(s) processed.");
		}

		/// <summary>
		/// Appends keys from local PO files that are not yet present in the Google Sheet.
		/// Translation cells for new rows are left empty so translators can fill them in.
		/// Never modifies existing sheet cells.
		/// Only runs when <see cref="LocaExcelBridge.CanPush"/> is <c>true</c> and the source type is GoogleDocs.
		/// </summary>
		/// <param name="_bridge">The bridge asset to push new keys to.</param>
		/// <param name="_dryRun">
		/// When <c>true</c>, no data is written to the sheet. Instead a TSV preview file is written
		/// to <c>./Temp/PushKeysDryRun[_group].txt</c> so the new keys can be reviewed before pushing.
		/// </param>
		public static void PushNewKeysToSheets(LocaExcelBridge _bridge, bool _dryRun = false)
		{
			if (_bridge == null)
				return;

			if (_bridge.EdSourceType != LocaExcelBridge.SourceType.GoogleDocs)
				return;

			if (!_bridge.CanPush)
			{
				UiLog.LogWarning($"{nameof(LocaGettextSheetsSyncer)}: CanPush is false for '{_bridge.name}'. Skipping.");
				return;
			}

			string spreadsheetId = ExtractSpreadsheetId(_bridge.EdGoogleUrl);
			if (string.IsNullOrEmpty(spreadsheetId))
			{
				EditorUtility.DisplayDialog("Push new keys",
					$"Could not extract spreadsheet ID from URL:\n{_bridge.EdGoogleUrl}", "OK");
				return;
			}

			string token = GoogleServiceAccountAuth.GetAccessToken(_bridge.EdServiceAccountJsonPath, _writeAccess: !_dryRun);
			if (token == null)
			{
				EditorUtility.DisplayDialog("Push new keys",
					"Failed to obtain a Google auth token. Check the service account JSON path.", "OK");
				return;
			}

			// Find the key column index in the bridge config.
			int keyColIdx = GetKeyColIdx(_bridge);
			if (keyColIdx < 0)
			{
				EditorUtility.DisplayDialog("Push new keys",
					"No Key column configured in the bridge.", "OK");
				return;
			}

			// Download current sheet content to discover existing keys.
			string sheetName;
			int sheetId;
			try
			{
				(sheetName, sheetId, _) = GetFirstSheetInfo(spreadsheetId, token);
			}
			catch (Exception ex)
			{
				EditorUtility.DisplayDialog("Push new keys – Error",
					$"Failed to read spreadsheet metadata:\n{ex.Message}", "OK");
				UiLog.LogError($"{nameof(LocaGettextSheetsSyncer)}: Failed to GET spreadsheet metadata: {ex}");
				return;
			}

			List<List<string>> sheetValues;
			try
			{
				sheetValues = GetSheetValues(spreadsheetId, token, sheetName);
			}
			catch (Exception ex)
			{
				EditorUtility.DisplayDialog("Push new keys – Error",
					$"Failed to read sheet:\n{ex.Message}", "OK");
				UiLog.LogError($"{nameof(LocaGettextSheetsSyncer)}: Failed to GET sheet values: {ex}");
				return;
			}

			var existingKeys = new HashSet<string>(StringComparer.Ordinal);
			int startRow     = _bridge.EdStartRow;
			for (int r = startRow; r < sheetValues.Count; r++)
			{
				var row = sheetValues[r];
				if (keyColIdx < row.Count && !string.IsNullOrEmpty(row[keyColIdx]))
					existingKeys.Add(row[keyColIdx]);
			}

			// Collect all active msgids from PO files and build per-language translation lookups.
			// Only msgids that appear in the corresponding POT file are eligible for pushing;
			// this prevents stale/debug keys that linger in PO files from polluting the sheet.
			string group    = _bridge.EdGroup;
			var poFiles     = LocaCsvExporter.FindPoFiles(group);

			// Build POT whitelist so stale PO-only keys are never pushed.
			var potWhitelist = new HashSet<string>(StringComparer.Ordinal);
			string potPath = LocaPoMerger.GetPotPath(group);
			if (!string.IsNullOrEmpty(potPath) && File.Exists(potPath))
			{
				var potFile = PoFile.Parse(File.ReadAllText(potPath, Encoding.UTF8));
				foreach (var e in potFile.Entries)
				{
					if (!e.IsObsolete && !string.IsNullOrEmpty(e.MsgId))
						potWhitelist.Add(e.MsgId);
				}
				UiLog.Log($"{nameof(LocaGettextSheetsSyncer)}: POT whitelist loaded — {potWhitelist.Count} keys from '{Path.GetFileName(potPath)}'.");
			}
			else
			{
				UiLog.LogWarning($"{nameof(LocaGettextSheetsSyncer)}: No POT file found for group '{group}' — pushing all PO keys without filtering.");
			}

			var allMsgIds   = new HashSet<string>(StringComparer.Ordinal);
			var msgIdOrder  = new List<string>();
			var langLookups = new Dictionary<string, Dictionary<string, PoEntry>>(StringComparer.OrdinalIgnoreCase);

			foreach (var (fileLang, _, filePath) in poFiles)
			{
				string fileContent = File.ReadAllText(filePath, Encoding.UTF8);
				var poFile = PoFile.Parse(fileContent);

				if (!langLookups.TryGetValue(fileLang, out var lookup))
					langLookups[fileLang] = lookup = new Dictionary<string, PoEntry>(StringComparer.Ordinal);

				foreach (var entry in poFile.Entries)
				{
					if (entry.IsObsolete || string.IsNullOrEmpty(entry.MsgId))
						continue;

					// Skip keys not in the POT (stale debug/test entries).
					if (potWhitelist.Count > 0 && !potWhitelist.Contains(entry.MsgId))
						continue;

					if (allMsgIds.Add(entry.MsgId))
						msgIdOrder.Add(entry.MsgId);

					if (!lookup.ContainsKey(entry.MsgId))
						lookup[entry.MsgId] = entry;
				}
			}

			msgIdOrder.Sort(StringComparer.Ordinal);

			var newKeys = FindNewKeys(existingKeys, msgIdOrder);

			if (newKeys.Count == 0)
			{
				EditorUtility.DisplayDialog("Push new keys",
					"All keys from PO files are already present in the sheet. Nothing to push.", "OK");
				return;
			}

			// Build rows: key cell = msgid, translation cells populated from PO where available.
			int numCols = _bridge.NumColumns;
			var newRows = new List<List<string>>(newKeys.Count);

			foreach (string msgId in newKeys)
			{
				var row = new List<string>(numCols);
				for (int c = 0; c < numCols; c++)
					row.Add(string.Empty);

				row[keyColIdx] = msgId;

				for (int c = 0; c < numCols; c++)
				{
					if (c == keyColIdx) continue;
					var d = _bridge.GetColumnDescription(c);
					if (d?.ColumnType != LocaExcelBridge.EInColumnType.LanguageTranslation) continue;
					if (string.IsNullOrWhiteSpace(d.LanguageId)) continue;

					string lang = d.LanguageId.Trim().ToLowerInvariant();
					if (!langLookups.TryGetValue(lang, out var lookup)) continue;
					if (!lookup.TryGetValue(msgId, out var entry)) continue;

					if (!entry.IsPlural)
						row[c] = entry.MsgStr ?? string.Empty;
					else if (d.PluralForm >= 0 && entry.MsgStrForms != null && d.PluralForm < entry.MsgStrForms.Length)
						row[c] = entry.MsgStrForms[d.PluralForm] ?? string.Empty;
				}

				newRows.Add(row);
			}

			// Dry-run: write preview file and return without touching the sheet.
			if (_dryRun)
			{
				WritePushDryRunFile(_bridge, group, newRows);
				return;
			}

			try
			{
				// If the sheet has no header row yet, create it from the bridge column definitions.
				if (sheetValues.Count == 0 || (sheetValues.Count == 1 && sheetValues[0].All(string.IsNullOrEmpty)))
				{
					var header = new List<string>(_bridge.NumColumns);
					for (int c = 0; c < _bridge.NumColumns; c++)
					{
						var d = _bridge.GetColumnDescription(c);
						header.Add(d?.Description ?? string.Empty);
					}
					AppendSheetRows(spreadsheetId, token, sheetName, new List<List<string>> { header });
				}

				string appendedRange = AppendSheetRows(spreadsheetId, token, sheetName, newRows);

				// Highlight the key cell of every new row so translators can spot them easily.
				// Highlighting is skipped when the configured colour has zero alpha.
				if (!string.IsNullOrEmpty(appendedRange) &&
				    TryParseRangeStartRow(appendedRange, out int startRowIndex))
				{
					var highlightColor = UiToolkitConfiguration.Instance != null
						? UiToolkitConfiguration.Instance.NewKeyHighlightColor
						: new UnityEngine.Color(1.0f, 0.95f, 0.2f, 1.0f);

					if (highlightColor.a > 0f)
					{
						try
						{
							ApplyColumnBackground(spreadsheetId, token, sheetId,
								startRowIndex, newRows.Count, keyColIdx,
								highlightColor.r, highlightColor.g, highlightColor.b);
						}
						catch (Exception ex)
						{
							// Non-fatal — data was pushed successfully, formatting is best-effort.
							UiLog.LogWarning($"{nameof(LocaGettextSheetsSyncer)}: Could not apply key-cell highlight: {ex.Message}");
						}
					}
				}
			}
			catch (Exception ex)
			{
				EditorUtility.DisplayDialog("Push new keys – Error",
					$"Failed to append rows:\n{ex.Message}", "OK");
				UiLog.LogError($"{nameof(LocaGettextSheetsSyncer)}: Failed to append rows: {ex}");
				return;
			}

			EditorUtility.DisplayDialog("Push new keys",
				$"Successfully appended {newKeys.Count} new key(s) to the sheet.", "OK");
			UiLog.Log($"{nameof(LocaGettextSheetsSyncer)}: Pushed {newKeys.Count} new key(s) to sheet '{spreadsheetId}'.");

			SaveSheetXlsxBackup(_bridge, spreadsheetId, token);
		}

		// -----------------------------------------------------------------------
		// Dry-run helper
		// -----------------------------------------------------------------------

		/// <summary>
		/// Writes a TSV preview of the keys that <see cref="PushNewKeysToSheets"/> would append,
		/// saved to <c>./Temp/PushKeysDryRun[_group].txt</c>. Opens the file when the user clicks OK.
		/// </summary>
		private static void WritePushDryRunFile(LocaExcelBridge _bridge, string _group, List<List<string>> _newRows)
		{
			string tempDir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Temp"));
			Directory.CreateDirectory(tempDir);

			string groupSuffix = string.IsNullOrEmpty(_group) ? string.Empty : $"_{_group}";
			string filePath    = Path.Combine(tempDir, $"PushKeysDryRun{groupSuffix}.txt");

			var sb = new StringBuilder();

			// Header row from bridge column descriptions.
			int numCols = _bridge.NumColumns;
			var headerParts = new string[numCols];
			for (int c = 0; c < numCols; c++)
			{
				var d = _bridge.GetColumnDescription(c);
				headerParts[c] = d?.Description ?? $"Col{c}";
			}
			sb.AppendLine(string.Join("\t", headerParts));

			// Data rows.
			foreach (var row in _newRows)
			{
				var parts = new string[numCols];
				for (int c = 0; c < numCols; c++)
					parts[c] = c < row.Count ? row[c] : string.Empty;
				sb.AppendLine(string.Join("\t", parts));
			}

			File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
			UiLog.Log($"{nameof(LocaGettextSheetsSyncer)}: Dry-run preview written to '{filePath}'.");

			bool open = EditorUtility.DisplayDialog(
				"Push new keys – Dry Run",
				$"{_newRows.Count} key(s) would be pushed to the sheet.\n\nPreview saved to:\n{filePath}",
				"Open file", "Close");

			if (open)
				System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(filePath) { UseShellExecute = true });
		}

		/// <summary>
		/// Finds rows in the Google Sheet whose key is not present in any active (non-obsolete) PO entry
		/// for the bridge's group — i.e. keys that have been removed from the project but linger in the sheet.
		/// </summary>
		/// <param name="_bridge">The bridge to query.</param>
		/// <returns>
		/// A list of (0-based row index, key string) pairs for obsolete sheet rows,
		/// an empty list when none are found, or <c>null</c> when the operation cannot proceed
		/// (wrong source type, missing auth, API error, etc.).
		/// </returns>
		public static List<(int rowIndex0, string key)> FindObsoleteInSheets(LocaExcelBridge _bridge)
		{
			if (_bridge == null || _bridge.EdSourceType != LocaExcelBridge.SourceType.GoogleDocs || !_bridge.CanPush)
				return null;

			string spreadsheetId = ExtractSpreadsheetId(_bridge.EdGoogleUrl);
			if (string.IsNullOrEmpty(spreadsheetId))
				return null;

			string token = GoogleServiceAccountAuth.GetAccessToken(_bridge.EdServiceAccountJsonPath, _writeAccess: false);
			if (token == null)
			{
				UiLog.LogError($"{nameof(LocaGettextSheetsSyncer)}: FindObsoleteInSheets: failed to obtain auth token.");
				return null;
			}

			int keyColIdx = GetKeyColIdx(_bridge);
			if (keyColIdx < 0)
				return null;

			List<List<string>> sheetValues;
			try
			{
				string sheetName = GetFirstSheetInfo(spreadsheetId, token).name;
				sheetValues = GetSheetValues(spreadsheetId, token, sheetName);
			}
			catch (Exception ex)
			{
				UiLog.LogError($"{nameof(LocaGettextSheetsSyncer)}: FindObsoleteInSheets: failed to read sheet: {ex.Message}");
				return null;
			}

			var activeMsgIds = LoadAllActiveMsgIds(_bridge.EdGroup);

			var result = new List<(int, string)>();
			int startRow = _bridge.EdStartRow;
			for (int r = startRow; r < sheetValues.Count; r++)
			{
				var row = sheetValues[r];
				if (keyColIdx >= row.Count) continue;

				string key = row[keyColIdx];
				if (string.IsNullOrEmpty(key)) continue;

				if (!activeMsgIds.Contains(key))
					result.Add((r, key));
			}

			return result;
		}

		/// <summary>
		/// Marks the given sheet rows as obsolete by applying a pale-red background and
		/// a "Obsolete" note to the key cell of each row.
		/// </summary>
		/// <param name="_bridge">The bridge identifying the spreadsheet.</param>
		/// <param name="_obsoleteRows">Pre-computed list from <see cref="FindObsoleteInSheets"/>.</param>
		public static void MarkObsoleteInSheets(LocaExcelBridge _bridge, List<(int rowIndex0, string key)> _obsoleteRows)
		{
			if (_bridge == null || _obsoleteRows == null || _obsoleteRows.Count == 0)
				return;

			string spreadsheetId = ExtractSpreadsheetId(_bridge.EdGoogleUrl);
			if (string.IsNullOrEmpty(spreadsheetId))
			{
				EditorUtility.DisplayDialog("Mark obsolete – Error",
					$"Could not extract spreadsheet ID from URL:\n{_bridge.EdGoogleUrl}", "OK");
				return;
			}

			string token = GoogleServiceAccountAuth.GetAccessToken(_bridge.EdServiceAccountJsonPath, _writeAccess: true);
			if (token == null)
			{
				EditorUtility.DisplayDialog("Mark obsolete – Error",
					"Failed to obtain a Google auth token.", "OK");
				return;
			}

			int keyColIdx = GetKeyColIdx(_bridge);
			if (keyColIdx < 0)
			{
				EditorUtility.DisplayDialog("Mark obsolete – Error",
					"No Key column configured in the bridge.", "OK");
				return;
			}

			(_, int sheetId, _) = GetFirstSheetInfo(spreadsheetId, token);

			try
			{
				string json = BuildMarkObsoleteJson(sheetId, keyColIdx, _obsoleteRows);
				string url  = $"https://sheets.googleapis.com/v4/spreadsheets/{spreadsheetId}:batchUpdate";

				using var client = new HttpClient();
				client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
				var content  = new StringContent(json, Encoding.UTF8, "application/json");
				var response = client.PostAsync(url, content).GetAwaiter().GetResult();
				string body  = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

				if (!response.IsSuccessStatusCode)
					throw new InvalidOperationException($"Sheets API batchUpdate error {(int)response.StatusCode}: {body}");
			}
			catch (Exception ex)
			{
				EditorUtility.DisplayDialog("Mark obsolete – Error",
					$"Failed to mark obsolete rows:\n{ex.Message}", "OK");
				UiLog.LogError($"{nameof(LocaGettextSheetsSyncer)}: MarkObsoleteInSheets failed: {ex}");
				return;
			}

			EditorUtility.DisplayDialog("Mark obsolete",
				$"Marked {_obsoleteRows.Count} key(s) as obsolete in the sheet.", "OK");
			UiLog.Log($"{nameof(LocaGettextSheetsSyncer)}: Marked {_obsoleteRows.Count} obsolete key(s) in '{spreadsheetId}'.");
		}

		// -----------------------------------------------------------------------
		// Internal helpers exposed for testing
		// -----------------------------------------------------------------------

		/// <summary>Builds a canonical column list from parsed PO file data.</summary>
		/// Produces one Key column followed by, for each language (sorted alphabetically),
		/// a singular column and one column per plural form used in any entry.
		/// </summary>
		internal static List<LocaExcelBridge.InColumnDescription> BuildColumnsFromPoData(
			IList<(string lang, PoFile file)> _poData)
		{
			var langMaxPluralForms = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
			var langOrder = new List<string>();

			foreach (var (lang, poFile) in _poData)
			{
				string normLang = lang.Trim().ToLowerInvariant();
				if (!langMaxPluralForms.ContainsKey(normLang))
				{
					langMaxPluralForms[normLang] = 0;
					langOrder.Add(normLang);
				}

				int maxForms = langMaxPluralForms[normLang];
				foreach (var entry in poFile.Entries)
				{
					if (entry.IsObsolete || !entry.IsPlural || entry.MsgStrForms == null)
						continue;
					maxForms = Math.Max(maxForms, entry.MsgStrForms.Length);
				}
				langMaxPluralForms[normLang] = maxForms;
			}

			langOrder.Sort(StringComparer.OrdinalIgnoreCase);

			var columns = new List<LocaExcelBridge.InColumnDescription>();

			var keyCol = new LocaExcelBridge.InColumnDescription
			{
				ColumnType = LocaExcelBridge.EInColumnType.Key,
				LanguageId = string.Empty,
				KeyPrefix  = string.Empty,
				KeyPostfix = string.Empty,
				PluralForm = -1,
			};
			keyCol.UpdateDescriptionField();
			columns.Add(keyCol);

			foreach (string lang in langOrder)
			{
				var singularCol = new LocaExcelBridge.InColumnDescription
				{
					ColumnType = LocaExcelBridge.EInColumnType.LanguageTranslation,
					LanguageId = lang,
					KeyPrefix  = string.Empty,
					KeyPostfix = string.Empty,
					PluralForm = -1,
				};
				singularCol.UpdateDescriptionField();
				columns.Add(singularCol);

				int maxForms = langMaxPluralForms[lang];
				for (int i = 0; i < maxForms; i++)
				{
					var pluralCol = new LocaExcelBridge.InColumnDescription
					{
						ColumnType = LocaExcelBridge.EInColumnType.LanguageTranslation,
						LanguageId = lang,
						KeyPrefix  = string.Empty,
						KeyPostfix = string.Empty,
						PluralForm = i,
					};
					pluralCol.UpdateDescriptionField();
					columns.Add(pluralCol);
				}
			}

			return columns;
		}

		/// <summary>
		/// Writes <paramref name="_content"/> to <paramref name="_filePath"/> and, if a companion
		/// file with the other extension (<c>.po</c> ↔ <c>.po.txt</c>) exists, to that file too.
		/// This keeps both copies byte-identical, as required by the Unity TextAsset import convention.
		/// </summary>
		private static void WritePoFilePair(string _filePath, string _content)
		{
			var enc = new UTF8Encoding(false);
			File.WriteAllText(_filePath, _content, enc);

			// Write companion file when it exists.
			string companion;
			if (_filePath.EndsWith(".po.txt", StringComparison.OrdinalIgnoreCase))
				companion = _filePath.Substring(0, _filePath.Length - 4); // strip ".txt"
			else if (_filePath.EndsWith(".po", StringComparison.OrdinalIgnoreCase))
				companion = _filePath + ".txt";
			else
				return;

			if (File.Exists(companion))
				File.WriteAllText(companion, _content, enc);
		}

		/// <summary>
		/// Merges a <see cref="ProcessedLocaEntry"/> into a <see cref="PoEntry"/>.
		/// Sheet values overwrite local translations; empty sheet cells leave the local value untouched.
		/// </summary>
		/// <returns><c>true</c> if any field was updated; <c>false</c> if nothing changed.</returns>
		internal static bool MergeTranslationIntoPoEntry(ProcessedLocaEntry _entry, PoEntry _poEntry)
		{
			bool modified = false;

			// Text comes from PluralForm=-1 bridge columns and maps directly to MsgStr.
			// Update only when content meaningfully differs; ignore trailing-whitespace noise from sheet cells.
			if (!string.IsNullOrEmpty(_entry.Text) && _entry.Text.Trim() != (_poEntry.MsgStr ?? string.Empty).Trim())
			{
				_poEntry.MsgStr = _entry.Text;
				modified = true;
			}

			if (_entry.Forms != null)
			{
				if (!_poEntry.IsPlural)
				{
					// Non-plural PO entry: bridge PluralForm=0 columns store the singular translation
					// in Forms[0], but it must land in MsgStr (not MsgStrForms, which Serialize ignores
					// for non-plural entries).  Only apply when Text hasn't already set MsgStr.
					if (string.IsNullOrEmpty(_entry.Text) &&
						_entry.Forms.Length > 0 &&
						!string.IsNullOrEmpty(_entry.Forms[0]) &&
						_entry.Forms[0] != _poEntry.MsgStr)
					{
						_poEntry.MsgStr = _entry.Forms[0];
						modified = true;
					}
				}
				else
				{
					// Plural PO entry (has msgid_plural): write each form to MsgStrForms.
					for (int i = 0; i < _entry.Forms.Length; i++)
					{
						if (string.IsNullOrEmpty(_entry.Forms[i]))
							continue;

						if (_poEntry.MsgStrForms == null)
							_poEntry.MsgStrForms = new string[Math.Max(2, i + 1)];
						else if (_poEntry.MsgStrForms.Length <= i)
							Array.Resize(ref _poEntry.MsgStrForms, i + 1);

						if (_entry.Forms[i] != _poEntry.MsgStrForms[i])
						{
							_poEntry.MsgStrForms[i] = _entry.Forms[i];
							modified = true;
						}
					}
				}
			}

			return modified;
		}

		/// <summary>
		/// Returns the subset of <paramref name="_allMsgIds"/> that is not present in
		/// <paramref name="_existingKeys"/>, preserving the order of <paramref name="_allMsgIds"/>.
		/// </summary>
		internal static List<string> FindNewKeys(ICollection<string> _existingKeys, IEnumerable<string> _allMsgIds)
		{
			var result = new List<string>();
			foreach (string key in _allMsgIds)
			{
				if (!_existingKeys.Contains(key))
					result.Add(key);
			}
			return result;
		}

		// -----------------------------------------------------------------------
		// Private helpers shared by PushNewKeysToSheets / FindObsoleteInSheets
		// -----------------------------------------------------------------------

		private static int GetKeyColIdx(LocaExcelBridge _bridge)
		{
			for (int i = 0; i < _bridge.NumColumns; i++)
			{
				var d = _bridge.GetColumnDescription(i);
				if (d?.ColumnType == LocaExcelBridge.EInColumnType.Key)
					return i;
			}
			return -1;
		}

		/// <summary>
		/// Loads all active (non-obsolete) msgids from every PO file for the given group.
		/// </summary>
		private static HashSet<string> LoadAllActiveMsgIds(string _group)
		{
			var result  = new HashSet<string>(StringComparer.Ordinal);
			var poFiles = LocaCsvExporter.FindPoFiles(_group);

			foreach (var (_, _, filePath) in poFiles)
			{
				string content = File.ReadAllText(filePath, Encoding.UTF8);
				var poFile = PoFile.Parse(content);
				foreach (var entry in poFile.Entries)
				{
					if (!entry.IsObsolete && !string.IsNullOrEmpty(entry.MsgId))
						result.Add(entry.MsgId);
				}
			}

			return result;
		}

		/// <summary>
		/// Builds the JSON body for a Sheets <c>spreadsheets:batchUpdate</c> request that
		/// applies a pale-red background and an "Obsolete" note to the key cell of each given row.
		/// </summary>
		private static string BuildMarkObsoleteJson(int _sheetId, int _keyColIdx, List<(int rowIndex0, string key)> _rows)
		{
			var sb = new StringBuilder();
			sb.Append("{\"requests\":[");

			bool first = true;
			foreach (var (rowIdx, _) in _rows)
			{
				if (!first) sb.Append(',');
				first = false;

				string range =
					$"\"sheetId\":{_sheetId}," +
					$"\"startRowIndex\":{rowIdx}," +
					$"\"endRowIndex\":{rowIdx + 1}," +
					$"\"startColumnIndex\":{_keyColIdx}," +
					$"\"endColumnIndex\":{_keyColIdx + 1}";

				// Pale-red background
				sb.Append("{\"repeatCell\":{\"range\":{");
				sb.Append(range);
				sb.Append("},\"cell\":{\"userEnteredFormat\":{\"backgroundColor\":{");
				sb.Append("\"red\":1.0,\"green\":0.6,\"blue\":0.6");
				sb.Append("}}},\"fields\":\"userEnteredFormat.backgroundColor\"}}");

				sb.Append(',');

				// "Obsolete" note
				sb.Append("{\"updateCells\":{\"range\":{");
				sb.Append(range);
				sb.Append("},\"rows\":[{\"values\":[{\"note\":\"Obsolete\"}]}],\"fields\":\"note\"}}");
			}

			sb.Append("]}");
			return sb.ToString();
		}

		// -----------------------------------------------------------------------
		// Sheets API helpers
		// -----------------------------------------------------------------------

		/// <summary>
		/// Returns the title and numeric sheet ID of the first sheet in the spreadsheet.
		/// Falls back to <see cref="DEFAULT_SHEET_NAME"/> and sheetId 0 if values cannot be determined.
		/// </summary>
		internal static (string name, int sheetId, int rowCount) GetFirstSheetInfo(string _spreadsheetId, string _token)
		{
			string url = $"https://sheets.googleapis.com/v4/spreadsheets/{_spreadsheetId}?fields=sheets.properties";

			using var client = new HttpClient();
			client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token);

			var response = client.GetAsync(url).GetAwaiter().GetResult();
			string body  = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

			if (!response.IsSuccessStatusCode)
				throw new InvalidOperationException($"Sheets API GET metadata error {(int)response.StatusCode}: {body}");

			string name   = DEFAULT_SHEET_NAME;
			int sheetId   = 0;
			int rowCount  = 0;

			// Parse title
			int titleIdx = body.IndexOf("\"title\"", StringComparison.Ordinal);
			if (titleIdx >= 0)
			{
				int colonIdx = body.IndexOf(':', titleIdx);
				if (colonIdx >= 0)
				{
					int quoteStart = body.IndexOf('"', colonIdx + 1);
					if (quoteStart >= 0)
					{
						int pos = quoteStart;
						name = ParseJsonString(body, ref pos);
					}
				}
			}

			// Parse sheetId
			int idIdx = body.IndexOf("\"sheetId\"", StringComparison.Ordinal);
			if (idIdx >= 0)
			{
				int colonIdx = body.IndexOf(':', idIdx);
				if (colonIdx >= 0)
				{
					int numStart = colonIdx + 1;
					while (numStart < body.Length && char.IsWhiteSpace(body[numStart])) numStart++;
					int numEnd = numStart;
					while (numEnd < body.Length && char.IsDigit(body[numEnd])) numEnd++;
					if (numEnd > numStart)
						int.TryParse(body.Substring(numStart, numEnd - numStart), out sheetId);
				}
			}

			// Parse rowCount from gridProperties
			int rcIdx = body.IndexOf("\"rowCount\"", StringComparison.Ordinal);
			if (rcIdx >= 0)
			{
				int colonIdx = body.IndexOf(':', rcIdx);
				if (colonIdx >= 0)
				{
					int numStart = colonIdx + 1;
					while (numStart < body.Length && char.IsWhiteSpace(body[numStart])) numStart++;
					int numEnd = numStart;
					while (numEnd < body.Length && char.IsDigit(body[numEnd])) numEnd++;
					if (numEnd > numStart)
						int.TryParse(body.Substring(numStart, numEnd - numStart), out rowCount);
				}
			}

			return (name, sheetId, rowCount);
		}

		internal static List<List<string>> GetSheetValues(string _spreadsheetId, string _token, string _sheetName)
		{
			string url = $"https://sheets.googleapis.com/v4/spreadsheets/{_spreadsheetId}/values/{Uri.EscapeDataString(_sheetName)}";

			using var client = new HttpClient();
			client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token);

			var response = client.GetAsync(url).GetAwaiter().GetResult();
			string body  = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

			if (!response.IsSuccessStatusCode)
				throw new InvalidOperationException($"Sheets API GET error {(int)response.StatusCode}: {body}");

			return ParseSheetValues(body);
		}

		internal static string AppendSheetRows(string _spreadsheetId, string _token, string _sheetName, List<List<string>> _rows)
		{
			string url =
				$"https://sheets.googleapis.com/v4/spreadsheets/{_spreadsheetId}/values/{Uri.EscapeDataString(_sheetName)}:append" +
				"?valueInputOption=RAW&insertDataOption=INSERT_ROWS";

			string json = BuildValuesJson(_rows);

			using var client = new HttpClient();
			client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token);

			var content  = new StringContent(json, Encoding.UTF8, "application/json");
			var response = client.PostAsync(url, content).GetAwaiter().GetResult();
			string body  = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

			if (!response.IsSuccessStatusCode)
				throw new InvalidOperationException($"Sheets API append error {(int)response.StatusCode}: {body}");

			return ParseJsonStringValue(body, "updatedRange");
		}

		/// <summary>
		/// Applies a solid background colour to a single column of consecutive rows
		/// using the Sheets spreadsheets:batchUpdate (formatting) endpoint.
		/// </summary>
		private static void ApplyColumnBackground(
			string _spreadsheetId, string _token, int _sheetId,
			int _startRowIndex, int _rowCount, int _colIndex,
			float _r, float _g, float _b)
		{
			string url = $"https://sheets.googleapis.com/v4/spreadsheets/{_spreadsheetId}:batchUpdate";

			string json =
				"{\"requests\":[{\"repeatCell\":{" +
				"\"range\":{" +
				$"\"sheetId\":{_sheetId}," +
				$"\"startRowIndex\":{_startRowIndex}," +
				$"\"endRowIndex\":{_startRowIndex + _rowCount}," +
				$"\"startColumnIndex\":{_colIndex}," +
				$"\"endColumnIndex\":{_colIndex + 1}" +
				"}," +
				"\"cell\":{\"userEnteredFormat\":{\"backgroundColor\":{" +
				$"\"red\":{_r.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)}," +
				$"\"green\":{_g.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)}," +
				$"\"blue\":{_b.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)}" +
				"}}}," +
				"\"fields\":\"userEnteredFormat.backgroundColor\"" +
				"}}]}";

			using var client = new HttpClient();
			client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token);

			var content  = new StringContent(json, Encoding.UTF8, "application/json");
			var response = client.PostAsync(url, content).GetAwaiter().GetResult();
			string body  = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

			if (!response.IsSuccessStatusCode)
				throw new InvalidOperationException($"Sheets API batchUpdate (format) error {(int)response.StatusCode}: {body}");
		}

		/// <summary>
		/// Parses the 0-based start row index from a Sheets range string such as
		/// <c>"Sheet1!A6:D8"</c> or <c>"Sheet1!A6"</c>.
		/// Returns true on success; the out value is 0-based (Sheets API uses 0-based row indices).
		/// </summary>
		private static bool TryParseRangeStartRow(string _range, out int _startRowIndex0Based)
		{
			_startRowIndex0Based = 0;
			if (string.IsNullOrEmpty(_range)) return false;

			// Strip optional sheet-name prefix ("Sheet1!A6:D8" → "A6:D8").
			int exclamIdx = _range.IndexOf('!');
			string cellPart = exclamIdx >= 0 ? _range.Substring(exclamIdx + 1) : _range;

			// Take the first cell reference only ("A6:D8" → "A6").
			int colonIdx = cellPart.IndexOf(':');
			string firstCell = colonIdx >= 0 ? cellPart.Substring(0, colonIdx) : cellPart;

			// Strip leading column letters to get the 1-based row number.
			int i = 0;
			while (i < firstCell.Length && char.IsLetter(firstCell[i])) i++;
			if (i >= firstCell.Length) return false;

			if (!int.TryParse(firstCell.Substring(i), out int row1Based) || row1Based < 1)
				return false;

			_startRowIndex0Based = row1Based - 1;
			return true;
		}

		/// <summary>Extracts the string value of a simple JSON key from a flat JSON object.</summary>
		private static string ParseJsonStringValue(string _json, string _key)
		{
			string search = $"\"{_key}\"";
			int idx = _json.IndexOf(search, StringComparison.Ordinal);
			if (idx < 0) return null;

			int colonIdx = _json.IndexOf(':', idx + search.Length);
			if (colonIdx < 0) return null;

			int quoteStart = _json.IndexOf('"', colonIdx + 1);
			if (quoteStart < 0) return null;

			int pos = quoteStart;
			return ParseJsonString(_json, ref pos);
		}

		// -----------------------------------------------------------------------
		// JSON builders and parsers
		// -----------------------------------------------------------------------

		internal static string BuildValuesJson(List<List<string>> _rows)
		{
			var sb = new StringBuilder();
			sb.Append("{\"values\":[");

			for (int r = 0; r < _rows.Count; r++)
			{
				if (r > 0) sb.Append(',');
				sb.Append('[');

				var row = _rows[r];
				for (int c = 0; c < row.Count; c++)
				{
					if (c > 0) sb.Append(',');
					sb.Append('"');
					sb.Append(JsonEscape(row[c]));
					sb.Append('"');
				}
				sb.Append(']');
			}

			sb.Append("]}");
			return sb.ToString();
		}

		internal static List<List<string>> ParseSheetValues(string _json)
		{
			var result = new List<List<string>>();
			if (string.IsNullOrEmpty(_json))
				return result;

			int valuesIdx = _json.IndexOf("\"values\"", StringComparison.Ordinal);
			if (valuesIdx < 0)
				return result;

			int outerStart = _json.IndexOf('[', valuesIdx);
			if (outerStart < 0)
				return result;

			int pos = outerStart + 1;
			int len = _json.Length;

			while (pos < len)
			{
				SkipWhitespace(_json, ref pos);
				if (pos >= len || _json[pos] == ']') break;

				if (_json[pos] == '[')
				{
					pos++;
					var row = new List<string>();

					while (pos < len)
					{
						SkipWhitespace(_json, ref pos);
						if (pos >= len) break;

						char ch = _json[pos];
						if (ch == ']') { pos++; break; }
						if (ch == ',') { pos++; continue; }

						if (ch == '"')
						{
							row.Add(ParseJsonString(_json, ref pos));
						}
						else
						{
							int end = pos;
							while (end < len && _json[end] != ',' && _json[end] != ']') end++;
							row.Add(_json.Substring(pos, end - pos).Trim());
							pos = end;
						}
					}
					result.Add(row);
				}
				else if (_json[pos] == ',')
				{
					pos++;
				}
				else
				{
					break;
				}
			}

			return result;
		}

		private static void SkipWhitespace(string _s, ref int _pos)
		{
			while (_pos < _s.Length && char.IsWhiteSpace(_s[_pos])) _pos++;
		}

		private static string ParseJsonString(string _json, ref int _pos)
		{
			_pos++; // skip opening '"'
			var sb = new StringBuilder();

			while (_pos < _json.Length)
			{
				char c = _json[_pos];
				if (c == '"') { _pos++; break; }

				if (c == '\\' && _pos + 1 < _json.Length)
				{
					_pos++;
					switch (_json[_pos])
					{
						case '"':  sb.Append('"');  break;
						case '\\': sb.Append('\\'); break;
						case '/':  sb.Append('/');  break;
						case 'n':  sb.Append('\n'); break;
						case 'r':  sb.Append('\r'); break;
						case 't':  sb.Append('\t'); break;
						default:   sb.Append(_json[_pos]); break;
					}
				}
				else
				{
					sb.Append(c);
				}
				_pos++;
			}
			return sb.ToString();
		}

		private static string JsonEscape(string _s)
		{
			if (_s == null)
				return string.Empty;

			var sb = new StringBuilder(_s.Length + 4);
			foreach (char c in _s)
			{
				switch (c)
				{
					case '"':  sb.Append("\\\""); break;
					case '\\': sb.Append("\\\\"); break;
					case '\n': sb.Append("\\n");  break;
					case '\r': sb.Append("\\r");  break;
					case '\t': sb.Append("\\t");  break;
					default:
						if (c < 0x20)
							sb.Append($"\\u{(int)c:x4}");
						else
							sb.Append(c);
						break;
				}
			}
			return sb.ToString();
		}

		// -----------------------------------------------------------------------
		// Key-affix helpers
		// -----------------------------------------------------------------------

		/// <summary>Normalizes CR+LF and lone CR to LF so that PO keys (which may
		/// have been imported with Windows line endings) match sheet keys (always LF).</summary>
		private static string NormalizeCrlf(string _s)
		{
			if (_s == null || !_s.Contains('\r'))
				return _s ?? string.Empty;
			return _s.Replace("\r\n", "\n").Replace('\r', '\n');
		}

		private static string ReverseKeyAffixes(string _key, LocaExcelBridge.InColumnDescription _desc)
		{
			if (_desc == null)
				return _key ?? string.Empty;

			return ReverseKeyAffixes(_key, _desc.KeyPrefix, _desc.KeyPostfix);
		}

		internal static string ReverseKeyAffixes(string _key, string _prefix, string _postfix)
		{
			string result  = _key     ?? string.Empty;
			string prefix  = _prefix  ?? string.Empty;
			string postfix = _postfix ?? string.Empty;

			if (prefix.Length > 0 && result.StartsWith(prefix, StringComparison.Ordinal))
				result = result.Substring(prefix.Length);
			if (postfix.Length > 0 && result.EndsWith(postfix, StringComparison.Ordinal))
				result = result.Substring(0, result.Length - postfix.Length);

			return result;
		}

		/// <summary>
		/// Deletes rows <paramref name="_fromRow0Based"/> (inclusive) through <paramref name="_toRow0Based"/> (exclusive)
		/// from the given sheet using the Sheets <c>spreadsheets:batchUpdate</c> endpoint.
		/// </summary>
		internal static void DeleteSheetRows(string _spreadsheetId, string _token, int _sheetId, int _fromRow0Based, int _toRow0Based)
		{
			if (_fromRow0Based >= _toRow0Based)
				return;

			string url = $"https://sheets.googleapis.com/v4/spreadsheets/{_spreadsheetId}:batchUpdate";

			string json =
				"{\"requests\":[{\"deleteDimension\":{" +
				"\"range\":{" +
				$"\"sheetId\":{_sheetId}," +
				"\"dimension\":\"ROWS\"," +
				$"\"startIndex\":{_fromRow0Based}," +
				$"\"endIndex\":{_toRow0Based}" +
				"}}}]}";

			using var client = new HttpClient();
			client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token);
			var content  = new StringContent(json, Encoding.UTF8, "application/json");
			var response = client.PostAsync(url, content).GetAwaiter().GetResult();
			string body  = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

			if (!response.IsSuccessStatusCode)
				throw new InvalidOperationException($"Sheets API delete rows error {(int)response.StatusCode}: {body}");

			UiLog.Log($"{nameof(LocaGettextSheetsSyncer)}: Deleted {_toRow0Based - _fromRow0Based} excess row(s) from sheet.");
		}

		/// <summary>
		/// Public entry point for the [Backup Sheets] button.
		/// Obtains a fresh read-scope token and delegates to <see cref="SaveSheetXlsxBackup"/>.
		/// </summary>
		public static void BackupSheets(LocaExcelBridge _bridge)
		{
			if (_bridge == null)
				return;

			if (_bridge.EdSourceType != LocaExcelBridge.SourceType.GoogleDocs)
				return;

			string spreadsheetId = ExtractSpreadsheetId(_bridge.EdGoogleUrl);
			if (string.IsNullOrEmpty(spreadsheetId))
			{
				UiLog.LogError($"{nameof(LocaGettextSheetsSyncer)}: Could not extract spreadsheet ID from URL: {_bridge.EdGoogleUrl}");
				return;
			}

			string token = GoogleServiceAccountAuth.GetAccessToken(_bridge.EdServiceAccountJsonPath, _writeAccess: false);
			if (token == null)
			{
				UiLog.LogError($"{nameof(LocaGettextSheetsSyncer)}: Failed to obtain auth token for backup.");
				return;
			}

			SaveSheetXlsxBackup(_bridge, spreadsheetId, token);
		}

		/// <summary>
		/// Downloads the spreadsheet as an xlsx file and saves it as <c>.bak_{bridge.name}.xlsx</c>
		/// alongside the bridge asset.
		/// Non-fatal — failures are logged as warnings only.
		/// </summary>
		internal static void SaveSheetXlsxBackup(LocaExcelBridge _bridge, string _spreadsheetId, string _token)
		{
			string backupPath = GetBackupPath(_bridge);
			if (backupPath == null)
			{
				UiLog.LogWarning($"{nameof(LocaGettextSheetsSyncer)}: Cannot save backup — asset path unknown.");
				return;
			}

			string exportUrl = $"https://docs.google.com/spreadsheets/d/{_spreadsheetId}/export?format=xlsx";

			try
			{
				using var client = new HttpClient();
				client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token);
				var response = client.GetAsync(exportUrl).GetAwaiter().GetResult();

				if (!response.IsSuccessStatusCode)
				{
					UiLog.LogWarning($"{nameof(LocaGettextSheetsSyncer)}: Backup download failed ({(int)response.StatusCode}).");
					return;
				}

				byte[] bytes = response.Content.ReadAsByteArrayAsync().GetAwaiter().GetResult();
				File.WriteAllBytes(backupPath, bytes);
				UiLog.Log($"{nameof(LocaGettextSheetsSyncer)}: Sheet backup saved → '{backupPath}'.");
			}
			catch (Exception ex)
			{
				UiLog.LogWarning($"{nameof(LocaGettextSheetsSyncer)}: Sheet backup failed: {ex.Message}");
			}
		}

		/// <summary>Returns the full path of the local xlsx backup for <paramref name="_bridge"/>,
		/// or <c>null</c> if the asset path cannot be determined.</summary>
		public static string GetBackupPath(LocaExcelBridge _bridge)
		{
			if (_bridge == null)
				return null;
			string assetPath = AssetDatabase.GetAssetPath(_bridge);
			if (string.IsNullOrEmpty(assetPath))
				return null;
			string projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
			string assetDir    = Path.GetDirectoryName(assetPath) ?? string.Empty;
			return Path.GetFullPath(Path.Combine(projectRoot, assetDir, $".bak_{_bridge.name}.xlsx"));
		}

		internal static string ExtractSpreadsheetId(string _url)
		{
			if (string.IsNullOrEmpty(_url))
				return null;

			const string marker = "/d/";
			int markerIdx = _url.IndexOf(marker, StringComparison.Ordinal);
			if (markerIdx < 0)
				return null;

			int idStart = markerIdx + marker.Length;
			int idEnd   = _url.IndexOfAny(new[] { '/', '?', '#' }, idStart);
			return idEnd >= 0
				? _url.Substring(idStart, idEnd - idStart)
				: _url.Substring(idStart);
		}
	}
}
