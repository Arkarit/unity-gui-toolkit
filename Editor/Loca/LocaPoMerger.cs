using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Editor
{
	/// <summary>
	/// Editor tool that merges POT template files into their corresponding PO translation files.
	/// POT files are located via <see cref="UiToolkitConfiguration.m_potPath"/>.
	/// PO files are expected at <c>Assets/Resources/{lang}.po</c> or <c>{lang}_{group}.po</c>.
	/// Invoked via Unity menu: Gui Toolkit / Localization / Merge POT into PO files.
	/// </summary>
	[EditorAware]
	public static class LocaPoMerger
	{
		private const string DEFAULT_LOCA_GROUP = "__default__";

		/// <summary>
		/// Menu entry: performs a dry-run, shows a summary dialog, then (on confirmation) merges
		/// all POT files into all matching PO files.
		/// </summary>
		[MenuItem(StringConstants.LOCA_MERGE_POT_MENU_NAME, priority = Constants.LOCA_MERGE_POT_MENU_PRIORITY)]
		public static void MergeAllPots()
		{
			AssetReadyGate.WhenReady(SafeMergeAllPots);
		}

		/// <summary>
		/// Called automatically by <see cref="LocaProcessor"/> after POT generation when
		/// <see cref="UiToolkitConfiguration.AutoMergePotToPo"/> is enabled.
		/// Merges all POTs into all matching PO files without showing a confirmation dialog.
		/// </summary>
		public static void MergeAfterProcessing()
		{
			var potGroups  = FindAllPotGroups();
			var locaFiles  = FindAllLocaFiles();
			var toMerge    = BuildMergeList(potGroups, locaFiles);

			if (toMerge.Count == 0)
				return;

			try
			{
				int done = 0;
				foreach (var (lang, group) in toMerge)
				{
					EditorUtility.DisplayProgressBar(
						"Merging POT \u2192 PO",
						$"{lang} / {group ?? "default"}",
						(float)done / toMerge.Count);

					MergeGroupForLanguage(lang, group, _dryRun: false);
					done++;
				}
				AssetDatabase.Refresh();
			}
			finally
			{
				EditorUtility.ClearProgressBar();
			}
		}

		/// <summary>
		/// Merges the POT for <paramref name="_group"/> into the PO file for <paramref name="_languageId"/>.
		/// When <paramref name="_dryRun"/> is true the merge is computed but no files are written.
		/// </summary>
		/// <param name="_languageId">Language identifier, e.g. <c>"de"</c> or <c>"en"</c>.</param>
		/// <param name="_group">
		/// Group name matching the POT suffix, or null / empty for the default group.
		/// </param>
		/// <param name="_dryRun">
		/// When true, returns merge statistics without modifying any files.
		/// </param>
		/// <returns>
		/// A <see cref="PoMergeResult"/> with merge statistics, or null if the POT was not found.
		/// </returns>
		public static PoMergeResult MergeGroupForLanguage(string _languageId, string _group, bool _dryRun = false)
		{
			string potPath = GetPotPath(_group);
			if (string.IsNullOrEmpty(potPath) || !File.Exists(potPath))
			{
				UiLog.LogWarning($"LocaPoMerger: POT file not found for group '{_group ?? "default"}' at '{potPath}'");
				return null;
			}

			PoFile pot;
			try
			{
				pot = PoFile.Parse(File.ReadAllText(potPath, Encoding.UTF8));
			}
			catch (Exception e)
			{
				UiLog.LogError($"LocaPoMerger: failed to read POT '{potPath}': {e.Message}");
				return null;
			}

			string poPath = GetPoPhysicalPath(_languageId, _group);

			PoFile existingPo;
			if (File.Exists(poPath))
			{
				try
				{
					existingPo = PoFile.Parse(File.ReadAllText(poPath, Encoding.UTF8));
				}
				catch (Exception e)
				{
					UiLog.LogError($"LocaPoMerger: failed to read PO '{poPath}': {e.Message}");
					return null;
				}
			}
			else
			{
				existingPo = CreateEmptyPoFile(_languageId);
			}

			var (merged, result) = PoMergeEngine.Merge(existingPo, pot);

			// Always ensure the header is populated. If the existing file had an empty header
			// (e.g. was hand-written without one, or pre-dates this requirement) we fill it in
			// now so that every written PO file is well-formed.
			if (string.IsNullOrEmpty(merged.HeaderMsgStr))
			{
				merged.HasHeader    = true;
				merged.HeaderMsgStr = DefaultHeaderMsgStr(_languageId);
			}

			if (!_dryRun)
			{
				try
				{
					if (File.Exists(poPath) && PoSsotHeader.HasSsotHeader(poPath))
						PoBackupManager.CreateBackup(poPath);

					Directory.CreateDirectory(Path.GetDirectoryName(poPath));
					File.WriteAllText(poPath, merged.Serialize(), Encoding.UTF8);

					string unityPath = ConvertToUnityPath(poPath);
					if (!string.IsNullOrEmpty(unityPath))
						AssetDatabase.ImportAsset(unityPath);
				}
				catch (Exception e)
				{
					UiLog.LogError($"LocaPoMerger: failed to write PO '{poPath}': {e.Message}");
				}
			}

			return result;
		}

		private static void SafeMergeAllPots()
		{
			var potGroups = FindAllPotGroups();
			if (potGroups.Count == 0)
			{
				EditorUtility.DisplayDialog("Merge POT \u2192 PO", "No POT files found. Run \u2018Process Loca\u2019 first.", "OK");
				return;
			}

			var locaFiles = FindAllLocaFiles();
			if (locaFiles.Count == 0)
			{
				EditorUtility.DisplayDialog("Merge POT \u2192 PO", "No PO files found in Resources.", "OK");
				return;
			}

			var toMerge = BuildMergeList(potGroups, locaFiles);
			if (toMerge.Count == 0)
			{
				EditorUtility.DisplayDialog(
					"Merge POT \u2192 PO",
					"No language/group combinations match between found POT files and PO files.",
					"OK");
				return;
			}

			// Dry run to gather statistics
			var total = new PoMergeResult();
			foreach (var (lang, group) in toMerge)
			{
				var r = MergeGroupForLanguage(lang, group, _dryRun: true);
				if (r == null)
					continue;
				total.AddedKeys     += r.AddedKeys;
				total.ObsoleteKeys  += r.ObsoleteKeys;
				total.PreservedKeys += r.PreservedKeys;
				total.FuzzyKeys     += r.FuzzyKeys;
			}

			string message =
				$"Merge {toMerge.Count} PO file(s)?\n\n" +
				$"  \u2022 Added (new keys):    {total.AddedKeys}\n" +
				$"  \u2022 Preserved:            {total.PreservedKeys}\n" +
				$"  \u2022 Obsolete (to remove): {total.ObsoleteKeys}\n" +
				$"  \u2022 Fuzzy (needs review): {total.FuzzyKeys}";

			if (!EditorUtility.DisplayDialog("Merge POT \u2192 PO", message, "Merge", "Cancel"))
				return;

			try
			{
				int done = 0;
				foreach (var (lang, group) in toMerge)
				{
					EditorUtility.DisplayProgressBar(
						"Merging POT \u2192 PO",
						$"{lang} / {group ?? "default"}",
						(float)done / toMerge.Count);

					MergeGroupForLanguage(lang, group, _dryRun: false);
					done++;
				}

				AssetDatabase.Refresh();
				UiLog.Log($"LocaPoMerger: merged {toMerge.Count} PO file(s). " +
					$"Added={total.AddedKeys}, Preserved={total.PreservedKeys}, Obsolete={total.ObsoleteKeys}");
			}
			finally
			{
				EditorUtility.ClearProgressBar();
			}
		}

		private static List<(string lang, string group)> BuildMergeList(
			List<string> _potGroups,
			List<(string lang, string group)> _locaFiles)
		{
			var list = new List<(string, string)>();
			foreach (var (lang, group) in _locaFiles)
			{
				if (_potGroups.Contains(NormalizeGroup(group)))
					list.Add((lang, group));
			}
			return list;
		}

		private static List<string> FindAllPotGroups()
		{
			var groups = new List<string>();
			string potDir = GetPotDirectory();
			if (string.IsNullOrEmpty(potDir) || !Directory.Exists(potDir))
				return groups;

			foreach (var file in Directory.GetFiles(potDir, "*.pot"))
			{
				string name = Path.GetFileNameWithoutExtension(file);
				if (string.Equals(name, "loca", StringComparison.OrdinalIgnoreCase))
					groups.Add(null); // default group
				else if (name.StartsWith("loca_", StringComparison.OrdinalIgnoreCase))
					groups.Add(name.Substring(5));
			}

			return groups;
		}

		private static List<(string lang, string group)> FindAllLocaFiles()
		{
			var result = new List<(string, string)>();
			var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			string[] guids = AssetDatabase.FindAssets("t:textasset");

			foreach (string guid in guids)
			{
				string assetPath = AssetDatabase.GUIDToAssetPath(guid);

				// Accept both "de.po.txt" (Unity TextAsset convention) and bare "de.po"
				string baseName;
				if (assetPath.EndsWith(".po.txt", StringComparison.OrdinalIgnoreCase))
					baseName = Path.GetFileName(assetPath.Substring(0, assetPath.Length - ".txt".Length)); // → "de.po"
				else if (assetPath.EndsWith(".po", StringComparison.OrdinalIgnoreCase))
					baseName = Path.GetFileName(assetPath); // → "de.po"
				else
					continue;

				// Strip trailing ".po" to get the language/group base name
				string key = baseName.Substring(0, baseName.Length - ".po".Length);

				// Prefer .po.txt — skip if we already registered this key from that extension
				if (!seen.Add(key))
					continue;

				int underscoreIdx = key.IndexOf('_');
				if (underscoreIdx < 0)
					result.Add((key, null));
				else
					result.Add((key.Substring(0, underscoreIdx), key.Substring(underscoreIdx + 1)));
			}

			return result;
		}

		private static string GetPotDirectory()
		{
			if (UiToolkitConfiguration.Instance == null)
				return null;

			string potPath = UiToolkitConfiguration.Instance.m_potPath;
			if (string.IsNullOrEmpty(potPath))
				return null;

			string result = EditorFileUtility.GetApplicationDataDir() + potPath;
			if (result.EndsWith(".pot", StringComparison.OrdinalIgnoreCase))
				result = Path.GetDirectoryName(result);

			return result;
		}

		private static string GetPotPath(string _group)
		{
			string dir = GetPotDirectory();
			if (string.IsNullOrEmpty(dir))
				return null;

			string groupAppendix = string.IsNullOrEmpty(_group) ? string.Empty : $"_{_group}";
			return EditorFileUtility.GetSafePath(Path.Combine(dir, $"loca{groupAppendix}.pot"));
		}

		private static string GetPoPhysicalPath(string _languageId, string _group)
		{
			string groupAppendix = string.IsNullOrEmpty(_group) ? string.Empty : $"_{_group}";
			string baseName = $"{_languageId}{groupAppendix}.po";

			// Unity loads PO files via Resources.Load<TextAsset>("{lang}.po") which maps to
			// "{lang}.po.txt" on disk (Unity strips the last .txt extension for TextAssets).
			// Prefer the .po.txt canonical form; fall back to bare .po if that already exists.
			// Search all Resources directories (supports sub-folders like __Funatics/Resources/).
			foreach (string resourcesDir in FindResourcesDirectories())
			{
				string txtPath = Path.Combine(resourcesDir, baseName + ".txt");
				if (File.Exists(txtPath))
					return txtPath;

				string poPath = Path.Combine(resourcesDir, baseName);
				if (File.Exists(poPath))
					return poPath;
			}

			// Neither exists yet — create as .po.txt in the first (root) Resources directory.
			string defaultResourcesDir = Path.Combine(Application.dataPath, "Resources");
			return Path.Combine(defaultResourcesDir, baseName + ".txt");
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

			if (Directory.Exists(root))
				yield return root;

			if (Directory.Exists(dataPath))
			{
				foreach (string dir in Directory.GetDirectories(dataPath, "Resources", SearchOption.AllDirectories))
				{
					if (!string.Equals(dir, root, StringComparison.OrdinalIgnoreCase))
						yield return dir;
				}
			}
		}

		private static PoFile CreateEmptyPoFile(string _languageId)
		{
			return new PoFile
			{
				HasHeader    = true,
				HeaderMsgStr = DefaultHeaderMsgStr(_languageId),
				Entries      = new List<PoEntry>()
			};
		}

		private static string DefaultHeaderMsgStr(string _languageId)
			=> $"Language: {_languageId}\\nContent-Type: text/plain; charset=UTF-8\\nContent-Transfer-Encoding: 8bit\\n";

		private static string ConvertToUnityPath(string _absolutePath)
		{
			string dataPath = Path.GetFullPath(Application.dataPath).Replace('\\', '/');
			string full     = Path.GetFullPath(_absolutePath).Replace('\\', '/');
			if (full.StartsWith(dataPath, StringComparison.OrdinalIgnoreCase))
				return "Assets" + full.Substring(dataPath.Length);
			return null;
		}

		private static string NormalizeGroup(string _group) =>
			string.IsNullOrEmpty(_group) || _group == DEFAULT_LOCA_GROUP ? null : _group;
	}
}
