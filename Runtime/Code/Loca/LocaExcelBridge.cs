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
using System.Threading;
using System.Diagnostics;


#endif

namespace GuiToolkit
{
	/// <summary>
	/// ScriptableObject-based localization provider that imports translations from Excel (.xlsx) files.
	/// Supports both local files and Google Sheets URLs (with optional service account authentication).
	/// Maps Excel columns to localization keys and language translations, with support for plural forms.
	/// </summary>
	[CreateAssetMenu(fileName = nameof(LocaExcelBridge), menuName = StringConstants.LOCA_EXCEL_BRIDGE)]
	public class LocaExcelBridge : ScriptableObject, ILocaProvider
	{
		/// <summary>
		/// Specifies whether the Excel source is a local file or a Google Sheets document.
		/// </summary>
		public enum SourceType
		{
			/// <summary>Excel file located on disk.</summary>
			Local,
			/// <summary>Excel file hosted on Google Sheets (requires URL).</summary>
			GoogleDocs,
		}

		/// <summary>
		/// Defines how an Excel column should be interpreted during import.
		/// </summary>
		public enum EInColumnType
		{
			/// <summary>Column is not processed.</summary>
			Ignore,
			/// <summary>Column contains the master localization key (msgid).</summary>
			Key,
			/// <summary>Column contains translated text for a specific language.</summary>
			LanguageTranslation,
		}

		/// <summary>
		/// Describes how a single Excel column maps to localization data.
		/// Configures language, key affixes, and plural form index per column.
		/// </summary>
		[Serializable]
		public class InColumnDescription
		{
			/// <summary>
			/// (Read-only) Human-readable description auto-generated from other fields.
			/// Displayed in the inspector for easier configuration review.
			/// </summary>
			[ReadOnly] public string Description;
			
			/// <summary>How this column should be interpreted.</summary>
			public EInColumnType ColumnType;
			
			/// <summary>
			/// The language identifier for this column (e.g., "en", "de").
			/// Only used when <see cref="ColumnType"/> is <see cref="EInColumnType.LanguageTranslation"/>.
			/// </summary>
			public string LanguageId;
			
			/// <summary>
			/// Optional prefix prepended to keys from this column.
			/// Useful for namespacing keys from shared Excel sheets.
			/// </summary>
			public string KeyPrefix;
			
			/// <summary>
			/// Optional suffix appended to keys from this column.
			/// </summary>
			public string KeyPostfix;
			
			/// <summary>
			/// Plural form index for this column: -1 for singular (no plurals), 0-5 for plural forms.
			/// Allows Excel columns to map to specific msgstr[N] plural slots.
			/// </summary>
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
		[SerializeField][Optional] private bool m_useGoogleAuth = false;
		[SerializeField][Optional] private string m_serviceAccountJsonPath = string.Empty;
		[SerializeField] private string m_group;
		[SerializeField] private List<InColumnDescription> m_columnDescriptions = new();
		[SerializeField] private int m_startRow = 0; // all rows before are ignored (0-based index)

		[Header("Output (Read-Only)")]
		[Space]
		[GuiToolkit.ReadOnly][SerializeField] private ProcessedLoca m_processedLoca;

		/// <summary>
		/// Gets the processed localization data imported from the Excel source.
		/// Returns an empty <see cref="ProcessedLoca"/> if not yet collected.
		/// </summary>
		public ProcessedLoca Localization => m_processedLoca != null ? m_processedLoca : new ProcessedLoca();

		/// <summary>
		/// Gets the number of configured columns.
		/// </summary>
		public int NumColumns => m_columnDescriptions.Count;

#if UNITY_EDITOR
		// Editor-accessible properties (public so the editor assembly can read them).

		/// <summary>(Editor) The configured source type.</summary>
		public SourceType EdSourceType => m_sourceType;

		/// <summary>(Editor) The Google Sheets URL, if configured.</summary>
		public string EdGoogleUrl => m_googleUrl;

		/// <summary>(Editor) The local Excel file path, if configured.</summary>
		public string EdExcelPath => m_excelPath.Path;

		/// <summary>(Editor) The localization group name.</summary>
		public string EdGroup => m_group;

		/// <summary>(Editor) Whether Google service-account authentication is enabled.</summary>
		public bool EdUseGoogleAuth => m_useGoogleAuth;

		/// <summary>(Editor) Path to the service account JSON key file.</summary>
		public string EdServiceAccountJsonPath => m_serviceAccountJsonPath;

