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
		private const string MENU_DEPS_DIRECT          = "Assets/Log Direct Dependencies";
		private const string MENU_DEPS_DIRECT_NO_PKG   = "Assets/Log Direct Dependencies (Project only)";
		private const string MENU_DEPENDENTS_CACHED    = "Assets/Log Direct Dependents";
		private const string MENU_DEPENDENTS_REBUILD   = "Assets/Log Direct Dependents (Rebuild Cache)";

		// Unity YAML file extensions that can contain serialized GUID references.
		private static readonly HashSet<string> s_YamlExtensions =
			new HashSet<string>(StringComparer.OrdinalIgnoreCase)
			{
				".unity", ".prefab", ".mat", ".asset", ".controller", ".anim",
				".overrideController", ".playable", ".mixer", ".terrainlayer",
				".renderTexture", ".spriteatlas", ".flare", ".guiskin",
				".physicMaterial", ".physicsMaterial2D", ".lighting",
			};

		// Matches any 32-hex-char GUID value after "guid:" in Unity YAML.
		private static readonly Regex s_GuidPattern =
			new Regex(@"\bguid:\s*([0-9a-fA-F]{32})\b", RegexOptions.Compiled);

		// -----------------------------------------------------------------------
		// Reverse-GUID index cache
		// Key:   GUID string (lowercase)
		// Value: sorted list of asset paths (under Assets/) that reference that GUID
		// null means "not yet built"
		// -----------------------------------------------------------------------
		private static Dictionary<string, List<string>> s_ReverseIndex;
		private static int    s_IndexedFileCount;
		private static string s_IndexTimestamp;

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
			LogDirectDependentsInternal(rebuildCache: false);

		[MenuItem(MENU_DEPENDENTS_REBUILD, priority = 2101)]
		private static void LogDirectDependentsRebuild() =>
			LogDirectDependentsInternal(rebuildCache: true);

		[MenuItem(MENU_DEPENDENTS_CACHED, validate = true)]
		[MenuItem(MENU_DEPENDENTS_REBUILD, validate = true)]
		private static bool Validate_Dependents() =>
			Selection.assetGUIDs != null && Selection.assetGUIDs.Length > 0;

		// -----------------------------------------------------------------------
		// Implementation: Dependents
		// -----------------------------------------------------------------------

		private static void LogDirectDependentsInternal(bool rebuildCache)
		{
			var selected = GetSelectedAssetPaths();
			if (selected.Count == 0)
			{
				Debug.LogWarning("[AssetDependencyLogger] No assets selected.");
				return;
			}

			// Ensure index is ready.
			if (rebuildCache || s_ReverseIndex == null)
				BuildReverseIndex();
			else
				Debug.Log($"[AssetDependencyLogger] Using cached GUID index " +
				          $"({s_IndexedFileCount} files indexed at {s_IndexTimestamp}).");

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
				if (s_ReverseIndex != null && s_ReverseIndex.TryGetValue(guid, out var refs))
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

		private static void BuildReverseIndex()
		{
			var index = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

			string[] candidates = AssetDatabase.GetAllAssetPaths()
				.Where(p =>
					p.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase) &&
					s_YamlExtensions.Contains(Path.GetExtension(p)))
				.ToArray();

			int total     = candidates.Length;
			bool cancelled = false;

			try
			{
				for (int i = 0; i < total; i++)
				{
					if (i % 50 == 0)
					{
						cancelled = EditorUtility.DisplayCancelableProgressBar(
							"Building GUID Index",
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
				Debug.LogWarning("[AssetDependencyLogger] GUID index build cancelled — cache not updated.");
				return;
			}

			// Sort each file list for stable output.
			foreach (var list in index.Values)
				list.Sort(StringComparer.OrdinalIgnoreCase);

			s_ReverseIndex     = index;
			s_IndexedFileCount = total;
			s_IndexTimestamp   = DateTime.Now.ToString("HH:mm:ss");

			Debug.Log($"[AssetDependencyLogger] GUID index built: {total} files scanned, " +
			          $"{index.Count} unique GUIDs indexed.");
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

	{
		private const string MENU_DEPS_DIRECT        = "Assets/Log Direct Dependencies";
		private const string MENU_DEPS_DIRECT_NO_PKG = "Assets/Log Direct Dependencies (Project only)";
		private const string MENU_DEPENDENTS_DIRECT  = "Assets/Log Direct Dependents";

		// Unity YAML file extensions that can contain serialized GUID references.
		private static readonly HashSet<string> s_YamlExtensions =
			new HashSet<string>(StringComparer.OrdinalIgnoreCase)
			{
				".unity", ".prefab", ".mat", ".asset", ".controller", ".anim",
				".overrideController", ".playable", ".mixer", ".terrainlayer",
				".renderTexture", ".spriteatlas", ".flare", ".guiskin",
				".physicMaterial", ".physicsMaterial2D", ".lighting",
			};

		// Matches any 32-hex-char GUID value after "guid:" in Unity YAML.
		private static readonly Regex s_GuidPattern =
			new Regex(@"\bguid:\s*([0-9a-fA-F]{32})\b", RegexOptions.Compiled);

		// -----------------------------------------------------------------------
		// Menu: Dependencies
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
		// Menu: Dependents
		// -----------------------------------------------------------------------

		[MenuItem(MENU_DEPENDENTS_DIRECT, priority = 2100)]
		private static void LogDirectDependents()
		{
			var selected = GetSelectedAssetPaths();
			if (selected.Count == 0)
			{
				Debug.LogWarning("[AssetDependencyLogger] No assets selected.");
				return;
			}

			// Map from GUID → asset path for all selected assets.
			var guidToPath = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			foreach (var path in selected)
			{
				string guid = AssetDatabase.AssetPathToGUID(path);
				if (!string.IsNullOrEmpty(guid))
					guidToPath[guid] = path;
			}

			if (guidToPath.Count == 0)
				return;

			// All YAML-based files under Assets/ are candidates for containing a reference.
			string[] candidates = AssetDatabase.GetAllAssetPaths()
				.Where(p =>
					p.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase) &&
					s_YamlExtensions.Contains(Path.GetExtension(p)))
				.ToArray();

			// dependentsMap: selectedPath → sorted list of files that reference it
			var dependentsMap = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
			foreach (var path in selected)
				dependentsMap[path] = new List<string>();

			int total = candidates.Length;
			bool cancelled = false;

			try
			{
				for (int i = 0; i < total; i++)
				{
					if (i % 50 == 0)
					{
						cancelled = EditorUtility.DisplayCancelableProgressBar(
							"Log Direct Dependents",
							$"Scanning {Path.GetFileName(candidates[i])}… ({i}/{total})",
							(float)i / total);
						if (cancelled)
							break;
					}

					string candidate = candidates[i];
					string fullPath  = YamlUtility.AssetPathToFullPath(candidate);
					if (!File.Exists(fullPath))
						continue;

					string text;
					try   { text = File.ReadAllText(fullPath); }
					catch { continue; }

					// Quick pre-check: skip files with no GUID references at all.
					if (!text.Contains("guid:"))
						continue;

					// Collect all GUIDs referenced in this file (one regex pass).
					var foundGuids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
					foreach (Match m in s_GuidPattern.Matches(text))
						foundGuids.Add(m.Groups[1].Value);

					// Record this file as a dependent for every selected asset it mentions.
					foreach (var kvp in guidToPath)
					{
						if (foundGuids.Contains(kvp.Key))
							dependentsMap[kvp.Value].Add(candidate);
					}
				}
			}
			finally
			{
				EditorUtility.ClearProgressBar();
			}

			// Log results.
			foreach (var kvp in dependentsMap)
			{
				kvp.Value.Sort(StringComparer.OrdinalIgnoreCase);
				var sb = new StringBuilder();
				sb.AppendLine($"[Direct Dependents] Who references: {kvp.Key}");
				sb.AppendLine($"Count: {kvp.Value.Count}");
				foreach (var d in kvp.Value)
					sb.AppendLine($"  - {d}");
				Debug.Log(sb.ToString());
			}

			if (!cancelled)
				Debug.Log($"[AssetDependencyLogger] Log Direct Dependents finished. " +
				          $"Scanned {total} candidate file(s).");
		}

		[MenuItem(MENU_DEPENDENTS_DIRECT, validate = true)]
		private static bool Validate_Dependents() =>
			Selection.assetGUIDs != null && Selection.assetGUIDs.Length > 0;

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
