#if UNITY_6000_0_OR_NEWER
#define UITK_USE_ROSLYN
#endif

using System;
using System.Collections.Generic;
using GuiToolkit.Exceptions;
using UnityEngine;

#if UNITY_EDITOR
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
#if UITK_USE_ROSLYN
using ExcelDataReader;
#endif
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
			LanguageTranslation,
		}

		[Serializable]
		public class ColumnDescription
		{
			public ColumnType ColumnType;
			public string LanguageId;   // manually assigned per language column
			public string KeyPrefix;
			public string KeyPostfix;
			// -1: singular (no plurals); 0..5: plural form index
			public int PluralForm = -1;
		}

		[PathField(_isFolder: false, _relativeToPath: ".", _extensions: "xlsx")]
		[SerializeField][Mandatory] private PathField m_excelPath;
		[SerializeField] private string m_group;
		[SerializeField] private List<ColumnDescription> m_columnDescriptions = new();
		[SerializeField] private int m_startRow = 0; // all rows before are ignored (0-based index)

		private LocaJson m_cached; // loaded at runtime from Resources

		public LocaJson Localization
		{
			get
			{
				LoadJsonIfNeeded();
				return m_cached;
			}
		}
		
		public int NumColumns => m_columnDescriptions.Count;
		
		public string GetKey(string _key, int _column)
		{
			if (!_column.IsInRange(0, NumColumns))
				return null;
			
			var description = GetColumnDescription(_column);
			if (description.ColumnType != ColumnType.LanguageTranslation)
				return null;
				
			return $"{description.KeyPrefix}{_key}{description.KeyPostfix}";
		}

		public ColumnDescription GetColumnDescription(int _column)
		{
			if (_column < 0 || _column >= m_columnDescriptions.Count)
				return null;
			
			return m_columnDescriptions[_column];
		}


#if UNITY_EDITOR
		public void CollectData()
		{
#if UITK_USE_ROSLYN
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
			if (sheet.Rows.Count <= m_startRow)
			{
				Debug.LogWarning($"{nameof(LocaExcelBridge)}: Worksheet has no data rows after start row {m_startRow}.");
				WriteJson(new LocaJson { Group = m_group, Entries = new List<LocaJsonEntry>() });
				return;
			}

			int colCount = sheet.Columns.Count;

			// Infer column config if not matching column count
			if (m_columnDescriptions == null || m_columnDescriptions.Count != colCount)
			{
				m_columnDescriptions = new List<ColumnDescription>(colCount);
				for (int c = 0; c < colCount; c++)
				{
					m_columnDescriptions.Add(new ColumnDescription
					{
						ColumnType = (c == 0) ? ColumnType.Key : ColumnType.LanguageTranslation,
						KeyPrefix = string.Empty,
						KeyPostfix = string.Empty,
						LanguageId = string.Empty,
						PluralForm = -1
					});
				}
			}

			int keyCol = -1;
			ColumnDescription keyColDesc = null;
			var langColumns = new List<(int col, string lang, ColumnDescription desc)>();

			for (int c = 0; c < colCount; c++)
			{
				var desc = m_columnDescriptions[c];
				if (desc == null)
					continue;

				switch (desc.ColumnType)
				{
					case ColumnType.Key:
						keyCol = c;
						keyColDesc = desc;
						break;

					case ColumnType.LanguageTranslation:
						string langId = NormalizeLang(desc.LanguageId);
						if (string.IsNullOrEmpty(langId))
						{
							Debug.LogWarning($"{nameof(LocaExcelBridge)}: Empty LanguageId at column {c}, skipping.");
							continue;
						}
						langColumns.Add((c, langId, desc));
						break;
				}
			}

			if (keyCol < 0)
			{
				Debug.LogError($"{nameof(LocaExcelBridge)}: No key column defined or inferred.");
				return;
			}

			var byLangAndKey = new Dictionary<(string lang, string key), LocaJsonEntry>(1024, StringTupleComparer.Ordinal);

			for (int r = m_startRow; r < sheet.Rows.Count; r++)
			{
				string baseKey = sheet.Rows[r][keyCol]?.ToString()?.Trim();
				if (string.IsNullOrEmpty(baseKey))
					continue;

				string baseEffectiveKey = ApplyKeyAffixes(baseKey, keyColDesc);

				foreach (var lc in langColumns)
				{
					string lang = lc.lang;
					string cell = sheet.Rows[r][lc.col]?.ToString();
					cell = cell != null ? cell.Trim() : string.Empty;

					int plural = lc.desc?.PluralForm ?? -1;

					// skip empty cells
					if (string.IsNullOrEmpty(cell))
						continue;

					string effectiveKey = ApplyKeyAffixes(baseEffectiveKey, lc.desc);

					var k = (lang, effectiveKey);
					if (!byLangAndKey.TryGetValue(k, out var entry))
					{
						entry = new LocaJsonEntry { LanguageId = lang, Key = effectiveKey };
						byLangAndKey[k] = entry;
					}

					if (plural < 0)
					{
						entry.Text = cell;
					}
					else
					{
						entry.Forms ??= new string[6];
						entry.Forms[plural] = cell;
					}
				}
			}

			var pruned = byLangAndKey.Values
				.Where(e =>
					!string.IsNullOrEmpty(e.Text) ||
					(e.Forms != null && e.Forms.Any(s => !string.IsNullOrEmpty(s)))
				)
				.ToList();

			var result = new LocaJson
			{
				Group = m_group,
				Entries = pruned
			};

			WriteJson(result);
			AssetDatabase.Refresh();
#else
			throw new RoslynUnavailableException();
#endif
		}
		
