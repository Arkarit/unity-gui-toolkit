using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Editor
{
	/// <summary>
	/// Editor tool that exports all PO translation files to a single CSV file in a format
	/// compatible with <see cref="LocaExcelBridge"/> column layout.
	/// Columns: Key, Context, then one singular column per language, plus plural-form columns
	/// when any entry carries plural forms.
	/// </summary>
	public static class LocaCsvExporter
	{
		[MenuItem("Gui Toolkit/Localization/Export to CSV")]
		private static void ExportToCsvMenu()
		{
			string path = EditorUtility.SaveFilePanel("Export Translations to CSV", "", "translations.csv", "csv");
			if (string.IsNullOrEmpty(path))
				return;

			Export(path);
		}

		/// <summary>
		/// Exports PO translations to a CSV file at the given path.
		/// </summary>
		/// <param name="_outputPath">Absolute file-system path for the output CSV.</param>
		/// <param name="_group">
		/// Group name to export, or <c>null</c> to export all groups.
		/// Pass an empty string to export only the default (ungrouped) PO files.
		/// </param>
		public static void Export(string _outputPath, string _group = null)
		{
			string csv = BuildCsv(_group);
			if (csv == null)
				return;

			File.WriteAllText(_outputPath, csv, new UTF8Encoding(false));
			AssetDatabase.Refresh();

			int keyCount = csv.Split('\n').Length - 2; // subtract header and trailing newline
			EditorUtility.DisplayDialog("CSV Export",
				$"Successfully exported {Math.Max(0, keyCount)} key(s) to:\n{_outputPath}", "OK");
		}

		/// <summary>
		/// Builds the full CSV content string for the given translation group.
		/// </summary>
		/// <param name="_group">
		/// Group name, or <c>null</c> to include all groups combined.
		/// </param>
		/// <returns>The CSV text ready to write to disk, or <c>null</c> if no PO data was found.</returns>
		public static string BuildCsv(string _group)
		{
			var poFiles = FindPoFiles(_group);
			if (poFiles.Count == 0)
			{
				UiLog.LogWarning("LocaCsvExporter: No PO files found in Resources.");
				return null;
			}

			// Parse every PO file; accumulate entries per language.
			var langEntries = new Dictionary<string, List<PoEntry>>(StringComparer.OrdinalIgnoreCase);
			foreach (var (lang, _, filePath) in poFiles)
			{
				string content = File.ReadAllText(filePath, Encoding.UTF8);
				var poFile = PoFile.Parse(content);

				if (!langEntries.TryGetValue(lang, out var list))
				{
					list = new List<PoEntry>();
					langEntries[lang] = list;
				}

				list.AddRange(poFile.Entries.Where(e => !e.IsObsolete));
			}

			if (langEntries.Count == 0)
			{
				UiLog.LogWarning("LocaCsvExporter: No entries found in PO files.");
				return null;
			}

			var languages = langEntries.Keys
				.OrderBy(l => l, StringComparer.OrdinalIgnoreCase)
				.ToList();

			// Build per-language lookup: ComposedKey -> PoEntry (first occurrence wins).
			var langLookup = new Dictionary<string, Dictionary<string, PoEntry>>(StringComparer.OrdinalIgnoreCase);
			foreach (var (lang, entries) in langEntries)
			{
				var lookup = new Dictionary<string, PoEntry>(StringComparer.Ordinal);
				foreach (var entry in entries)
				{
					if (!lookup.ContainsKey(entry.ComposedKey))
						lookup[entry.ComposedKey] = entry;
				}
				langLookup[lang] = lookup;
			}

			// Collect all unique ComposedKeys across all languages; determine max plural form count.
			var keyOrder = new List<string>();
			var keySet = new HashSet<string>(StringComparer.Ordinal);
			var keyInfo = new Dictionary<string, (string msgId, string csvContext)>(StringComparer.Ordinal);
			int maxPluralForms = 0;

			foreach (string lang in languages)
			{
				foreach (var entry in langEntries[lang])
				{
					string composed = entry.ComposedKey;
					if (keySet.Add(composed))
					{
						keyOrder.Add(composed);
						string csvCtx = entry.IsPlural
							? $"PLURAL:{entry.MsgIdPlural}"
							: (entry.Context ?? string.Empty);
						keyInfo[composed] = (entry.MsgId, csvCtx);
					}

					if (entry.MsgStrForms != null)
						maxPluralForms = Math.Max(maxPluralForms, entry.MsgStrForms.Length);
				}
			}

			keyOrder.Sort(StringComparer.Ordinal);

			// Header row.
			var header = new List<string> { "Key", "Context" };
			foreach (string lang in languages)
			{
				header.Add(lang);
				for (int i = 0; i < maxPluralForms; i++)
					header.Add($"{lang}[{i}]");
			}

			var sb = new StringBuilder();
			sb.AppendLine(ToCsvRow(header));

			// Data rows.
			foreach (string composed in keyOrder)
			{
				var (msgId, csvCtx) = keyInfo[composed];
				var row = new List<string> { msgId, csvCtx };

				foreach (string lang in languages)
				{
					langLookup[lang].TryGetValue(composed, out var entry);
					row.Add(entry?.MsgStr ?? string.Empty);

					for (int i = 0; i < maxPluralForms; i++)
					{
						string form =
							entry?.MsgStrForms != null && i < entry.MsgStrForms.Length
								? (entry.MsgStrForms[i] ?? string.Empty)
								: string.Empty;
						row.Add(form);
					}
				}

				sb.AppendLine(ToCsvRow(row));
			}

			return sb.ToString();
		}

		// -----------------------------------------------------------------------
		// Internal helpers
		// -----------------------------------------------------------------------

		/// <summary>
		/// Finds all *.po / *.po.txt files in the project's Resources folder,
		/// optionally filtered to a specific group.
		/// </summary>
		internal static List<(string lang, string group, string filePath)> FindPoFiles(string _group)
		{
			var result = new List<(string, string, string)>();
			string resourcesPath = Path.Combine(Application.dataPath, "Resources");
			if (!Directory.Exists(resourcesPath))
				return result;

			IEnumerable<string> allFiles =
				Directory.GetFiles(resourcesPath, "*.po", SearchOption.TopDirectoryOnly)
				.Concat(Directory.GetFiles(resourcesPath, "*.po.txt", SearchOption.TopDirectoryOnly));

			foreach (string filePath in allFiles)
			{
				string fileName = Path.GetFileName(filePath);
				string baseName;
				if (fileName.EndsWith(".po.txt", StringComparison.OrdinalIgnoreCase))
					baseName = fileName.Substring(0, fileName.Length - 7);
				else if (fileName.EndsWith(".po", StringComparison.OrdinalIgnoreCase))
					baseName = fileName.Substring(0, fileName.Length - 3);
				else
					continue;

				if (string.IsNullOrEmpty(baseName))
					continue;

				// Filename format: {lang} or {lang}_{group}.
				int underscoreIdx = baseName.IndexOf('_');
				string lang, fileGroup;
				if (underscoreIdx < 0)
				{
					lang = baseName;
					fileGroup = null;
				}
				else
				{
					lang = baseName.Substring(0, underscoreIdx);
					fileGroup = baseName.Substring(underscoreIdx + 1);
				}

				if (string.IsNullOrEmpty(lang))
					continue;

				// Apply group filter when requested.
				if (_group != null)
				{
					bool wantDefault = string.IsNullOrEmpty(_group);
					if (wantDefault && fileGroup != null)
						continue;
					if (!wantDefault && !string.Equals(fileGroup, _group, StringComparison.OrdinalIgnoreCase))
						continue;
				}

				result.Add((lang, fileGroup, filePath));
			}

			return result;
		}

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
