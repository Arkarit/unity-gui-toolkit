using System;
using System.Collections.Generic;
using UnityEngine;

#if UNITY_EDITOR
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using ExcelDataReader;
using UnityEditor;
#endif

namespace GuiToolkit
{
	[CreateAssetMenu(fileName = nameof(LocaExcelBridge), menuName = StringConstants.LOCA_EXCEL_BRIDGE)]
	public class LocaExcelBridge : ScriptableObject, ILocaProvider
	{
		public enum ColumnType
		{
			Ignore,
			Key,
			LanguageTranslation
		}

		[Serializable]
		public struct ColumnDescription
		{
			public ColumnType ColumnType;
			public string Prefix;
			public string Postfix;
		}

		[PathField(_isFolder:false, _relativeToPath:".", _extensions:"xlsx")]
		[SerializeField][Mandatory] private PathField m_excelPath;
		[SerializeField] private string m_group;
		[SerializeField] private List<ColumnDescription> m_columnDescriptions = new List<ColumnDescription>();

		// Runtime state
		private string m_currentLanguageId;
		private Dictionary<string, string> m_table; // key: group:key

		[Serializable]
		private class SerializableTable
		{
			public string group;
			public string[] languages;
			public Row[] rows;
		}

		[Serializable]
		private class Row
		{
			public string key;
			public Trans[] trans;
		}

		[Serializable]
		private class Trans
		{
			public string lang;
			public string text;
		}

		public void InitData(string _languageId)
		{
			LoadJsonIfNeeded();
			ChangeLanguage(_languageId);
		}

		public string Translate(string _s, string _group = null)
		{
			if (string.IsNullOrEmpty(_s))
				return string.Empty;

			if (m_table == null)
				LoadJsonIfNeeded();

			string group = string.IsNullOrEmpty(_group) ? m_group : _group;
			string k = MakeGroupedKey(group, _s);

			if (m_table != null && m_table.TryGetValue(k, out var v))
				return v ?? string.Empty;

			// Fallback: return key if not found
			return _s;
		}

		public string Translate(string _singularKey, string _pluralKey, int _n, string _group = null)
		{
			// Simple plural rule: n == 1 -> singular, else plural
			var key = (_n == 1 || string.IsNullOrEmpty(_pluralKey)) ? _singularKey : _pluralKey;
			return Translate(key, _group);
		}