		/// <summary>(Editor) Zero-based row index where translation data starts.</summary>
		public int EdStartRow => m_startRow;

		/// <summary>(Editor) Returns a shallow copy of the configured column descriptions.</summary>
		public List<InColumnDescription> EdColumnDescriptions =>
			new List<InColumnDescription>(m_columnDescriptions ?? new List<InColumnDescription>());

		/// <summary>
		/// (Editor) Replaces the column description list and marks the asset dirty.
		/// </summary>
		/// <param name="_columns">The new column list to set.</param>
		public void EdSetColumnDescriptions(List<InColumnDescription> _columns)
		{
			m_columnDescriptions = _columns ?? new List<InColumnDescription>();
			EditorUtility.SetDirty(this);
		}

		/// <summary>
		/// (Editor) Delegate invoked by <see cref="PushToSpreadsheet"/>.
		/// Registered by <c>LocaExcelBridgePusher</c> via <c>[InitializeOnLoad]</c>.
		/// </summary>
		internal static Action<LocaExcelBridge> s_pushCallback;

		/// <summary>
		/// Returns <c>true</c> when the current configuration supports pushing translations
		/// back to the source spreadsheet or exporting them as a CSV file.
		/// </summary>
		public bool CanPush =>
			m_sourceType == SourceType.Local
				? (m_excelPath != null && !string.IsNullOrEmpty(m_excelPath.Path))
				: (!string.IsNullOrEmpty(m_googleUrl) && m_useGoogleAuth && !string.IsNullOrEmpty(m_serviceAccountJsonPath));

		/// <summary>
		/// Pushes the current PO translations back to the configured spreadsheet source.
		/// For Google Sheets: writes via the Sheets API v4 (requires write-scope auth).
		/// For local XLSX: exports a CSV file alongside the XLSX with the same base name.
		/// Actual work is delegated to <c>LocaExcelBridgePusher</c> in the editor assembly.
		/// </summary>
		[ContextMenu("Push to Spreadsheet")]
		public void PushToSpreadsheet()
		{
			if (s_pushCallback == null)
			{
				UiLog.LogError($"{nameof(LocaExcelBridge)}: Push callback not registered. Ensure the editor assembly is loaded.");
				return;
			}

			s_pushCallback.Invoke(this);
		}
#endif

		/// <summary>
		/// Composes the full localization key for a given base key and column index.
		/// Applies the column's <see cref="InColumnDescription.KeyPrefix"/> and <see cref="InColumnDescription.KeyPostfix"/>.
		/// </summary>
		/// <param name="_key">The base key from the Key column.</param>
		/// <param name="_column">The column index.</param>
		/// <returns>The composed key with affixes applied, or null if the column is invalid or not a language translation column.</returns>
		public string GetKey( string _key, int _column )
		{
			if (!_column.IsInRange(0, NumColumns))
				return null;

			var description = GetColumnDescription(_column);
			if (description.ColumnType != EInColumnType.LanguageTranslation)
				return null;

			return $"{description.KeyPrefix}{_key}{description.KeyPostfix}";
		}

		/// <summary>
		/// Gets the column description for a given column index.
		/// </summary>
		/// <param name="_column">The zero-based column index.</param>
		/// <returns>The <see cref="InColumnDescription"/> for that column, or null if the index is out of range.</returns>
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


			// Infer column config only if not yet configured (never overwrite existing user configuration)
			if (m_columnDescriptions == null || m_columnDescriptions.Count == 0)
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

			string bearerToken = null;
			if (m_useGoogleAuth && !string.IsNullOrEmpty(m_serviceAccountJsonPath))
			{
				bearerToken = GoogleServiceAccountAuth.GetAccessToken(m_serviceAccountJsonPath);
				if (bearerToken == null)
				{
					UiLog.LogError($"{nameof(LocaExcelBridge)}: Failed to obtain Google auth token");
					return null;
				}
			}

			using (UnityWebRequest www = UnityWebRequest.Get(path))
			{
				if (bearerToken != null)
					www.SetRequestHeader("Authorization", $"Bearer {bearerToken}");

				var op = www.SendWebRequest();
				var sw = Stopwatch.StartNew();
				const int timeoutMs = 60000;

				while (!op.isDone)
				{
					if (sw.ElapsedMilliseconds > timeoutMs)
					{
						www.Abort();
						UiLog.LogError($"{nameof(LocaExcelBridge)}: Download timeout for XLSX:{path}");
						return null;
					}

					EditorApplication.QueuePlayerLoopUpdate();
					Thread.Sleep(10);
				}

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
