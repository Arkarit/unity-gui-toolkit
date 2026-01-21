#if UNITY_6000_0_OR_NEWER
#define UITK_USE_ROSLYN
#endif

using System;
using System.Collections.Generic;
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
using UnityEngine.Networking;
#endif

namespace GuiToolkit
{
	[CreateAssetMenu(fileName = nameof(LocaExcelBridge), menuName = StringConstants.LOCA_EXCEL_BRIDGE)]
	public class LocaExcelBridge : ScriptableObject, ILocaProvider
	{
		public enum SourceType
		{
			Local,
			GoogleDocs,
		}
		
		public enum EInColumnType
		{
			Ignore,
			Key,
			LanguageTranslation,
		}

		[Serializable]
		public class InColumnDescription
		{
			[ReadOnly] public string Description; // only purpose is making better readable tables in editor
			public EInColumnType ColumnType;
			public string LanguageId;   // manually assigned per language column
			public string KeyPrefix;
			public string KeyPostfix;
			// -1: singular (no plurals); 0..5: plural form index
			public int PluralForm = -1;

			public void UpdateDescriptionField()
			{
				switch (ColumnType)
				{
					case EInColumnType.Ignore:
						Description = "Ignored";
						break;
					case EInColumnType.Key:
						Description = "Master Key";
						break;
					case EInColumnType.LanguageTranslation:
						Description = LanguageId;
						if (!string.IsNullOrEmpty(KeyPrefix))
							Description += $" prefix '{KeyPrefix}'";
						if (!string.IsNullOrEmpty(KeyPostfix))
							Description += $" postfix '{KeyPostfix}'";
						if (PluralForm == -1)
						{
							Description += " singular";
							break;
						}

						Description += $" plural {PluralForm}";
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}
			}
		}

		[Header("Input")]
		[Space]
		[SerializeField] private SourceType m_sourceType;
		[PathField(_isFolder: false, _relativeToPath: ".", _extensions: "xlsx")]
		[SerializeField][Optional] private PathField m_excelPath;
		[SerializeField][Optional] private string m_googleUrl;
		[SerializeField] private string m_group;
		[SerializeField] private List<InColumnDescription> m_columnDescriptions = new();
		[SerializeField] private int m_startRow = 0; // all rows before are ignored (0-based index)

		[Header("Output (Read-Only)")]
		[Space]
		[GuiToolkit.ReadOnly][SerializeField] private ProcessedLoca m_processedLoca;

		public ProcessedLoca Localization => m_processedLoca != null ? m_processedLoca : new ProcessedLoca();

		public int NumColumns => m_columnDescriptions.Count;

		public string GetKey( string _key, int _column )
		{
			if (!_column.IsInRange(0, NumColumns))
				return null;

			var description = GetColumnDescription(_column);
			if (description.ColumnType != EInColumnType.LanguageTranslation)
				return null;

			return $"{description.KeyPrefix}{_key}{description.KeyPostfix}";
		}

		public InColumnDescription GetColumnDescription( int _column )
		{
			if (_column < 0 || _column >= m_columnDescriptions.Count)
				return null;

			return m_columnDescriptions[_column];
		}


#if UNITY_EDITOR
		private void OnValidate()
		{
			if (m_columnDescriptions == null)
				return;

			foreach (var description in m_columnDescriptions)
				description.UpdateDescriptionField();
		}

