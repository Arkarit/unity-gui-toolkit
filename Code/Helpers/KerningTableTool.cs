#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit
{
	/// \brief Kerning Table Tool
	/// 
	/// Kerning Table Tool is intended to clean up and save/load/diff/merge kerning tables in Text Mesh Pro.<br>
	/// Text Mesh Pro is not very good when it comes to importing and editing kerning tables.<br>
	/// However, here's some information on how to import a font with kerning into Unity and how to clean it up with the Kerning Table Tool.<br>
	/// <br>
	/// <h2>Import</h2>
	/// Text Mesh Pro can only import fonts which have the 'kern' array filled. A lot of fonts doesn't have this, and we can't do anything about it in terms of Unity scripting.<br>
	/// However, here's a procedure to overcome this:
	/// - First, try to import the font with FontAssetCreator. <br>
	///   If that works, you're done.
	///   <img src="import-kerning-table.png" align="left"><br><br><br><br><br><br><br><br><br><br><br><br><br><br><br><br><br><br><br><br><br><br>
	///   But if it fails, follow the next steps:
	/// - Download FontForge (https://fontforge.org/en-US/)
	/// - Load your Font
	/// - File/Generate Fonts with these settings:<br>
	///   <img src="fontforge-font-settings.png" align="left"><br><br><br><br><br><br><br><br><br><br><br><br><br><br><br><br><br><br><br><br><br><br><br><br>
	///   You now should be able to import the kerning table with FontAssetCreator.
	///   
	/// <br>
	/// <h2>Kerning Table Tool</h2>
	/// After you successfully imported the font with kerning tables you might need to clean it up.<br>
	/// It might be of giant size and contain invalid and/or unwanted chars, or adjustments that are so small that you can barely see it.<br>
	/// Thats where the Kerning Table Tool comes into play.<br>
	/// <img src="kerning-table-tool.png" align="left"><br><br><br><br><br><br><br><br><br><br><br><br><br><br>
	/// <br>
	/// <h3>Font Asset (Mandatory)</h3>
	/// Drag your font asset here. This is mandatory before you can perform any actions with the Kerning Table Tool.
	/// <h3>Clean Up</h3>
	/// This section deals with cleaning up the kerning table in the font asset.
	/// - <b>Skip Lower/Upper Pairs</b> Usually, combinations of a lower char as first, 
	///   and an upper char as second character (e.g. aB) are not necessary. You can strip them away with that setting.
	/// - <b>Use only specified chars</b> You can allow only pairs out of certain characters. This will e.g. strip rarely used characters away.
	/// - <b>Specified Chars</b> This is the list of allowed characters which are not stripped away.
	/// - <b>Skip if Advance less than</b> If the adjustments are so small, that they are below this value, they are stripped away. 
	///   If you don't want anything to be stripped away because of advance value, enter 0 here.
	/// - <b>Dry Run</b> If you want to test the operation first, check this. The pairs then are determined, but the asset is not written.
	///   This e.g. makes sense while you try to find out the correct value for advance.<br>
	/// - <b>Clean up Kerning Table</b> This button performs the clean up based on the settings described above.
	///   It outputs the stripped and added pairs in the console.
	///   
	/// <h3>Save/Load</h3>
	/// Here you can save and load kerning tables into JSON files.
	/// This can help you to preserve your kerning additions and tweaks if you have to reimport your font.<br>
	/// If the kerning informations in the actual OTF or TTF haven't changed, thats very simple. Before reimporting the font save the kerning to disk, and load it afterwards.<br>
	/// If the kerning informations do have changed, it's a bit more difficult. <br>
	/// That requires a specific work flow described here:<br>
	/// - After you have imported the font and cleaned up the kerning tables, save the kerning tables as e.g. ".MyFontDefaultKerning.json".
	/// - Do your additions and tweaks to the kerning.
	/// - After you're done, save a diff between the default kerning and your changed settings. Save it e.g. as ".MyFontKerningDiff.json"
	/// - If you now have to reimport the font, clean it up after it has been imported.
	/// - Then merge your ".MyFontKerningDiff.json" - that will bring back your changes.
	/// 
	/// Description of the buttons:
	/// - <b>Save kerning table</b> Saves a kerning table to disk as JSON.
	/// - <b>Save kerning table diff</b> Saves a kerning table diff to disk. 
	///   First, you have to load a JSON, with which the current kerning table is diffed.
	///   Usually, that will be your ".MyFontDefaultKerning.json".<br>
	///   Second, you are asked for a file name to save the diff. Only your changes will be saved in that file.
	/// - <b>Load kerning table</b> Loads a kerning table and replaces the current one.
	/// - <b>Merge kerning table</b> Loads a kerning table, but replaces only the glyph pairs of the current kerning table, which are stored in that file.

	public class KerningTableTool : EditorWindow
	{
		[Serializable]
		private class ListContainer
		{
			public List<TMP_GlyphPairAdjustmentRecord> Records;
		}


		private readonly GUIContent m_fontAssetGuiContent = new GUIContent("Font Asset", "Drag your font asset here");
		private TMP_FontAsset m_fontAsset;

		private readonly GUIContent m_dryRunGuiContent = new GUIContent("Dry Run", "Only test, don't actually save the asset");
		private bool m_dryRun = true;

		private readonly GUIContent m_skipLowerUpperGuiContent = new GUIContent("Skip lower/upper pairs", "Skip if first char is lower and second is upper; this combination is uncommon");
		private bool m_skipLowerUpper = true;

		private readonly GUIContent m_useOnlySpecifiedCharsGuiContent = new GUIContent("Use only specified chars", "Use only chars, which are contained in the string, which is shown below (if checked)");
		private bool m_useOnlySpecifiedChars = true;

		private readonly GUIContent m_specifiedCharsGuiContent = new GUIContent("Specified chars", "Use only chars, which are contained in this string");
		private string m_specifiedChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZÄÖÜabcdefghijklmnopqrstuvwxyzäöü";

		private readonly GUIContent m_skipIfLessThanGuiContent = new GUIContent("Skip if advance less than", "Skip adjustments, which are below this value to save space. A value of 0 entered here doesn't skip anything.");
		private float m_skipIfLessThan = 0.5f;


		// This is a getter/setter for the current adjustment record table
		private List<TMP_GlyphPairAdjustmentRecord> adjustmentRecords
		{
			get
			{
				if (m_fontAsset == null)
					return new List<TMP_GlyphPairAdjustmentRecord>();

				return m_fontAsset.fontFeatureTable.glyphPairAdjustmentRecords;
			}

			set
			{
				if (m_fontAsset == null)
					return;

				m_fontAsset.fontFeatureTable.glyphPairAdjustmentRecords = value;
				EditorUtility.SetDirty(m_fontAsset);
				AssetDatabase.SaveAssets();
				AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
			}
		}


		private void OnGUI()
		{
			GUILayout.Label("Font Asset (mandatory)", EditorStyles.boldLabel);
			m_fontAsset = EditorGUILayout.ObjectField(m_fontAssetGuiContent, m_fontAsset, typeof(TMP_FontAsset), false) as TMP_FontAsset;
			if (m_fontAsset)
			{
				EditorGUILayout.Space();
				GUILayout.Label("Clean up", EditorStyles.boldLabel);
				EditorGUILayout.BeginHorizontal(EditorStyles.helpBox);
				EditorGUILayout.BeginVertical();
				m_skipLowerUpper = EditorGUILayout.Toggle(m_skipLowerUpperGuiContent, m_skipLowerUpper);
				m_useOnlySpecifiedChars = EditorGUILayout.Toggle(m_useOnlySpecifiedCharsGuiContent, m_useOnlySpecifiedChars);
				if (m_useOnlySpecifiedChars)
					m_specifiedChars = EditorGUILayout.TextField(m_specifiedCharsGuiContent, m_specifiedChars);
				m_skipIfLessThan = EditorGUILayout.FloatField(m_skipIfLessThanGuiContent, m_skipIfLessThan);
				m_dryRun = EditorGUILayout.Toggle(m_dryRunGuiContent, m_dryRun);
				EditorGUILayout.EndVertical();

				if (GUILayout.Button("Clean up kerning table"))
					CleanUpAsset();

				EditorGUILayout.EndHorizontal();

				EditorGUILayout.Space();
				GUILayout.Label("Save/Load", EditorStyles.boldLabel);
				EditorGUILayout.BeginVertical(EditorStyles.helpBox);
				if (GUILayout.Button("Save kerning table"))
					SaveKerningTable();
				if (GUILayout.Button("Save kerning table diff"))
					SaveKerningTableDiff();
				if (GUILayout.Button("Load kerning table"))
					LoadKerningTable();
				if (GUILayout.Button("Merge kerning table"))
					MergeKerningTable();
				EditorGUILayout.EndVertical();
			}
		}


		private void CleanUpAsset()
		{
			Debug.Assert(m_fontAsset != null);

			List<TMP_GlyphPairAdjustmentRecord> oldAdjustmentRecords = adjustmentRecords;
			Dictionary<uint, uint> charactersByGlyphIndex = GetConversionDict(true);

			List<TMP_GlyphPairAdjustmentRecord> newAdjustmentRecords = new List<TMP_GlyphPairAdjustmentRecord>();

			Debug.Log($"Pairs before: {oldAdjustmentRecords.Count}");

			foreach (TMP_GlyphPairAdjustmentRecord record in oldAdjustmentRecords)
			{
				TMP_GlyphAdjustmentRecord first = record.firstAdjustmentRecord;
				TMP_GlyphAdjustmentRecord second = record.secondAdjustmentRecord;

				if (first.glyphIndex == 0 || second.glyphIndex == 0)
				{
					LogPair(' ', ' ', true, "Invalid glyph index");
					continue;
				}

				char a = (char) charactersByGlyphIndex[first.glyphIndex];
				char b = (char) charactersByGlyphIndex[second.glyphIndex];

				if (m_skipLowerUpper && char.IsLower(a) && char.IsUpper(b))
				{
					LogPair(a, b, true, m_skipLowerUpperGuiContent.text);
					continue;
				}

				if (m_useOnlySpecifiedChars && (m_specifiedChars.IndexOf(a) == -1 || m_specifiedChars.IndexOf(b) == -1))
				{
					LogPair(a, b, true, m_useOnlySpecifiedCharsGuiContent.text);
					continue;
				}

				if (!Mathf.Approximately(m_skipIfLessThan, 0))
				{
					float advance1 = Mathf.Abs(first.glyphValueRecord.xAdvance);
					float advance2 = Mathf.Abs(second.glyphValueRecord.xAdvance);
					float advance = Mathf.Max(advance1, advance2);
					if (advance < m_skipIfLessThan)
					{
						LogPair(a, b, true, m_skipIfLessThanGuiContent.text);
						continue;
					}
				}

				newAdjustmentRecords.Add(record);
				LogPair(a, b);
			}

			Debug.Log($"Pairs after: {newAdjustmentRecords.Count}");

			if (!m_dryRun)
				adjustmentRecords = newAdjustmentRecords;
		}

		private void SaveKerningTable() => SaveJsonWithFileSelector();

		private void SaveKerningTableDiff()
		{
			List<TMP_GlyphPairAdjustmentRecord> originalKerningTable = LoadJsonWithFileSelector("Load Original Kerning Table");
			if (originalKerningTable == null)
				return;
			List<TMP_GlyphPairAdjustmentRecord> currentKerningTable = adjustmentRecords;

			List<TMP_GlyphPairAdjustmentRecord> newKerningTable = new List<TMP_GlyphPairAdjustmentRecord>();

			foreach (TMP_GlyphPairAdjustmentRecord record in currentKerningTable)
			{
				TMP_GlyphPairAdjustmentRecord otherRecord = FindOtherRecord(record, originalKerningTable);
				if (otherRecord == null)
				{
					newKerningTable.Add(record);
					continue;
				}

				TMP_GlyphAdjustmentRecord first = record.firstAdjustmentRecord;
				TMP_GlyphAdjustmentRecord second = record.secondAdjustmentRecord;
				TMP_GlyphAdjustmentRecord otherFirst = otherRecord.firstAdjustmentRecord;
				TMP_GlyphAdjustmentRecord otherSecond = otherRecord.secondAdjustmentRecord;

				if (!IsEqual(first, otherFirst) || !IsEqual(second, otherSecond))
					newKerningTable.Add(record);
			}

			if (newKerningTable.Count == 0)
			{
				EditorUtility.DisplayDialog("No changes detected", "Diff is not saved", "OK");
				return;
			}

			SaveJsonWithFileSelector(newKerningTable);
		}

		private void LoadKerningTable()
		{
			List<TMP_GlyphPairAdjustmentRecord> kerningTable = LoadJsonWithFileSelector();
			if (kerningTable != null)
			{
				adjustmentRecords = kerningTable;
			}
		}

		private void MergeKerningTable()
		{
			List<TMP_GlyphPairAdjustmentRecord> mergeKerningTable = LoadJsonWithFileSelector("Merge Kerning Table");
			if (mergeKerningTable == null)
				return;
			List<TMP_GlyphPairAdjustmentRecord> currentKerningTable = adjustmentRecords;

			List<TMP_GlyphPairAdjustmentRecord> newKerningTable = new List<TMP_GlyphPairAdjustmentRecord>();

			foreach (TMP_GlyphPairAdjustmentRecord record in currentKerningTable)
			{
				TMP_GlyphPairAdjustmentRecord otherRecord = FindOtherRecord(record, mergeKerningTable);
				if (otherRecord == null)
				{
					newKerningTable.Add(record);
					continue;
				}

				TMP_GlyphAdjustmentRecord first = record.firstAdjustmentRecord;
				TMP_GlyphAdjustmentRecord second = record.secondAdjustmentRecord;
				TMP_GlyphAdjustmentRecord otherFirst = otherRecord.firstAdjustmentRecord;
				TMP_GlyphAdjustmentRecord otherSecond = otherRecord.secondAdjustmentRecord;

				if (!IsEqual(first, otherFirst) || !IsEqual(second, otherSecond))
					newKerningTable.Add(otherRecord);
			}

			foreach (TMP_GlyphPairAdjustmentRecord record in mergeKerningTable)
			{
				TMP_GlyphPairAdjustmentRecord otherRecord = FindOtherRecord(record, currentKerningTable);
				if (otherRecord == null)
					newKerningTable.Add(record);
			}

			adjustmentRecords = newKerningTable;
		}


		// Unfortunately, TMP_GlyphPairAdjustmentRecord uses glyph indices. These however are subject to change if you reimport the font (e.g. with different padding)
		// Thus, the glyph indices need to be changed to characters prior to saving, and changed back to glyph indices after loading (to match the potentially changed glyph indices)
		private List<TMP_GlyphPairAdjustmentRecord> ConvertGlyhpIndicesAndCharacters( List<TMP_GlyphPairAdjustmentRecord> records, bool glyphIndicesToCharacters )
		{
			List<TMP_GlyphPairAdjustmentRecord> result = DeepCopy(records);
			var conversionDict = GetConversionDict(glyphIndicesToCharacters);

			foreach (var record in result)
			{
				TMP_GlyphAdjustmentRecord first = record.firstAdjustmentRecord;
				TMP_GlyphAdjustmentRecord second = record.secondAdjustmentRecord;

				if (conversionDict.TryGetValue(first.glyphIndex, out uint firstGlyphIndex))
					first.glyphIndex = firstGlyphIndex;

				if (conversionDict.TryGetValue(second.glyphIndex, out uint secondGlyphIndex))
					second.glyphIndex = secondGlyphIndex;

				record.firstAdjustmentRecord = first;
				record.secondAdjustmentRecord = second;
			}

			return result;
		}

		// Create Dictionary from glyph indices to characters and vice versa
		private Dictionary<uint, uint> GetConversionDict(bool glyphIndicesToCharacters)
		{
			List<TMP_Character> characters = m_fontAsset.characterTable;
			Dictionary<uint, uint> result = new Dictionary<uint, uint>();
			foreach (var tmpCharacter in characters)
				result.Add
				(
					glyphIndicesToCharacters ? tmpCharacter.glyphIndex : tmpCharacter.unicode,
					glyphIndicesToCharacters ? tmpCharacter.unicode : tmpCharacter.glyphIndex
				);
			return result;
		}

		private void SaveJsonWithFileSelector(List<TMP_GlyphPairAdjustmentRecord> table = null)
		{
			var list = table == null ? adjustmentRecords : table;
			list = ConvertGlyhpIndicesAndCharacters(list, true);

			ListContainer listContainer = new ListContainer { Records = list };
			string s = JsonUtility.ToJson(listContainer, true);

			var path = EditorUtility.SaveFilePanel
			(
				"Save Kerning Table",
				"",
				".kerningTable.json",
				"json"
			);

			if (!string.IsNullOrEmpty(path))
				File.WriteAllText(path, s);
		}

		private List<TMP_GlyphPairAdjustmentRecord> LoadJsonWithFileSelector(string title = "Load Kerning Table")
		{
			var path = EditorUtility.OpenFilePanel
			(
				title,
				"",
				"json"
			);

			if (!string.IsNullOrEmpty(path))
			{
				string s = File.ReadAllText(path);
				if (!string.IsNullOrEmpty(s))
				{
					ListContainer listContainer = JsonUtility.FromJson<ListContainer>(s);
					listContainer.Records = ConvertGlyhpIndicesAndCharacters(listContainer.Records, false);
					return listContainer.Records;
				}
			}

			return null;
		}

		private static List<TMP_GlyphPairAdjustmentRecord> DeepCopy( List<TMP_GlyphPairAdjustmentRecord> records )
		{
			var t = new ListContainer { Records = records };
			var s = JsonUtility.ToJson(t);
			return JsonUtility.FromJson<ListContainer>(s).Records;
		}

		// Find a TMP_GlyphPairAdjustmentRecord in a kerning table. Returns null if not found or the TMP_GlyphPairAdjustmentRecord in the kerning table
		private static TMP_GlyphPairAdjustmentRecord FindOtherRecord( TMP_GlyphPairAdjustmentRecord record, List<TMP_GlyphPairAdjustmentRecord> kerningTable )
		{
			foreach (TMP_GlyphPairAdjustmentRecord otherRecord in kerningTable)
			{
				TMP_GlyphAdjustmentRecord first = record.firstAdjustmentRecord;
				TMP_GlyphAdjustmentRecord second = record.secondAdjustmentRecord;
				TMP_GlyphAdjustmentRecord otherFirst = otherRecord.firstAdjustmentRecord;
				TMP_GlyphAdjustmentRecord otherSecond = otherRecord.secondAdjustmentRecord;
				if (first.glyphIndex == otherFirst.glyphIndex && second.glyphIndex == otherSecond.glyphIndex)
					return otherRecord;
			}

			return null;
		}

		// Compare by value
		private static bool IsEqual( TMP_GlyphAdjustmentRecord record, TMP_GlyphAdjustmentRecord otherRecord )
		{
			var value = record.glyphValueRecord;
			var otherValue = otherRecord.glyphValueRecord;

			return
				   Mathf.Approximately(value.xAdvance, otherValue.xAdvance)
				&& Mathf.Approximately(value.yAdvance, otherValue.yAdvance)
				&& Mathf.Approximately(value.xPlacement, otherValue.xPlacement)
				&& Mathf.Approximately(value.yPlacement, otherValue.yPlacement);
		}

		// Standard output a char pair to console
		private static void LogPair( char _a, char _b, bool _skipped = false, string reason = null )
		{
			string pair = _a + "/" + _b;
			string start = _skipped ? "Skipping " : "Adding ";
			reason = string.IsNullOrEmpty(reason) ? "" : $": {reason}";
			Debug.Log($"{start} pair '{pair}'{reason}");
		}


		[MenuItem(StringConstants.KERNING_TABLE_TOOL_MENU_NAME, priority = Constants.KERNING_TABLE_TOOL_MENU_PRIORITY)]
		public static KerningTableTool GetWindow()
		{
			var window = GetWindow<KerningTableTool>();
			window.titleContent = new GUIContent("Kerning Table Tool");
			window.Focus();
			window.Repaint();
			return window;
		}
	}
}
#endif