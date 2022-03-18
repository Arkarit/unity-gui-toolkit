#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit
{
	public class CleanKerningTable : EditorWindow
	{
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

		private void OnGUI()
		{
			m_fontAsset = EditorGUILayout.ObjectField(m_fontAssetGuiContent, m_fontAsset, typeof(TMP_FontAsset), false) as TMP_FontAsset;
			m_skipLowerUpper = EditorGUILayout.Toggle(m_skipLowerUpperGuiContent, m_skipLowerUpper);
			m_useOnlySpecifiedChars = EditorGUILayout.Toggle(m_useOnlySpecifiedCharsGuiContent, m_useOnlySpecifiedChars);
			if (m_useOnlySpecifiedChars)
				m_specifiedChars = EditorGUILayout.TextField(m_specifiedCharsGuiContent, m_specifiedChars);
			m_skipIfLessThan = EditorGUILayout.FloatField(m_skipIfLessThanGuiContent, m_skipIfLessThan);

			EditorGUILayout.Space();

			m_dryRun = EditorGUILayout.Toggle(m_dryRunGuiContent, m_dryRun);

			if (m_fontAsset && GUILayout.Button("Clean up asset"))
				CleanUp();
		}

		private void CleanUp()
		{
			Debug.Assert(m_fontAsset != null);

			List<TMP_GlyphPairAdjustmentRecord> oldAdjustmentRecords = AdjustmentRecords;
			List<TMP_Character> characters = m_fontAsset.characterTable;
			Dictionary<uint, char> charactersByGlyphIndex = new Dictionary<uint, char>();
			foreach (var tmpCharacter in characters)
				charactersByGlyphIndex.Add(tmpCharacter.glyphIndex, (char) tmpCharacter.unicode);

			List<TMP_GlyphPairAdjustmentRecord> newAdjustmentRecords = new List<TMP_GlyphPairAdjustmentRecord>();

			foreach(TMP_GlyphPairAdjustmentRecord record in oldAdjustmentRecords)
			{
				TMP_GlyphAdjustmentRecord first = record.firstAdjustmentRecord;
				TMP_GlyphAdjustmentRecord second = record.secondAdjustmentRecord;

				if (first.glyphIndex == 0 || second.glyphIndex == 0)
				{
					LogPair(' ', ' ', true, "Invalid glyph index");
					continue;
				}

				char a = charactersByGlyphIndex[first.glyphIndex];
				char b = charactersByGlyphIndex[second.glyphIndex];

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

			if (!m_dryRun)
			{
				AdjustmentRecords = newAdjustmentRecords;
				EditorUtility.SetDirty(m_fontAsset);
				AssetDatabase.SaveAssets();
				AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate | ImportAssetOptions.ForceSynchronousImport);
			}
		}

		private void LogPair(char _a, char _b, bool _skipped = false, string reason = null)
		{
			string pair = _a + "/" + _b;
			string start = _skipped ? "Skipping " : "Adding ";
			reason = string.IsNullOrEmpty(reason) ? "" : $": {reason}";
			Debug.Log($"{start} pair '{pair}'{reason}");
		}

		private List<TMP_GlyphPairAdjustmentRecord> AdjustmentRecords
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
			}
		}

		[MenuItem(StringConstants.CLEAN_KERNING_TABLE_MENU_NAME, priority = Constants.CLEAN_KERNING_TABLE_MENU_PRIORITY)]
		public static CleanKerningTable GetWindow()
		{
			var window = GetWindow<CleanKerningTable>();
			window.titleContent = new GUIContent("Clean Kerning Table");
			window.Focus();
			window.Repaint();
			return window;
		}
	}
}
#endif