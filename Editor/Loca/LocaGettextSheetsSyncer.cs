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
	///   <item><see cref="PushToSheets"/> — appends keys that exist in PO files but are missing from the sheet.</item>
	/// </list>
	/// </summary>
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
		public static void PullFromSheets(LocaExcelBridge _bridge)
		{
			if (_bridge == null)
				return;

			if (_bridge.EdSourceType != LocaExcelBridge.SourceType.GoogleDocs)
			{
				EditorUtility.DisplayDialog("Pull from Sheets",
					"Bridge source type must be GoogleDocs.", "OK");
				return;
			}

			// Collect data from the spreadsheet.
			try
			{
				_bridge.CollectData();
			}
			catch (Exception ex)
			{
				EditorUtility.DisplayDialog("Pull from Sheets – Error",
					$"CollectData failed: {ex.Message}\n\n" +
					"Note: This feature requires Unity 6000 or newer (ExcelDataReader / Roslyn).", "OK");
				UiLog.LogError($"{nameof(LocaGettextSheetsSyncer)}: CollectData failed: {ex}");
				return;
			}

			var processedLoca = _bridge.Localization;
			if (processedLoca?.Entries == null || processedLoca.Entries.Count == 0)
			{
				EditorUtility.DisplayDialog("Pull from Sheets",
					"No entries found after collecting data. Nothing to pull.", "OK");
				return;
			}

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
				EditorUtility.DisplayDialog("Pull from Sheets",
					$"No PO files found for group '{group}'.", "OK");
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
					parsedPoFiles[filePath] = poFile;
					poLookups[filePath]     = poFile.BuildLookup();
				}
			}

			// Walk every processed entry and update the corresponding PO entries.
			var dirtyFiles   = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			int updatedCount = 0;

			foreach (var entry in processedLoca.Entries)
			{
				if (string.IsNullOrEmpty(entry.LanguageId))
					continue;

				string lang = entry.LanguageId.Trim().ToLowerInvariant();
				if (!langToFilePath.TryGetValue(lang, out string filePath))
					continue;

				var lookup = poLookups[filePath];

				foreach (var (ctxLang, ctxPrefix, ctxPostfix) in langContextKeys)
				{
					if (!string.Equals(ctxLang, lang, StringComparison.OrdinalIgnoreCase))
						continue;

					string baseEffectiveKey = ReverseKeyAffixes(entry.Key, ctxPrefix, ctxPostfix);
					string rawMsgId         = ReverseKeyAffixes(baseEffectiveKey, keyColDesc);
					string msgctxt          = string.IsNullOrEmpty(ctxPrefix) ? null : ctxPrefix;
					string composedKey      = string.IsNullOrEmpty(msgctxt)
						? rawMsgId
						: $"{msgctxt}\u0004{rawMsgId}";

					if (!lookup.TryGetValue(composedKey, out var poEntry))
						continue;

					if (MergeTranslationIntoPoEntry(entry, poEntry))
					{
						dirtyFiles.Add(filePath);
						updatedCount++;
					}
				}
			}

			if (dirtyFiles.Count == 0)
			{
				EditorUtility.DisplayDialog("Pull from Sheets",
					"No new translations found to merge. All PO entries already have translations.", "OK");
				return;
			}

			foreach (string filePath in dirtyFiles)
			{
				PoBackupManager.CreateBackup(filePath);
				string serialized = parsedPoFiles[filePath].Serialize();
				File.WriteAllText(filePath, serialized, new UTF8Encoding(false));
			}

			AssetDatabase.Refresh();

			EditorUtility.DisplayDialog("Pull from Sheets",
				$"Merged {updatedCount} translation(s) from the sheet into {dirtyFiles.Count} PO file(s).", "OK");
			UiLog.Log($"{nameof(LocaGettextSheetsSyncer)}: Pulled {updatedCount} translation(s) into {dirtyFiles.Count} PO file(s).");
		}

		/// <summary>
		/// Appends keys from local PO files that are not yet present in the Google Sheet.
		/// Translation cells for new rows are left empty so translators can fill them in.
		/// Never modifies existing sheet cells.
		/// Only runs when <see cref="LocaExcelBridge.CanPush"/> is <c>true</c> and the source type is GoogleDocs.
		/// </summary>
		/// <param name="_bridge">The bridge asset to push new keys to.</param>
		public static void PushToSheets(LocaExcelBridge _bridge)
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

			string token = GoogleServiceAccountAuth.GetAccessToken(_bridge.EdServiceAccountJsonPath, _writeAccess: true);
			if (token == null)
			{
				EditorUtility.DisplayDialog("Push new keys",
					"Failed to obtain a Google auth token. Check the service account JSON path.", "OK");
				return;
			}

			// Find the key column index in the bridge config.
			int keyColIdx = -1;
			for (int i = 0; i < _bridge.NumColumns; i++)
			{
				var d = _bridge.GetColumnDescription(i);
				if (d?.ColumnType == LocaExcelBridge.EInColumnType.Key)
				{
					keyColIdx = i;
					break;
				}
			}

			if (keyColIdx < 0)
			{
				EditorUtility.DisplayDialog("Push new keys",
					"No Key column configured in the bridge.", "OK");
				return;
			}

			// Download current sheet content to discover existing keys.
			string sheetName;
			try
			{
				sheetName = GetFirstSheetName(spreadsheetId, token);
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

			// Collect all active msgids from PO files.
			string group   = _bridge.EdGroup;
			var poFiles = LocaCsvExporter.FindPoFiles(group);

			var allMsgIds  = new HashSet<string>(StringComparer.Ordinal);
			var msgIdOrder = new List<string>();

			foreach (var (_, _, filePath) in poFiles)
			{
				string fileContent = File.ReadAllText(filePath, Encoding.UTF8);
				var poFile = PoFile.Parse(fileContent);

				foreach (var entry in poFile.Entries)
				{
					if (entry.IsObsolete || string.IsNullOrEmpty(entry.MsgId))
						continue;

					if (allMsgIds.Add(entry.MsgId))
						msgIdOrder.Add(entry.MsgId);
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

			// Build rows: key cell = msgid, all other cells empty.
			int numCols  = _bridge.NumColumns;
			var newRows  = new List<List<string>>(newKeys.Count);

			foreach (string msgId in newKeys)
			{
				var row = new List<string>(numCols);
				for (int c = 0; c < numCols; c++)
					row.Add(string.Empty);

				row[keyColIdx] = msgId;
				newRows.Add(row);
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

				AppendSheetRows(spreadsheetId, token, sheetName, newRows);
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
		}

		// -----------------------------------------------------------------------
		// Internal helpers exposed for testing
		// -----------------------------------------------------------------------

		/// <summary>
		/// Builds a canonical column list from parsed PO file data.
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
		/// Conservatively merges a <see cref="ProcessedLocaEntry"/> into a <see cref="PoEntry"/>.
		/// Only fills cells that are currently empty — existing translations are preserved.
		/// </summary>
		/// <returns><c>true</c> if any field was updated; <c>false</c> if nothing changed.</returns>
		internal static bool MergeTranslationIntoPoEntry(ProcessedLocaEntry _entry, PoEntry _poEntry)
		{
			bool modified = false;

			if (!string.IsNullOrEmpty(_entry.Text) && string.IsNullOrEmpty(_poEntry.MsgStr))
			{
				_poEntry.MsgStr = _entry.Text;
				modified = true;
			}

			if (_entry.Forms != null)
			{
				for (int i = 0; i < _entry.Forms.Length; i++)
				{
					if (string.IsNullOrEmpty(_entry.Forms[i]))
						continue;

					if (_poEntry.MsgStrForms == null)
						_poEntry.MsgStrForms = new string[Math.Max(2, i + 1)];
					else if (_poEntry.MsgStrForms.Length <= i)
						Array.Resize(ref _poEntry.MsgStrForms, i + 1);

					if (string.IsNullOrEmpty(_poEntry.MsgStrForms[i]))
					{
						_poEntry.MsgStrForms[i] = _entry.Forms[i];
						modified = true;
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
		// Sheets API helpers
		// -----------------------------------------------------------------------

		/// <summary>
		/// Returns the title of the first sheet in the spreadsheet by querying its metadata.
		/// Falls back to <see cref="DEFAULT_SHEET_NAME"/> if the title cannot be determined.
		/// </summary>
		private static string GetFirstSheetName(string _spreadsheetId, string _token)
		{
			string url = $"https://sheets.googleapis.com/v4/spreadsheets/{_spreadsheetId}?fields=sheets.properties.title";

			using var client = new HttpClient();
			client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token);

			var response = client.GetAsync(url).GetAwaiter().GetResult();
			string body  = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

			if (!response.IsSuccessStatusCode)
				throw new InvalidOperationException($"Sheets API GET metadata error {(int)response.StatusCode}: {body}");

			// Body looks like: {"sheets":[{"properties":{"title":"Tabelle1"}},...]}
			int titleIdx = body.IndexOf("\"title\"", StringComparison.Ordinal);
			if (titleIdx < 0)
				return DEFAULT_SHEET_NAME;

			int colonIdx = body.IndexOf(':', titleIdx);
			if (colonIdx < 0)
				return DEFAULT_SHEET_NAME;

			int quoteStart = body.IndexOf('"', colonIdx + 1);
			if (quoteStart < 0)
				return DEFAULT_SHEET_NAME;

			int pos = quoteStart;
			return ParseJsonString(body, ref pos);
		}

		private static List<List<string>> GetSheetValues(string _spreadsheetId, string _token, string _sheetName)
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

		private static void AppendSheetRows(string _spreadsheetId, string _token, string _sheetName, List<List<string>> _rows)
		{
			string url =
				$"https://sheets.googleapis.com/v4/spreadsheets/{_spreadsheetId}/values/{Uri.EscapeDataString(_sheetName)}:append" +
				"?valueInputOption=USER_ENTERED&insertDataOption=INSERT_ROWS";

			string json = BuildValuesJson(_rows);

			using var client = new HttpClient();
			client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _token);

			var content  = new StringContent(json, Encoding.UTF8, "application/json");
			var response = client.PostAsync(url, content).GetAwaiter().GetResult();
			string body  = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

			if (!response.IsSuccessStatusCode)
				throw new InvalidOperationException($"Sheets API append error {(int)response.StatusCode}: {body}");
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