		public void CollectData()
		{
#if UITK_USE_ROSLYN
			m_processedLoca = null;

			string xlsxPath = string.Empty;
			switch (m_sourceType)
			{
				case SourceType.Local:
					if (m_excelPath == null || string.IsNullOrEmpty(m_excelPath.Path))
					{
						UiLog.LogError($"{nameof(LocaExcelBridge)}: Excel path is not set.");
						return;
					}
					
					xlsxPath = m_excelPath.Path;
					break;
				
				case SourceType.GoogleDocs:
					xlsxPath = ProcessGoogleDocsUrl();
					if (string.IsNullOrEmpty(xlsxPath))
						return;
					
					break;
				
				default:
					throw new ArgumentOutOfRangeException();
			}

			if (!File.Exists(xlsxPath))
			{
				UiLog.LogError($"{nameof(LocaExcelBridge)}: Excel file not found: {xlsxPath}");
				return;
			}

			Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

			using var fs = File.Open(xlsxPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
			using var reader = ExcelReaderFactory.CreateReader(fs);
			DataSet ds = reader.AsDataSet();
			int tablesCount = ds.Tables.Count;
			if (tablesCount == 0)
			{
				UiLog.LogError($"{nameof(LocaExcelBridge)}: No worksheets found in {xlsxPath}");
				return;
			}

			var sheet = ds.Tables[0];
			int colCount = sheet.Columns.Count;

			for (int i = 1; i < tablesCount; i++)
			{
				var xlsxColCount = ds.Tables[i].Columns.Count;
				if (xlsxColCount < colCount)
				{
					UiLog.LogError($"{nameof(LocaExcelBridge)}: Column count  ({ds.Tables[i].Columns.Count}) too small for defined columns ({colCount})");
					return;
				}

				if (xlsxColCount > colCount)
					UiLog.LogWarning($"{nameof(LocaExcelBridge)}: Column count  ({ds.Tables[i].Columns.Count}) too large for defined columns ({colCount}). Ignored.");
			}

			if (sheet.Rows.Count <= m_startRow)
			{
				UiLog.LogError($"{nameof(LocaExcelBridge)}: Worksheet has no data rows after start row {m_startRow}.");
				return;
			}


			// Infer column config if not matching column count
			if (m_columnDescriptions == null || m_columnDescriptions.Count != colCount)
			{
				m_columnDescriptions = new List<InColumnDescription>(colCount);
				for (int c = 0; c < colCount; c++)
				{
					m_columnDescriptions.Add(new InColumnDescription
					{
						ColumnType = (c == 0) ? EInColumnType.Key : EInColumnType.LanguageTranslation,
						KeyPrefix = string.Empty,
						KeyPostfix = string.Empty,
						LanguageId = string.Empty,
						PluralForm = -1
					});
				}
			}

			int keyCol = -1;
			InColumnDescription keyColDesc = null;
			var langColumns = new List<(int col, string lang, InColumnDescription desc)>();

			for (int c = 0; c < colCount; c++)
			{
				var desc = m_columnDescriptions[c];
				if (desc == null)
					continue;

				switch (desc.ColumnType)
				{
					case EInColumnType.Key:
						keyCol = c;
						keyColDesc = desc;
						break;

					case EInColumnType.LanguageTranslation:
						string langId = NormalizeLang(desc.LanguageId);
						if (string.IsNullOrEmpty(langId))
						{
							UiLog.LogWarning($"{nameof(LocaExcelBridge)}: Empty LanguageId at column {c}, skipping.");
							continue;
						}
						langColumns.Add((c, langId, desc));
						break;
				}
			}

			if (keyCol < 0)
			{
				UiLog.LogError($"{nameof(LocaExcelBridge)}: No key column defined or inferred.");
				return;
			}

			var byLangAndKey = new Dictionary<(string lang, string key), ProcessedLocaEntry>(1024, StringTupleComparer.Ordinal);

			for (int i = 0; i < tablesCount; i++)
			{
				sheet = ds.Tables[i];
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
							entry = new ProcessedLocaEntry { LanguageId = lang, Key = effectiveKey };
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
			}

			var pruned = byLangAndKey.Values
				.Where(e =>
					!string.IsNullOrEmpty(e.Text) ||
					(e.Forms != null && e.Forms.Any(s => !string.IsNullOrEmpty(s)))
				)
				.ToList();

			m_processedLoca = new ProcessedLoca(m_group, pruned);

			EditorUtility.SetDirty(this);
			AssetDatabase.Refresh();
#else
			throw new RoslynUnavailableException();
#endif
		}


#if UITK_USE_ROSLYN
		private string ProcessGoogleDocsUrl()
		{
			string tempFile = Path.GetTempFileName();
			string path = NormalizeXlsxPath(m_googleUrl);
			if (string.IsNullOrWhiteSpace(path))
				return null;
			
			using (UnityWebRequest www = UnityWebRequest.Get(path))
			{
				var op = www.SendWebRequest();
				while (!op.isDone) { }

				if (www.result != UnityWebRequest.Result.Success)
				{
					UiLog.LogError($"{nameof(LocaExcelBridge)}: Failed to download XLSX: {www.error}");
					return null;
				}

				File.WriteAllBytes(tempFile, www.downloadHandler.data);
				return tempFile;
			}
		}

		private static string NormalizeXlsxPath( string _path )
		{
			// Empty -> fail
			if (string.IsNullOrWhiteSpace(_path))
			{
				UiLog.LogError("Error: <empty> is not a valid path");
				return null;
			}

			// Local or network path -> fail
			if (!_path.StartsWith("http", StringComparison.OrdinalIgnoreCase))
			{
				UiLog.LogError($"Error: '{_path}' is not a valid URL");
				return null;
			}

			// Already an export URL with XLSX -> unchanged
			if (_path.Contains("/export", StringComparison.OrdinalIgnoreCase)
			    && _path.Contains("format=xlsx", StringComparison.OrdinalIgnoreCase))
			{
				return _path;
			}

			// Try to treat it as a normal Google Sheets "edit" URL.
			const string marker = "/d/";
			int markerIndex = _path.IndexOf(marker, StringComparison.Ordinal);
			if (markerIndex < 0)
			{
				UiLog.LogError($"Error: {nameof(LocaExcelBridge)}: Google Sheets URL does not contain '/d/': {_path}");
				return null;
			}

			int idStart = markerIndex + marker.Length;
			int idEnd = _path.IndexOfAny(new[] { '/', '?', '#' }, idStart);

			string docId;
			if (idEnd >= 0)
				docId = _path.Substring(idStart, idEnd - idStart);
			else
				docId = _path.Substring(idStart);

			if (string.IsNullOrEmpty(docId))
			{
				UiLog.LogError($"Error: {nameof(LocaExcelBridge)}: Could not extract document id from Google Sheets URL: {_path}");
				return null;
			}

			string exportUrl = $"https://docs.google.com/spreadsheets/d/{docId}/export?format=xlsx&id={docId}";
			return exportUrl;
		}

		private static string ApplyKeyAffixes( string _key, InColumnDescription _desc )
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

		private void WriteJson( ProcessedLoca _data )
		{
			string assetName = string.IsNullOrEmpty(name) ? "LocaTable" : name;
			string relDir = $"Assets/Resources/{LocaProviderList.RESOURCES_SUB_PATH}";
			Directory.CreateDirectory(relDir);
			string outPath = Path.Combine(relDir, assetName + ".json");

			string json = JsonUtility.ToJson(_data, true);
			File.WriteAllText(outPath, json, new UTF8Encoding(false));
			UiLog.LogInternal($"{nameof(LocaExcelBridge)}: Wrote JSON -> {outPath}");
		}
#endif
#endif

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
