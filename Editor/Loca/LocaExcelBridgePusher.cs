using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Editor
{
	/// <summary>
	/// Editor-side implementation of the "Push to Spreadsheet" feature for <see cref="LocaExcelBridge"/>.
	/// Registers a push callback on load so that <see cref="LocaExcelBridge.PushToSpreadsheet"/> can
	/// delegate the actual work to this class without creating a direct runtime → editor assembly dependency.
	/// </summary>
	[InitializeOnLoad]
	public static class LocaExcelBridgePusher
	{
		static LocaExcelBridgePusher()
		{
			LocaExcelBridge.s_pushCallback = Push;
		}

		// -----------------------------------------------------------------------
		// Entry point
		// -----------------------------------------------------------------------

		private static void Push(LocaExcelBridge _bridge)
		{
			if (_bridge == null)
				return;

			if (_bridge.EdSourceType == LocaExcelBridge.SourceType.GoogleDocs)
				PushToGoogleSheets(_bridge);
			else
				ExportToCsvAlongside(_bridge);
		}

		// -----------------------------------------------------------------------
		// Google Sheets push
		// -----------------------------------------------------------------------

		private static void PushToGoogleSheets(LocaExcelBridge _bridge)
		{
			string spreadsheetId = ExtractSpreadsheetId(_bridge.EdGoogleUrl);
			if (string.IsNullOrEmpty(spreadsheetId))
			{
				UiLog.LogError($"{nameof(LocaExcelBridgePusher)}: Could not extract spreadsheet ID from URL: {_bridge.EdGoogleUrl}");
				return;
			}

			string token = GoogleServiceAccountAuth.GetAccessToken(_bridge.EdServiceAccountJsonPath, _writeAccess: true);
			if (token == null)
			{
				UiLog.LogError($"{nameof(LocaExcelBridgePusher)}: Failed to obtain write-scope auth token.");
				return;
			}

			var rows = BuildRows(_bridge);
			if (rows == null)
				return;

			int startRow = _bridge.EdStartRow; // 0-based; Sheets API uses 1-based rows
			string rangeStart = $"A{startRow + 1}";
			string rangeEnd   = $"{ColToLetter(rows[0].Count - 1)}{startRow + rows.Count}";
			string range      = $"{rangeStart}:{rangeEnd}";

			string json = BuildBatchUpdateJson(range, rows);
			string url  = $"https://sheets.googleapis.com/v4/spreadsheets/{spreadsheetId}/values:batchUpdate";

			try
			{
				using var client = new HttpClient();
				client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
				var content  = new StringContent(json, Encoding.UTF8, "application/json");
				var response = client.PostAsync(url, content).GetAwaiter().GetResult();
				string responseBody = response.Content.ReadAsStringAsync().GetAwaiter().GetResult();

				if (!response.IsSuccessStatusCode)
				{
					UiLog.LogError($"{nameof(LocaExcelBridgePusher)}: Sheets API error {(int)response.StatusCode}: {responseBody}");
					return;
				}

				UiLog.Log($"{nameof(LocaExcelBridgePusher)}: Successfully pushed {rows.Count} row(s) to spreadsheet '{spreadsheetId}'.");
				EditorUtility.DisplayDialog("Push Complete",
					$"Successfully pushed {rows.Count} row(s) to Google Sheets.", "OK");
			}
			catch (Exception ex)
			{
				UiLog.LogError($"{nameof(LocaExcelBridgePusher)}: HTTP request failed: {ex.Message}");
			}
		}

		// -----------------------------------------------------------------------
		// Local CSV export (alongside XLSX)
		// -----------------------------------------------------------------------

		private static void ExportToCsvAlongside(LocaExcelBridge _bridge)
		{
			string xlsxPath = _bridge.EdExcelPath;
			if (string.IsNullOrEmpty(xlsxPath))
			{
				UiLog.LogError($"{nameof(LocaExcelBridgePusher)}: No local Excel path configured.");
				return;
			}

			var rows = BuildRows(_bridge);
			if (rows == null)
				return;

			string csvPath = Path.ChangeExtension(xlsxPath, ".csv");
			var sb = new StringBuilder();
			foreach (var row in rows)
				sb.AppendLine(ToCsvRow(row));

			File.WriteAllText(csvPath, sb.ToString(), new UTF8Encoding(false));
			AssetDatabase.Refresh();

			UiLog.Log($"{nameof(LocaExcelBridgePusher)}: Exported {rows.Count} row(s) to '{csvPath}'.");
			EditorUtility.DisplayDialog("Push Complete",
				$"Successfully exported {rows.Count} row(s) to:\n{csvPath}", "OK");
		}

		// -----------------------------------------------------------------------
		// Row builder — shared by both push paths
		// -----------------------------------------------------------------------

		/// <summary>
		/// Builds a list of rows (one list-of-strings per row) matching the bridge's
		/// <see cref="LocaExcelBridge.GetColumnDescription"/> column layout.
		/// </summary>
		private static List<List<string>> BuildRows(LocaExcelBridge _bridge)
		{
			int numCols = _bridge.NumColumns;
			if (numCols == 0)
			{
				UiLog.LogError($"{nameof(LocaExcelBridgePusher)}: No column descriptions configured.");
				return null;
			}

			// Identify key column and language columns.
			int keyColIdx = -1;
			LocaExcelBridge.InColumnDescription keyColDesc = null;
			var langCols = new List<(int colIdx, string lang, int pluralForm, LocaExcelBridge.InColumnDescription desc)>();

			for (int c = 0; c < numCols; c++)
			{
				var desc = _bridge.GetColumnDescription(c);
				if (desc == null)
					continue;

				if (desc.ColumnType == LocaExcelBridge.EInColumnType.Key)
				{
					keyColIdx = c;
					keyColDesc = desc;
				}
				else if (desc.ColumnType == LocaExcelBridge.EInColumnType.LanguageTranslation
				         && !string.IsNullOrWhiteSpace(desc.LanguageId))
				{
					langCols.Add((c, desc.LanguageId.Trim().ToLowerInvariant(), desc.PluralForm, desc));
				}
			}

			if (keyColIdx < 0)
			{
				UiLog.LogError($"{nameof(LocaExcelBridgePusher)}: No Key column configured.");
				return null;
			}

			if (langCols.Count == 0)
			{
				UiLog.LogWarning($"{nameof(LocaExcelBridgePusher)}: No language columns configured — nothing to push.");
				return null;
			}

			// Load PO files for each distinct language configured in the bridge.
			string group     = _bridge.EdGroup;
			var distinctLangs = langCols.Select(lc => lc.lang).Distinct().ToList();

			var langLookup = new Dictionary<string, Dictionary<string, PoEntry>>(StringComparer.OrdinalIgnoreCase);
			foreach (string lang in distinctLangs)
			{
				var lookup = LoadPoLookup(lang, group);
				if (lookup != null)
					langLookup[lang] = lookup;
				else
					UiLog.LogWarning($"{nameof(LocaExcelBridgePusher)}: No PO file found for language '{lang}', group '{group}'.");
			}

			if (langLookup.Count == 0)
			{
				UiLog.LogError($"{nameof(LocaExcelBridgePusher)}: No PO data loaded. Nothing to push.");
				return null;
			}

			// Collect all unique base keys from all languages.
			// We pick the first available language column for each entry to reverse affixes.
			var baseKeySet   = new HashSet<string>(StringComparer.Ordinal);
			var baseKeyOrder = new List<string>();

			// Use the first language column's desc for affix reversal (covers the common case
			// where all language columns share identical or empty affix settings).
			var refLangCol = langCols[0];

			foreach (var (lang, lookup) in langLookup)
			{
				// Find the lang column desc for this lang (first match).
				var lc = langCols.FirstOrDefault(x => string.Equals(x.lang, lang, StringComparison.OrdinalIgnoreCase));
				var langDesc = lc.desc ?? refLangCol.desc;

				foreach (string effectiveKey in lookup.Keys)
				{
					string baseKey = ReverseKeyAffixes(ReverseKeyAffixes(effectiveKey, langDesc), keyColDesc);
					if (baseKeySet.Add(baseKey))
						baseKeyOrder.Add(baseKey);
				}
			}

			baseKeyOrder.Sort(StringComparer.Ordinal);

			// Build each data row.
			var rows = new List<List<string>>(baseKeyOrder.Count);

			foreach (string baseKey in baseKeyOrder)
			{
				var row = new List<string>(numCols);
				for (int c = 0; c < numCols; c++)
					row.Add(string.Empty);

				row[keyColIdx] = baseKey;

				// Apply key-column affixes to get the intermediate effective key.
				string baseEffectiveKey = ApplyKeyAffixes(baseKey, keyColDesc);

				foreach (var (colIdx, lang, pluralForm, langDesc) in langCols)
				{
					string effectiveKey = ApplyKeyAffixes(baseEffectiveKey, langDesc);

					if (!langLookup.TryGetValue(lang, out var lookup))
						continue;

					if (!lookup.TryGetValue(effectiveKey, out var entry))
						continue;

					if (pluralForm < 0)
					{
						// Singular column.
						row[colIdx] = entry.MsgStr ?? string.Empty;
					}
					else if (entry.MsgStrForms != null && pluralForm < entry.MsgStrForms.Length)
					{
						// Plural-form column — explicit plural forms present.
						row[colIdx] = entry.MsgStrForms[pluralForm] ?? string.Empty;
					}
					else if (pluralForm == 0)
					{
						// Plural-form[0] requested but entry has no plural forms (singular-only PO entry).
						// Fall back to the singular msgstr so that non-pluralised translations are not lost.
						row[colIdx] = entry.MsgStr ?? string.Empty;
					}
				}

				rows.Add(row);
			}

			return rows;
		}

		// -----------------------------------------------------------------------
		// PO file loading
		// -----------------------------------------------------------------------

		private static Dictionary<string, PoEntry> LoadPoLookup(string _lang, string _group)
		{
			string groupSuffix = string.IsNullOrEmpty(_group) ? string.Empty : $"_{_group}";
			string baseName    = $"{_lang}{groupSuffix}";

			// Search all Resources directories under Assets (supports sub-folders like __Funatics/Resources/).
			foreach (string resourcesPath in FindResourcesDirectories())
			{
				// Try .po.txt first (preferred Unity TextAsset import), then .po.
				string path = Path.Combine(resourcesPath, baseName + ".po.txt");
				if (!File.Exists(path))
					path = Path.Combine(resourcesPath, baseName + ".po");
				if (!File.Exists(path))
					continue;

				string content = File.ReadAllText(path, Encoding.UTF8);
				var poFile = PoFile.Parse(content);

				var lookup = new Dictionary<string, PoEntry>(StringComparer.Ordinal);
				foreach (var entry in poFile.Entries)
				{
					if (entry.IsObsolete)
						continue;
					// Key by MsgId (LocaExcelBridge does not use msgctxt).
					if (!lookup.ContainsKey(entry.MsgId))
						lookup[entry.MsgId] = entry;
				}

				return lookup;
			}

			return null;
		}

		/// <summary>
		/// Enumerates all "Resources" directories under <see cref="Application.dataPath"/>,
		/// yielding the root <c>Assets/Resources</c> first for backward compatibility,
		/// followed by all nested ones (e.g. <c>Assets/__Funatics/Resources</c>).
		/// </summary>
		private static IEnumerable<string> FindResourcesDirectories()
		{
			string dataPath = Application.dataPath;
			string root     = Path.Combine(dataPath, "Resources");

			// Root first (backward compat).
			if (Directory.Exists(root))
				yield return root;

			// Then all nested Resources folders.
			if (Directory.Exists(dataPath))
			{
				foreach (string dir in Directory.GetDirectories(dataPath, "Resources", SearchOption.AllDirectories))
				{
					if (!string.Equals(dir, root, StringComparison.OrdinalIgnoreCase))
						yield return dir;
				}
			}
		}

		// -----------------------------------------------------------------------
		// Affix helpers (mirrors LocaExcelBridge logic, kept private here)
		// -----------------------------------------------------------------------

		private static string ApplyKeyAffixes(string _key, LocaExcelBridge.InColumnDescription _desc)
		{
			if (_desc == null)
				return _key ?? string.Empty;

			string prefix  = _desc.KeyPrefix  ?? string.Empty;
			string postfix = _desc.KeyPostfix ?? string.Empty;
			return string.Concat(prefix, _key ?? string.Empty, postfix);
		}

		private static string ReverseKeyAffixes(string _effectiveKey, LocaExcelBridge.InColumnDescription _desc)
		{
			if (_desc == null)
				return _effectiveKey ?? string.Empty;

			string result  = _effectiveKey ?? string.Empty;
			string prefix  = _desc.KeyPrefix  ?? string.Empty;
			string postfix = _desc.KeyPostfix ?? string.Empty;

			if (prefix.Length  > 0 && result.StartsWith(prefix,  StringComparison.Ordinal))
				result = result.Substring(prefix.Length);
			if (postfix.Length > 0 && result.EndsWith(postfix, StringComparison.Ordinal))
				result = result.Substring(0, result.Length - postfix.Length);

			return result;
		}

		// -----------------------------------------------------------------------
		// Google Sheets ID extraction
		// -----------------------------------------------------------------------

		private static string ExtractSpreadsheetId(string _url)
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

		// -----------------------------------------------------------------------
		// JSON / HTTP helpers
		// -----------------------------------------------------------------------

		private static string BuildBatchUpdateJson(string _range, List<List<string>> _rows)
		{
			var sb = new StringBuilder();
			sb.Append("{\"valueInputOption\":\"USER_ENTERED\",\"data\":[{\"range\":\"");
			sb.Append(JsonEscape(_range));
			sb.Append("\",\"majorDimension\":\"ROWS\",\"values\":[");

			for (int r = 0; r < _rows.Count; r++)
			{
				if (r > 0)
					sb.Append(',');

				sb.Append('[');
				var row = _rows[r];
				for (int c = 0; c < row.Count; c++)
				{
					if (c > 0)
						sb.Append(',');
					sb.Append('"');
					sb.Append(JsonEscape(row[c]));
					sb.Append('"');
				}
				sb.Append(']');
			}

			sb.Append("]}]}");
			return sb.ToString();
		}

		/// <summary>Converts a zero-based column index to a Sheets-style letter (A, B, …, Z, AA, …).</summary>
		private static string ColToLetter(int _col)
		{
			string result = string.Empty;
			int n = _col + 1; // 1-based
			while (n > 0)
			{
				n--;
				result = (char)('A' + n % 26) + result;
				n /= 26;
			}
			return result;
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
		// CSV helpers (used for local XLSX path)
		// -----------------------------------------------------------------------

		private static string ToCsvRow(IEnumerable<string> _fields)
			=> string.Join(",", _fields.Select(CsvField));

		private static string CsvField(string _value)
		{
			if (string.IsNullOrEmpty(_value))
				return string.Empty;

			bool needsQuoting = _value.IndexOfAny(new[] { ',', '"', '\n', '\r' }) >= 0;
			if (!needsQuoting)
				return _value;

			return "\"" + _value.Replace("\"", "\"\"") + "\"";
		}
	}
}
