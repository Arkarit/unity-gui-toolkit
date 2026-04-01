using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Editor
{
	/// <summary>
	/// Adds right-click context menu items to the Project window for logging asset dependencies
	/// and dependents.
	/// <para>
	/// <b>Dependencies</b> (what the selected asset needs) use
	/// <see cref="AssetDatabase.GetDependencies"/> — the authoritative Unity source.<br/>
	/// <b>Dependents</b> (who references the selected asset) use a cached reverse GUID index.
	/// On first use (or after "Rebuild Cache") the tool reads every YAML-format file under
	/// <c>Assets/</c> once and builds an in-memory map <c>GUID → files that reference it</c>.
	/// Subsequent lookups are O(1) — no file IO at query time.
	/// </para>
	/// </summary>
	[EditorAware]
	internal static class AssetDependencyLogger
	{
		private const string MENU_DEPS_DIRECT               = "Assets/Log Direct Dependencies";
		private const string MENU_DEPS_DIRECT_NO_PKG        = "Assets/Log Direct Dependencies (Project only)";
		private const string MENU_DEPENDENTS_CACHED         = "Assets/Log Direct Dependents";
		private const string MENU_DEPENDENTS_REBUILD        = "Assets/Log Direct Dependents (Rebuild Full Cache)";
		private const string MENU_DEPENDENTS_REBUILD_SCRIPTS = "Assets/Log Direct Dependents (Rebuild Scripts-Only Cache)";

		// All Unity YAML file extensions that can contain serialized GUID references.
		private static readonly HashSet<string> s_YamlExtensions =
			new HashSet<string>(StringComparer.OrdinalIgnoreCase)
			{
				".unity", ".prefab", ".mat", ".asset", ".controller", ".anim",
				".overrideController", ".playable", ".mixer", ".terrainlayer",
				".renderTexture", ".spriteatlas", ".flare", ".guiskin",
				".physicMaterial", ".physicsMaterial2D", ".lighting",
			};

		// Subset: only file types that can contain !u!114 MonoBehaviour blocks.
		// Used by tools that query component/script references (e.g. ReplaceComponentsWindow).
		private static readonly HashSet<string> s_ScriptBearingExtensions =
			new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".prefab", ".unity", ".asset" };

		// Matches any 32-hex-char GUID value after "guid:" in Unity YAML.
		private static readonly Regex s_GuidPattern =
			new Regex(@"\bguid:\s*([0-9a-fA-F]{32})\b", RegexOptions.Compiled);

		// -----------------------------------------------------------------------
		// Reverse-GUID index cache — full (all YAML types)
		// Key:   GUID string  |  Value: sorted list of asset paths that reference it
		// null means "not yet built"
		// -----------------------------------------------------------------------
		private static Dictionary<string, List<string>> s_ReverseIndex;
		private static int    s_IndexedFileCount;
		private static string s_IndexTimestamp;

		// Reverse-GUID index cache — scripts-only (.prefab / .unity / .asset)
		private static Dictionary<string, List<string>> s_ScriptBearingIndex;
		private static int    s_ScriptBearingFileCount;
		private static string s_ScriptBearingTimestamp;

		// -----------------------------------------------------------------------
		// Menu: Dependencies (no caching needed — fast by nature)
		// -----------------------------------------------------------------------

		[MenuItem(MENU_DEPS_DIRECT, priority = 2000)]
		private static void LogDirectDependencies() =>
			LogDependenciesInternal(includePackages: true);

		[MenuItem(MENU_DEPS_DIRECT_NO_PKG, priority = 2001)]
		private static void LogDirectDependenciesProjectOnly() =>
			LogDependenciesInternal(includePackages: false);

		[MenuItem(MENU_DEPS_DIRECT, validate = true)]
		[MenuItem(MENU_DEPS_DIRECT_NO_PKG, validate = true)]
		private static bool Validate_Dependencies() =>
			Selection.assetGUIDs != null && Selection.assetGUIDs.Length > 0;

		// -----------------------------------------------------------------------
		// Menu: Dependents — cached
		// -----------------------------------------------------------------------

		[MenuItem(MENU_DEPENDENTS_CACHED, priority = 2100)]
		private static void LogDirectDependentsCached() =>
			LogDirectDependentsInternal(rebuildCache: false, scriptsOnly: false);

		[MenuItem(MENU_DEPENDENTS_REBUILD, priority = 2101)]
		private static void LogDirectDependentsRebuild() =>
			LogDirectDependentsInternal(rebuildCache: true, scriptsOnly: false);

		[MenuItem(MENU_DEPENDENTS_REBUILD_SCRIPTS, priority = 2102)]
		private static void LogDirectDependentsRebuildScripts() =>
			LogDirectDependentsInternal(rebuildCache: true, scriptsOnly: true);

		[MenuItem(MENU_DEPENDENTS_CACHED, validate = true)]
		[MenuItem(MENU_DEPENDENTS_REBUILD, validate = true)]
		[MenuItem(MENU_DEPENDENTS_REBUILD_SCRIPTS, validate = true)]
		private static bool Validate_Dependents() =>
			Selection.assetGUIDs != null && Selection.assetGUIDs.Length > 0;

		// -----------------------------------------------------------------------
		// Implementation: Dependents
		// -----------------------------------------------------------------------

		private static void LogDirectDependentsInternal(bool rebuildCache, bool scriptsOnly)
		{
			var selected = GetSelectedAssetPaths();
			if (selected.Count == 0)
			{
				Debug.LogWarning("[AssetDependencyLogger] No assets selected.");
				return;
			}

			var index = scriptsOnly ? s_ScriptBearingIndex : s_ReverseIndex;

			// Ensure index is ready.
			if (rebuildCache || index == null)
			{
				BuildReverseIndex(scriptsOnly);
				index = scriptsOnly ? s_ScriptBearingIndex : s_ReverseIndex;
			}
			else
			{
				string label = scriptsOnly ? "scripts-only" : "full";
				int    count = scriptsOnly ? s_ScriptBearingFileCount : s_IndexedFileCount;
				string ts    = scriptsOnly ? s_ScriptBearingTimestamp  : s_IndexTimestamp;
				Debug.Log($"[AssetDependencyLogger] Using cached GUID index ({label}, " +
				          $"{count} files indexed at {ts}).");
			}

			// Query index for each selected asset.
			foreach (var path in selected)
			{
				string guid = AssetDatabase.AssetPathToGUID(path);
				if (string.IsNullOrEmpty(guid))
				{
					Debug.LogWarning($"[AssetDependencyLogger] Could not resolve GUID for: {path}");
					continue;
				}

				List<string> dependents;
				if (index != null && index.TryGetValue(guid, out var refs))
					dependents = new List<string>(refs);
				else
					dependents = new List<string>();

				var sb = new StringBuilder();
				sb.AppendLine($"[Direct Dependents] Who references: {path}");
				sb.AppendLine($"Count: {dependents.Count}");
				foreach (var d in dependents)
					sb.AppendLine($"  - {d}");
				Debug.Log(sb.ToString());
			}
		}

		// -----------------------------------------------------------------------
		// Index building
		// -----------------------------------------------------------------------

		private static void BuildReverseIndex(bool scriptsOnly = false)
		{
			HashSet<string> extensions = scriptsOnly ? s_ScriptBearingExtensions : s_YamlExtensions;
			string label = scriptsOnly ? "Scripts-Only" : "Full";

			var index = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

			string[] candidates = AssetDatabase.GetAllAssetPaths()
				.Where(p =>
					p.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase) &&
					extensions.Contains(Path.GetExtension(p)))
				.ToArray();

			int total      = candidates.Length;
			bool cancelled = false;

			try
			{
				for (int i = 0; i < total; i++)
				{
					if (i % 50 == 0)
					{
						cancelled = EditorUtility.DisplayCancelableProgressBar(
							$"Building GUID Index ({label})",
							$"Scanning {Path.GetFileName(candidates[i])}… ({i}/{total})",
							(float)i / total);
						if (cancelled)
							break;
					}

					string fullPath = YamlUtility.AssetPathToFullPath(candidates[i]);
					if (!File.Exists(fullPath))
						continue;

					string text;
					try   { text = File.ReadAllText(fullPath); }
					catch { continue; }

					// Quick pre-check: skip files with no GUID references at all.
					if (!text.Contains("guid:"))
						continue;

					string candidate = candidates[i];
					foreach (Match m in s_GuidPattern.Matches(text))
					{
						string refGuid = m.Groups[1].Value;
						if (!index.TryGetValue(refGuid, out var files))
						{
							files = new List<string>();
							index[refGuid] = files;
						}
						// Avoid adding the same file twice (a file may reference the same GUID multiple times).
						if (files.Count == 0 || files[files.Count - 1] != candidate)
							files.Add(candidate);
					}
				}
			}
			finally
			{
				EditorUtility.ClearProgressBar();
			}

			if (cancelled)
			{
				Debug.LogWarning($"[AssetDependencyLogger] GUID index ({label}) build cancelled — cache not updated.");
				return;
			}

			// Sort each file list for stable output.
			foreach (var list in index.Values)
				list.Sort(StringComparer.OrdinalIgnoreCase);

			if (scriptsOnly)
			{
				s_ScriptBearingIndex     = index;
				s_ScriptBearingFileCount = total;
				s_ScriptBearingTimestamp = DateTime.Now.ToString("HH:mm:ss");
			}
			else
			{
				s_ReverseIndex     = index;
				s_IndexedFileCount = total;
				s_IndexTimestamp   = DateTime.Now.ToString("HH:mm:ss");
			}

			Debug.Log($"[AssetDependencyLogger] GUID index ({label}) built: {total} files scanned, " +
			          $"{index.Count} unique GUIDs indexed.");
		}

		// -----------------------------------------------------------------------
		// Shared API used by other editor tools
		// -----------------------------------------------------------------------

		/// <summary>
		/// Ensures the reverse GUID index is built. Builds it now if not already cached.
		/// Pass <paramref name="scriptsOnly"/> = <c>true</c> to use the faster scripts-only index
		/// (prefabs, scenes, ScriptableObjects) when only MonoBehaviour references are needed.
		/// Returns <c>true</c> if the index is ready; <c>false</c> if the user cancelled.
		/// </summary>
		internal static bool EnsureIndex(bool scriptsOnly = false)
		{
			var index = scriptsOnly ? s_ScriptBearingIndex : s_ReverseIndex;
			if (index != null)
				return true;
			BuildReverseIndex(scriptsOnly);
			return (scriptsOnly ? s_ScriptBearingIndex : s_ReverseIndex) != null;
		}

		/// <summary>
		/// Returns <c>true</c> if <paramref name="assetPath"/> is referenced by at least one
		/// other asset in the project according to the cached index.
		/// Always returns <c>true</c> when the index has not been built (safe default).
		/// </summary>
		internal static bool HasDependents(string assetPath, bool scriptsOnly = false)
		{
			var index = scriptsOnly ? s_ScriptBearingIndex : s_ReverseIndex;
			if (index == null)
				return true;
			string guid = AssetDatabase.AssetPathToGUID(assetPath);
			return !string.IsNullOrEmpty(guid)
				&& index.TryGetValue(guid, out var list)
				&& list.Count > 0;
		}

		/// <summary>
		/// Returns all asset paths that directly reference the asset identified by <paramref name="guid"/>.
		/// Returns an empty collection if the index has not been built or the GUID has no dependents.
		/// </summary>
		internal static IReadOnlyList<string> GetDependents(string guid, bool scriptsOnly = false)
		{
			var index = scriptsOnly ? s_ScriptBearingIndex : s_ReverseIndex;
			if (index == null || string.IsNullOrEmpty(guid))
				return Array.Empty<string>();
			return index.TryGetValue(guid, out var list)
				? (IReadOnlyList<string>)list
				: Array.Empty<string>();
		}

		// -----------------------------------------------------------------------
		// Implementation: Dependencies
		// -----------------------------------------------------------------------

		private static void LogDependenciesInternal(bool includePackages)
		{
			var selected = GetSelectedAssetPaths();
			if (selected.Count == 0)
			{
				Debug.LogWarning("[AssetDependencyLogger] No assets selected.");
				return;
			}

			foreach (var path in selected)
			{
				var deps = AssetDatabase.GetDependencies(path, recursive: false)
					.Distinct(StringComparer.OrdinalIgnoreCase)
					.OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
					.ToList();

				if (!includePackages)
					deps = deps.Where(p => p.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase)).ToList();

				deps.Remove(path);
				deps.Insert(0, path);

				var sb = new StringBuilder();
				sb.AppendLine($"[Direct Dependencies] For: {path}");
				sb.AppendLine($"Count (including self): {deps.Count}");
				foreach (var dep in deps)
				{
					var type = AssetDatabase.GetMainAssetTypeAtPath(dep);
					sb.AppendLine($"  - {dep}  ({(type != null ? type.Name : "<UnknownType>")})");
				}

				Debug.Log(sb.ToString());
			}

			Debug.Log("[AssetDependencyLogger] Log Direct Dependencies finished.");
		}

		// -----------------------------------------------------------------------

		private static List<string> GetSelectedAssetPaths()
		{
			var guids = Selection.assetGUIDs ?? Array.Empty<string>();
			var list  = new List<string>(guids.Length);
			foreach (var guid in guids)
			{
				var p = AssetDatabase.GUIDToAssetPath(guid);
				if (!string.IsNullOrEmpty(p))
					list.Add(p);
			}
			return list;
		}
	}
}