#if UITK_USE_ROSLYN
		private static string ApplyKeyAffixes( string _key, ColumnDescription _desc )
		{
			if (_desc == null)
				return _key ?? string.Empty;

			string prefix = _desc.KeyPrefix ?? string.Empty;
			string postfix = _desc.KeyPostfix ?? string.Empty;

			return string.Concat(prefix, _key ?? string.Empty, postfix);
		}

		private static string NormalizeLang( string _lang )
		{
			if (string.IsNullOrEmpty(_lang))
				return string.Empty;

			return _lang.Trim().ToLowerInvariant();
		}

		private void WriteJson( LocaJson _data )
		{
			string assetName = string.IsNullOrEmpty(name) ? "LocaTable" : name;
			string relDir = $"Assets/Resources/{LocaProviderList.RESOURCES_SUB_PATH}";
			Directory.CreateDirectory(relDir);
			string outPath = Path.Combine(relDir, assetName + ".json");

			string json = JsonUtility.ToJson(_data, true);
			File.WriteAllText(outPath, json, new UTF8Encoding(false));
			Debug.Log($"{nameof(LocaExcelBridge)}: Wrote JSON -> {outPath}");
		}
#endif
#endif

		private void LoadJsonIfNeeded()
		{
			if (m_cached != null)
				return;

			TextAsset ta = LoadTextAsset();
			if (!ta)
			{
				m_cached = new LocaJson { Group = m_group, Entries = new List<LocaJsonEntry>() };
				return;
			}

			m_cached = JsonUtility.FromJson<LocaJson>(ta.text);
			if (m_cached != null)
				return;
			
			m_cached = new LocaJson { Group = m_group, Entries = new List<LocaJsonEntry>() };
		}

		private TextAsset LoadTextAsset()
		{
			string assetName = string.IsNullOrEmpty(name) ? "LocaTable" : name;
			string resPath = $"{LocaProviderList.RESOURCES_SUB_PATH}{assetName}";
			return Resources.Load<TextAsset>(resPath);
		}

		private sealed class StringTupleComparer : IEqualityComparer<(string a, string b)>
		{
			public static readonly StringTupleComparer Ordinal = new StringTupleComparer();

			public bool Equals( (string a, string b) _x, (string a, string b) _y )
			{
				return string.Equals(_x.a, _y.a, StringComparison.Ordinal)
					   && string.Equals(_x.b, _y.b, StringComparison.Ordinal);
			}

			public int GetHashCode( (string a, string b) _obj )
			{
				int h1 = _obj.a != null ? _obj.a.GetHashCode() : 0;
				int h2 = _obj.b != null ? _obj.b.GetHashCode() : 0;
				unchecked { return (h1 * 397) ^ h2; }
			}
		}
	}
}