		public void ChangeLanguage(string _languageId)
		{
			if (string.IsNullOrEmpty(_languageId))
				return;

			TextAsset ta = LoadTextAsset();
			if (ta == null)
			{
				m_currentLanguageId = _languageId;
				m_table = new Dictionary<string, string>();
				return;
			}

			var table = JsonUtility.FromJson<SerializableTable>(ta.text);
			m_currentLanguageId = _languageId;
			m_table = BuildTable(table, m_currentLanguageId);
		}

#if UNITY_EDITOR
		// Converts xlsx to JSON and stores under Resources/m_resourcesSubPath/<assetName>.json
		public void CollectData()
		{
			if (m_excelPath == null || string.IsNullOrEmpty(m_excelPath.Path))
			{
				Debug.LogError($"{nameof(LocaExcelBridge)}: Excel path is not set.");
				return;
			}

			string xlsxPath = m_excelPath.Path;
			if (!File.Exists(xlsxPath))
			{
				Debug.LogError($"{nameof(LocaExcelBridge)}: Excel file not found: {xlsxPath}");
				return;
			}

			// Ensure codepages for legacy encodings (harmless for pure .xlsx)
			Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

			using var fs = File.Open(xlsxPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
			using var reader = ExcelReaderFactory.CreateReader(fs);
			DataSet ds = reader.AsDataSet();
			if (ds.Tables.Count == 0)
			{
				Debug.LogError($"{nameof(LocaExcelBridge)}: No worksheets found in {xlsxPath}");
				return;
			}

			var sheet = ds.Tables[0];
			if (sheet.Rows.Count == 0)
			{
				Debug.LogWarning($"{nameof(LocaExcelBridge)}: Worksheet is empty.");
				WriteJson(new SerializableTable
				{
					group = m_group,
					languages = Array.Empty<string>(),
					rows = Array.Empty<Row>()
				});
				return;
			}

			// Header detection
			var colCount = sheet.Columns.Count;
			var headers = new string[colCount];
			for (int c = 0; c < colCount; c++)
				headers[c] = sheet.Rows[0][c]?.ToString()?.Trim() ?? string.Empty;

			// If user did not configure columns, infer: col0 = Key, others = LanguageTranslation
			if (m_columnDescriptions == null || m_columnDescriptions.Count != colCount)
			{
				m_columnDescriptions = new List<ColumnDescription>(colCount);
				for (int c = 0; c < colCount; c++)
				{
					m_columnDescriptions.Add(new ColumnDescription
					{
						ColumnType = (c == 0) ? ColumnType.Key : ColumnType.LanguageTranslation,
						Prefix = string.Empty,
						Postfix = string.Empty
					});
				}
			}

			// Collect language ids from header for LanguageTranslation columns
			var langColumns = new List<(int col, string lang, string prefix, string postfix)>();
			int keyCol = -1;

			for (int c = 0; c < colCount; c++)
			{
				var desc = m_columnDescriptions[c];
				switch (desc.ColumnType)
				{
					case ColumnType.Key:
						keyCol = c;
						break;

					case ColumnType.LanguageTranslation:
					{
						string langId = headers[c];
						if (string.IsNullOrEmpty(langId))
						{
							Debug.LogWarning($"{nameof(LocaExcelBridge)}: Empty language id in header at column {c}. Skipping.");
							continue;
						}
						langColumns.Add((c, langId, desc.Prefix ?? string.Empty, desc.Postfix ?? string.Empty));
						break;
					}
				}
			}

			if (keyCol < 0)
			{
				Debug.LogError($"{nameof(LocaExcelBridge)}: No key column defined or inferred.");
				return;
			}

			var rows = new List<Row>(Math.Max(0, sheet.Rows.Count - 1));
			for (int r = 1; r < sheet.Rows.Count; r++)
			{
				string key = sheet.Rows[r][keyCol]?.ToString()?.Trim();
				if (string.IsNullOrEmpty(key))
					continue;

				var trans = new List<Trans>(langColumns.Count);
				foreach (var lc in langColumns)
				{
					string raw = sheet.Rows[r][lc.col]?.ToString() ?? string.Empty;
					string val = string.Concat(lc.prefix, raw, lc.postfix);
					trans.Add(new Trans { lang = lc.lang, text = val });
				}

				rows.Add(new Row { key = key, trans = trans.ToArray() });
			}

			var serializable = new SerializableTable
			{
				group = m_group,
				languages = langColumns.Select(x => x.lang).Distinct().ToArray(),
				rows = rows.ToArray()
			};

			WriteJson(serializable);
			AssetDatabase.Refresh();
		}

		private void WriteJson(SerializableTable _table)
		{
			string assetName = string.IsNullOrEmpty(name) ? "LocaTable" : name;
			string relDir = $"Assets/Resources/{LocaProviderList.RESOURCES_SUB_PATH}";
			Directory.CreateDirectory(relDir);
			string outPath = Path.Combine(relDir, assetName + ".json");

			// Pretty JSON via JsonUtility workaround (it has no pretty mode for nested arrays reliably).
			// Keep it simple: JsonUtility.ToJson with pretty = true is acceptable here.
			string json = JsonUtility.ToJson(_table, true);
			File.WriteAllText(outPath, json, new UTF8Encoding(false));
			Debug.Log($"{nameof(LocaExcelBridge)}: Wrote JSON -> {outPath}");
		}
#endif

		private void LoadJsonIfNeeded()
		{
			if (m_table != null)
				return;

			TextAsset ta = LoadTextAsset();
			if (!ta)
			{
				m_table = new Dictionary<string, string>();
				return;
			}

			var table = JsonUtility.FromJson<SerializableTable>(ta.text);
			// Keep last selected language or first available
			string lang = string.IsNullOrEmpty(m_currentLanguageId)
				? (table.languages != null && table.languages.Length > 0 ? table.languages[0] : "en")
				: m_currentLanguageId;

			m_table = BuildTable(table, lang);
		}

		private TextAsset LoadTextAsset()
		{
			string assetName = string.IsNullOrEmpty(name) ? "LocaTable" : name;
			string resPath = string.IsNullOrEmpty(LocaProviderList.RESOURCES_SUB_PATH)
				? assetName
				: $"{LocaProviderList.RESOURCES_SUB_PATH}/{assetName}";
			return Resources.Load<TextAsset>(resPath);
		}

		private Dictionary<string, string> BuildTable(SerializableTable _table, string _lang)
		{
			var dict = new Dictionary<string, string>(StringComparer.Ordinal);
			if (_table == null || _table.rows == null)
				return dict;

			foreach (var row in _table.rows)
			{
				if (row == null || string.IsNullOrEmpty(row.key))
					continue;

				string value = null;

				// Exact language
				if (row.trans != null)
				{
					for (int i = 0; i < row.trans.Length; i++)
					{
						if (row.trans[i] != null && string.Equals(row.trans[i].lang, _lang, StringComparison.Ordinal))
						{
							value = row.trans[i].text;
							break;
						}
					}

					// Fallback to first available if exact not found
					if (value == null && row.trans.Length > 0 && row.trans[0] != null)
						value = row.trans[0].text;
				}

				string grouped = MakeGroupedKey(_table.group, row.key);
				dict[grouped] = value ?? row.key;
			}

			return dict;
		}

		private static string MakeGroupedKey(string _group, string _key)
		{
			// Use "group:key" to avoid accidental collisions across groups
			if (string.IsNullOrEmpty(_group))
				return _key ?? string.Empty;

			return string.Concat(_group, ":", _key ?? string.Empty);
		}
	}
}
