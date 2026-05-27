using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Editor
{
	public class FindNonLocalizedWindow : EditorWindow
	{
		[SerializeField] private DefaultAsset m_potFile;

		private Vector2 m_scroll;
		private List<MissingEntry> m_results = new List<MissingEntry>();
		private bool m_hasResults;
		private string m_outputPath;

		private struct MissingEntry
		{
			public string PoFile;
			public string MsgId;
		}

		[MenuItem(StringConstants.LOCA_MISC_FIND_NON_LOCALIZED_MENU_NAME, false, Constants.LOCA_MISC_FIND_NON_LOCALIZED_MENU_PRIORITY)]
		public static void Open()
		{
			var wnd = GetWindow<FindNonLocalizedWindow>("Find Non Localized");
			wnd.minSize = new Vector2(500, 400);
			wnd.Show();
		}

		private void OnGUI()
		{
			EditorGUILayout.LabelField("Find Non-Localized Keys", EditorStyles.boldLabel);
			EditorGUILayout.Space();

			m_potFile = (DefaultAsset)EditorGUILayout.ObjectField(
				"POT File (optional)", m_potFile, typeof(DefaultAsset), false);
			EditorGUILayout.HelpBox(
				"Leave empty to search all POT/PO files. If set, only the selected .pot and its matching .po files are checked.",
				MessageType.None);

			if (m_potFile != null)
			{
				string path = AssetDatabase.GetAssetPath(m_potFile);
				if (!path.EndsWith(".pot", StringComparison.OrdinalIgnoreCase))
					EditorGUILayout.HelpBox("Selected file is not a .pot file.", MessageType.Warning);
			}

			EditorGUILayout.Space();

			if (GUILayout.Button("Find Missing Keys"))
				RunSearch();

			if (!m_hasResults)
				return;

			EditorGUILayout.Space();
			EditorGUILayout.LabelField($"Results: {m_results.Count} missing key(s)", EditorStyles.boldLabel);

			if (!string.IsNullOrEmpty(m_outputPath))
			{
				EditorGUILayout.BeginHorizontal();
				EditorGUILayout.LabelField(m_outputPath, EditorStyles.miniLabel);
				if (GUILayout.Button("Show", GUILayout.Width(50)))
					EditorUtility.RevealInFinder(m_outputPath);
				EditorGUILayout.EndHorizontal();
			}

			if (m_results.Count == 0)
			{
				EditorGUILayout.HelpBox("No missing keys found.", MessageType.Info);
				return;
			}

			m_scroll = EditorGUILayout.BeginScrollView(m_scroll);

			string currentFile = null;
			foreach (var entry in m_results)
			{
				if (entry.PoFile != currentFile)
				{
					currentFile = entry.PoFile;
					EditorGUILayout.Space();
					EditorGUILayout.LabelField(Path.GetFileName(currentFile), EditorStyles.boldLabel);
				}

				EditorGUILayout.LabelField(entry.MsgId, EditorStyles.wordWrappedLabel);
				EditorGUILayout.Space(2);
				var lineRect = EditorGUILayout.GetControlRect(false, 1f);
				EditorGUI.DrawRect(lineRect, new Color(0.5f, 0.5f, 0.5f, 0.4f));
				EditorGUILayout.Space(4);
			}

			EditorGUILayout.EndScrollView();
		}

		private void RunSearch()
		{
			m_results.Clear();
			m_hasResults = true;

			if (m_potFile != null)
			{
				string relPath = AssetDatabase.GetAssetPath(m_potFile);
				if (!relPath.EndsWith(".pot", StringComparison.OrdinalIgnoreCase))
				{
					Debug.LogWarning("FindNonLocalizedWindow: selected asset is not a .pot file.");
					return;
				}

				SearchSinglePot(Path.GetFullPath(relPath));
			}
			else
			{
				SearchAllPots();
			}

			WriteResultsToFile();
			Repaint();
		}

		private void SearchSinglePot(string potPath)
		{
			var potIds = ReadPotMsgIds(potPath);
			if (potIds == null)
				return;

			string group = GroupFromPotName(Path.GetFileNameWithoutExtension(potPath));
			foreach (string poPath in FindPoFilesForGroup(group))
				CheckPoFile(poPath, potIds);
		}

		private void SearchAllPots()
		{
			string potDir = LocaPoMerger.GetPotDirectory();
			if (string.IsNullOrEmpty(potDir) || !Directory.Exists(potDir))
			{
				Debug.LogWarning("FindNonLocalizedWindow: POT directory not found. Configure it in Gui Toolkit / Localization / Ui Toolkit Configuration.");
				return;
			}

			foreach (string potPath in Directory.GetFiles(potDir, "*.pot"))
			{
				var potIds = ReadPotMsgIds(potPath);
				if (potIds == null)
					continue;

				string group = GroupFromPotName(Path.GetFileNameWithoutExtension(potPath));
				foreach (string poPath in FindPoFilesForGroup(group))
					CheckPoFile(poPath, potIds);
			}
		}

		private static string GroupFromPotName(string fileNameNoExt)
		{
			if (string.Equals(fileNameNoExt, "loca", StringComparison.OrdinalIgnoreCase))
				return null;
			if (fileNameNoExt.StartsWith("loca_", StringComparison.OrdinalIgnoreCase))
				return fileNameNoExt.Substring(5);
			return null;
		}

		private static HashSet<string> ReadPotMsgIds(string potPath)
		{
			try
			{
				var pot = PoFile.Parse(File.ReadAllText(potPath, Encoding.UTF8));
				var ids = new HashSet<string>(StringComparer.Ordinal);
				foreach (var entry in pot.Entries)
				{
					if (!entry.IsObsolete)
						ids.Add(entry.MsgId);
				}
				return ids;
			}
			catch (Exception e)
			{
				Debug.LogError($"FindNonLocalizedWindow: failed to parse '{potPath}': {e.Message}");
				return null;
			}
		}

		private static List<string> FindPoFilesForGroup(string group)
		{
			var result = new List<string>();
			var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			string[] guids = AssetDatabase.FindAssets("t:textasset");

			foreach (string guid in guids)
			{
				string assetPath = AssetDatabase.GUIDToAssetPath(guid);

				string baseName;
				if (assetPath.EndsWith(".po.txt", StringComparison.OrdinalIgnoreCase))
					baseName = Path.GetFileName(assetPath.Substring(0, assetPath.Length - ".txt".Length));
				else if (assetPath.EndsWith(".po", StringComparison.OrdinalIgnoreCase))
					baseName = Path.GetFileName(assetPath);
				else
					continue;

				// Strip .po → "de" or "de_group"
				string key = baseName.Substring(0, baseName.Length - ".po".Length);

				// Prefer .po.txt over .po for the same key
				if (!seen.Add(key))
					continue;

				int underscoreIdx = key.IndexOf('_');
				string fileGroup = underscoreIdx < 0 ? null : key.Substring(underscoreIdx + 1);

				bool groupMatches =
					(group == null && fileGroup == null) ||
					string.Equals(fileGroup, group, StringComparison.OrdinalIgnoreCase);

				if (groupMatches)
					result.Add(Path.GetFullPath(assetPath));
			}

			return result;
		}

		private void CheckPoFile(string poPath, HashSet<string> potIds)
		{
			HashSet<string> existing;
			try
			{
				var po = PoFile.Parse(File.ReadAllText(poPath, Encoding.UTF8));
				existing = new HashSet<string>(StringComparer.Ordinal);
				foreach (var entry in po.Entries)
				{
					if (!entry.IsObsolete)
						existing.Add(entry.MsgId);
				}
			}
			catch (Exception e)
			{
				Debug.LogError($"FindNonLocalizedWindow: failed to parse '{poPath}': {e.Message}");
				return;
			}

			foreach (string msgId in potIds)
			{
				if (!existing.Contains(msgId))
					m_results.Add(new MissingEntry { PoFile = poPath, MsgId = msgId });
			}
		}

		private void WriteResultsToFile()
		{
			m_outputPath = Path.Combine(Application.temporaryCachePath, "MissingLoca.txt");
			try
			{
				var sb = new StringBuilder();
				sb.AppendLine($"Missing localization keys — {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
				sb.AppendLine($"Total: {m_results.Count}");

				string currentFile = null;
				foreach (var entry in m_results)
				{
					if (entry.PoFile != currentFile)
					{
						currentFile = entry.PoFile;
						sb.AppendLine();
						sb.AppendLine($"=== {Path.GetFileName(currentFile)} ===");
					}
					sb.AppendLine($"  {entry.MsgId}");
					sb.AppendLine("  " + new string('-', 60));
				}

				File.WriteAllText(m_outputPath, sb.ToString(), Encoding.UTF8);
				Debug.Log($"FindNonLocalizedWindow: results written to {m_outputPath}");
			}
			catch (Exception e)
			{
				Debug.LogError($"FindNonLocalizedWindow: failed to write results file: {e.Message}");
				m_outputPath = null;
			}
		}
	}
}
