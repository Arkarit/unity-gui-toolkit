using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace GuiToolkit.Editor
{
	[EditorAware]
	public class ReplaceComponentsWindow : EditorWindow
	{
		private const string k_PendingReloadPath    = "GuiToolkit.ReplaceComponents.PendingReloadPath";
		private const string k_PendingReloadIsScene = "GuiToolkit.ReplaceComponents.PendingReloadIsScene";

		private MonoScript m_SourceScript;
		private MonoScript m_TargetScript;
		private bool m_EntireProject = false;

		[MenuItem(StringConstants.REPLACE_COMPONENTS_WINDOW)]
		public static void ShowWindow()
		{
			var w = GetWindow<ReplaceComponentsWindow>("Replace Components");
			w.minSize = new Vector2(440, 140);
		}

		private void OnGUI()
		{
			EditorGUILayout.LabelField("Replace Component (YAML-based)", EditorStyles.boldLabel);

			m_SourceScript = (MonoScript)EditorGUILayout.ObjectField(
				"Source Component", m_SourceScript, typeof(MonoScript), false);

			m_TargetScript = (MonoScript)EditorGUILayout.ObjectField(
				"Target Component", m_TargetScript, typeof(MonoScript), false);

			EditorGUILayout.Space();
			m_EntireProject = EditorGUILayout.Toggle("Entire Project", m_EntireProject);
			EditorGUILayout.HelpBox(
				m_EntireProject
					? "Processes all prefabs and scenes in Assets/."
					: "Processes the current scene/prefab and all prefabs referenced in it.",
				MessageType.Info);

			EditorGUILayout.Space();

			using (new EditorGUI.DisabledScope(!IsValidComponentScript(m_SourceScript) ||
			                                   !IsValidComponentScript(m_TargetScript)))
			{
				if (GUILayout.Button("Replace Now"))
					DoReplace();
			}
		}

		// -----------------------------------------------------------------------

		private void DoReplace()
		{
			var srcType = m_SourceScript.GetClass();
			var dstType = m_TargetScript.GetClass();

			if (!typeof(Component).IsAssignableFrom(srcType) || !typeof(Component).IsAssignableFrom(dstType))
			{
				UiLog.LogError("Both source and target must inherit from UnityEngine.Component.");
				return;
			}

			if (srcType == dstType)
			{
				UiLog.LogError("Source and target must be different types.");
				return;
			}

			if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(m_SourceScript, out string srcGuid, out long _) ||
			    string.IsNullOrEmpty(srcGuid))
			{
				UiLog.LogError("Cannot resolve GUID for source script.");
				return;
			}

			if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(m_TargetScript, out string dstGuid, out long _) ||
			    string.IsNullOrEmpty(dstGuid))
			{
				UiLog.LogError("Cannot resolve GUID for target script.");
				return;
			}

			string scope = m_EntireProject ? "entire project" : "current scene/prefab";
			string msg = $"Replace all '{srcType.Name}' → '{dstType.Name}' in {scope}.\n" +
				"Fields with matching name and type are transferred automatically by Unity.\n" +
				"Non-matching fields will use the target component's default values.\n\n" +
				"This operation modifies YAML and .cs files directly and cannot be undone. " +
				"Make sure you have a clean working copy.\n\nContinue?";

			if (!EditorUtility.DisplayDialog("Replace Components", msg, "Replace", "Cancel"))
				return;

			var assetPaths = CollectAssetPaths(srcType);
			if (assetPaths.Count == 0)
			{
				EditorUtility.DisplayDialog("Replace Components", "No assets found for the selected scope.", "OK");
				return;
			}

			// --- Phase 1: build cross-file reference index ---
			var crossFileIndex = BuildProjectWideCrossFileIndex("Replace Components", assetPaths);

			string searchPattern = $"guid: {srcGuid}, type: 3";
			string replaceWith   = $"guid: {dstGuid}, type: 3";

			int filesModified      = 0;
			int componentsReplaced = 0;
			int errors             = 0;

			// Aggregate of all scriptGuid -> fieldNames across all processed assets.
			var globalScriptFieldMap = new Dictionary<string, HashSet<string>>();

			// --- Phase 2: YAML swap per asset ---
			AssetDatabase.StartAssetEditing();
			try
			{
				for (int i = 0; i < assetPaths.Count; i++)
				{
					string assetPath = assetPaths[i];
					EditorUtility.DisplayProgressBar("Replace Components – YAML",
						Path.GetFileName(assetPath), (float)i / assetPaths.Count);

					try
					{
						string fullPath = YamlUtility.AssetPathToFullPath(assetPath);
						string yaml     = File.ReadAllText(fullPath);

						if (!yaml.Contains(searchPattern))
							continue;

						// Find the local IDs of all TypeA component blocks in this file.
						var convertedIds = FindSourceComponentLocalIds(yaml, srcGuid);

						// Collect which script fields reference these specific instances.
						var perComponentMap = FindReferencingScriptFieldsPerComponent(yaml, convertedIds);
						MergeCrossFileRefs(assetPath, convertedIds, crossFileIndex, perComponentMap);
						var localScriptFieldMap = AggregateScriptFieldMap(perComponentMap);

						// Merge into global map for C# update phase.
						foreach (var kvp in localScriptFieldMap)
						{
							if (!globalScriptFieldMap.TryGetValue(kvp.Key, out var set))
								globalScriptFieldMap[kvp.Key] = set = new HashSet<string>();
							foreach (string fn in kvp.Value)
								set.Add(fn);
						}

						int count      = CountOccurrences(yaml, searchPattern);
						string newYaml = yaml.Replace(searchPattern, replaceWith);

						File.WriteAllText(fullPath, newYaml);
						AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

						componentsReplaced += count;
						filesModified++;
					}
					catch (Exception ex)
					{
						Debug.LogError($"[ReplaceComponentsWindow] Error processing {assetPath}: {ex}");
						errors++;
					}
				}
			}
			finally
			{
				EditorUtility.ClearProgressBar();
				AssetDatabase.StopAssetEditing();
			}

			// --- Phase 3: update C# field type declarations ---
			string srcTypeName   = srcType.Name;
			string dstTypeName   = dstType.Name;
			string dstNamespace  = dstType.Namespace ?? string.Empty;
			int csFilesModified  = 0;
			int scriptIndex      = 0;
			foreach (var kvp in globalScriptFieldMap)
			{
				EditorUtility.DisplayProgressBar("Replace Components – C# fields",
					$"Updating scripts… ({scriptIndex + 1}/{globalScriptFieldMap.Count})",
					(float)scriptIndex / Math.Max(globalScriptFieldMap.Count, 1));
				scriptIndex++;
				if (TryUpdateCSharpScriptFields(kvp.Key, kvp.Value, srcTypeName, dstTypeName, dstNamespace))
					csFilesModified++;
			}
			EditorUtility.ClearProgressBar();

			string summary = $"Replaced: {componentsReplaced} '{srcType.Name}' component(s) → '{dstType.Name}'\n" +
			                 $"YAML files:  {filesModified} modified\n" +
			                 $"C# scripts:  {csFilesModified} field-type(s) updated";
			if (errors > 0)
				summary += $"\nErrors:  {errors} (see Console for details)";

			EditorUtility.DisplayDialog("Replace Components – Done", summary, "OK");
			ReloadCurrentContext(csFilesModified > 0);
		}

		// -----------------------------------------------------------------------
		// Reference-finding helpers (adapted from LegacyTextToLocalizedTmpConverter)
		// -----------------------------------------------------------------------

		/// <summary>
		/// Returns the YAML local file IDs of all MonoBehaviour blocks whose m_Script GUID matches
		/// <paramref name="srcGuid"/> — i.e. all instances of the source component type in this file.
		/// </summary>
		private static HashSet<long> FindSourceComponentLocalIds(string yaml, string srcGuid)
		{
			var result   = new HashSet<long>();
			var blocks   = Regex.Split(yaml, @"(?=^--- !u!114 &)", RegexOptions.Multiline);
			var anchorRx = new Regex(@"--- !u!114 &(\d+)");
			var scriptRx = new Regex($@"m_Script:\s*\{{[^}}]*\bguid:\s*{Regex.Escape(srcGuid)}[^}}]*\}}");
			foreach (string block in blocks)
			{
				if (!block.StartsWith("--- !u!114 &", StringComparison.Ordinal))
					continue;
				if (!scriptRx.IsMatch(block))
					continue;
				var m = anchorRx.Match(block);
				if (m.Success && long.TryParse(m.Groups[1].Value, out long localId))
					result.Add(localId);
			}
			return result;
		}

		/// <summary>
		/// Scans all MonoBehaviour blocks in <paramref name="yaml"/> and returns a two-level map:
		/// componentId -> (scriptGuid -> set of field names that reference that component).
		/// </summary>
		private static Dictionary<long, Dictionary<string, HashSet<string>>> FindReferencingScriptFieldsPerComponent(
			string yaml, HashSet<long> convertedIds)
		{
			var result      = new Dictionary<long, Dictionary<string, HashSet<string>>>();
			var blockSplit  = Regex.Split(yaml, @"(?=^--- !u!114 &)", RegexOptions.Multiline);
			var scriptGuidRx = new Regex(@"m_Script:\s*\{[^}]*\bguid:\s*([a-fA-F0-9]+)[^}]*\}", RegexOptions.Compiled);
			var fieldRefRx   = new Regex(@"^\s+(\w+):\s*\{fileID:\s*(-?\d+)\}", RegexOptions.Compiled | RegexOptions.Multiline);

			foreach (string block in blockSplit)
			{
				if (!block.StartsWith("--- !u!114 &", StringComparison.Ordinal))
					continue;
				var scriptMatch = scriptGuidRx.Match(block);
				if (!scriptMatch.Success)
					continue;
				string scriptGuid = scriptMatch.Groups[1].Value;
				foreach (Match fm in fieldRefRx.Matches(block))
				{
					if (!long.TryParse(fm.Groups[2].Value, out long fileId))
						continue;
					if (!convertedIds.Contains(fileId))
						continue;
					string fieldName = fm.Groups[1].Value;
					if (!result.TryGetValue(fileId, out var scriptMap))
						result[fileId] = scriptMap = new Dictionary<string, HashSet<string>>();
					if (!scriptMap.TryGetValue(scriptGuid, out var fieldSet))
						scriptMap[scriptGuid] = fieldSet = new HashSet<string>();
					fieldSet.Add(fieldName);
				}
			}
			return result;
		}

		/// <summary>Flattens a per-component map to a scriptGuid -> fieldNames map.</summary>
		private static Dictionary<string, HashSet<string>> AggregateScriptFieldMap(
			Dictionary<long, Dictionary<string, HashSet<string>>> perComponentMap)
		{
			var aggregate = new Dictionary<string, HashSet<string>>();
			foreach (var scriptMap in perComponentMap.Values)
			{
				foreach (var kvp in scriptMap)
				{
					if (!aggregate.TryGetValue(kvp.Key, out var set))
						aggregate[kvp.Key] = set = new HashSet<string>();
					foreach (string fn in kvp.Value)
						set.Add(fn);
				}
			}
			return aggregate;
		}

		/// <summary>
		/// Merges cross-file refs from <paramref name="crossFileIndex"/> that target components in
		/// <paramref name="assetPath"/> into <paramref name="perComponentMap"/>.
		/// </summary>
		private static void MergeCrossFileRefs(
			string assetPath,
			HashSet<long> convertedIds,
			Dictionary<(string, long), Dictionary<string, HashSet<string>>> crossFileIndex,
			Dictionary<long, Dictionary<string, HashSet<string>>> perComponentMap)
		{
			string assetGuid = AssetDatabase.AssetPathToGUID(assetPath);
			if (string.IsNullOrEmpty(assetGuid))
				return;
			foreach (long localId in convertedIds)
			{
				var key = (assetGuid, localId);
				if (!crossFileIndex.TryGetValue(key, out var scriptMap))
					continue;
				if (!perComponentMap.TryGetValue(localId, out var existing))
					perComponentMap[localId] = existing = new Dictionary<string, HashSet<string>>();
				foreach (var kvp in scriptMap)
				{
					if (!existing.TryGetValue(kvp.Key, out var fieldSet))
						existing[kvp.Key] = fieldSet = new HashSet<string>();
					foreach (string fn in kvp.Value)
						fieldSet.Add(fn);
				}
			}
		}

		/// <summary>
		/// Builds an index of cross-file serialized field references to MonoBehaviours in
		/// <paramref name="targetAssetPaths"/>. Uses the <see cref="AssetDependencyLogger"/> cached
		/// reverse-GUID index to scan only the files that actually reference the targets, instead of
		/// every prefab and scene in the project.
		/// Key: (targetAssetGuid, targetLocalId). Value: scriptGuid -> field names.
		/// </summary>
		private static Dictionary<(string, long), Dictionary<string, HashSet<string>>> BuildProjectWideCrossFileIndex(
			string progressTitle, IReadOnlyList<string> targetAssetPaths)
		{
			var index = new Dictionary<(string, long), Dictionary<string, HashSet<string>>>();

			// Resolve only the files that actually reference our target assets.
			var targetPathSet = new HashSet<string>(targetAssetPaths, StringComparer.OrdinalIgnoreCase);
			var candidates    = new List<string>();

			if (AssetDependencyLogger.EnsureIndex())
			{
				var candidateSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
				foreach (string assetPath in targetAssetPaths)
				{
					string assetGuid = AssetDatabase.AssetPathToGUID(assetPath);
					if (string.IsNullOrEmpty(assetGuid)) continue;
					foreach (string dep in AssetDependencyLogger.GetDependents(assetGuid))
					{
						if (!targetPathSet.Contains(dep))
							candidateSet.Add(dep);
					}
				}
				candidates.AddRange(candidateSet);
			}
			else
			{
				// Fallback: scan all prefabs + scenes under Assets/
				foreach (string guid in AssetDatabase.FindAssets("t:Prefab t:Scene"))
				{
					string path = AssetDatabase.GUIDToAssetPath(guid);
					if (path.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase) && !targetPathSet.Contains(path))
						candidates.Add(path);
				}
			}

			int total = candidates.Count;
			var scriptGuidRx = new Regex(@"m_Script:\s*\{[^}]*\bguid:\s*([a-fA-F0-9]+)[^}]*\}", RegexOptions.Compiled);
			var crossRefRx   = new Regex(@"^\s+(\w+):\s*\{\s*fileID:\s*(-?\d+)\s*,\s*guid:\s*([a-fA-F0-9]+)",
				RegexOptions.Compiled | RegexOptions.Multiline);
			try
			{
				for (int i = 0; i < total; i++)
				{
					string assetPath = candidates[i];
					EditorUtility.DisplayProgressBar(progressTitle,
						$"Building cross-file index… ({i + 1}/{total})",
						(float)i / Math.Max(total, 1));
					if (!assetPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
						continue;
					string fullPath = YamlUtility.AssetPathToFullPath(assetPath);
					if (!File.Exists(fullPath))
						continue;
					string yaml;
					try { yaml = File.ReadAllText(fullPath); }
					catch { continue; }
					var blocks = Regex.Split(yaml, @"(?=^--- !u!114 &)", RegexOptions.Multiline);
					foreach (string block in blocks)
					{
						if (!block.StartsWith("--- !u!114 &", StringComparison.Ordinal))
							continue;
						var scriptMatch = scriptGuidRx.Match(block);
						if (!scriptMatch.Success)
							continue;
						string scriptGuid = scriptMatch.Groups[1].Value;
						foreach (Match m in crossRefRx.Matches(block))
						{
							string fieldName  = m.Groups[1].Value;
							string targetGuid = m.Groups[3].Value;
							if (!long.TryParse(m.Groups[2].Value, out long fileId))
								continue;
							var key = (targetGuid, fileId);
							if (!index.TryGetValue(key, out var scriptMap))
								index[key] = scriptMap = new Dictionary<string, HashSet<string>>();
							if (!scriptMap.TryGetValue(scriptGuid, out var fieldSet))
								scriptMap[scriptGuid] = fieldSet = new HashSet<string>();
							fieldSet.Add(fieldName);
						}
					}
				}
			}
			finally { EditorUtility.ClearProgressBar(); }
			return index;
		}

		/// <summary>
		/// Updates C# field type declarations in the script identified by <paramref name="scriptGuid"/>.
		/// Only the specific <paramref name="fieldNames"/> are retargeted from
		/// <paramref name="srcTypeName"/> to <paramref name="dstTypeName"/>.
		/// A <c>using</c> directive for <paramref name="dstNamespace"/> is added if absent.
		/// </summary>
		private static bool TryUpdateCSharpScriptFields(
			string scriptGuid, IEnumerable<string> fieldNames,
			string srcTypeName, string dstTypeName, string dstNamespace)
		{
			string assetPath = AssetDatabase.GUIDToAssetPath(scriptGuid);
			if (string.IsNullOrEmpty(assetPath) || !assetPath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
			{
				Debug.LogWarning($"[ReplaceComponentsWindow] Cannot resolve script for guid={scriptGuid}");
				return false;
			}
			if (!assetPath.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
			{
				Debug.LogWarning($"[ReplaceComponentsWindow] Skipping read-only package script: {assetPath}");
				return false;
			}
			string fullPath = YamlUtility.AssetPathToFullPath(assetPath);
			if (!File.Exists(fullPath))
				return false;

			string source  = File.ReadAllText(fullPath);
			string updated = source;
			foreach (string fieldName in fieldNames)
			{
				updated = Regex.Replace(
					updated,
					$@"(?<!\w){Regex.Escape(srcTypeName)}(\s+{Regex.Escape(fieldName)}(?!\w))",
					dstTypeName + "$1");
			}

			if (updated == source)
				return false;

			// Add 'using DstNamespace;' if needed.
			if (!string.IsNullOrEmpty(dstNamespace) &&
			    !Regex.IsMatch(updated, $@"^\s*using\s+{Regex.Escape(dstNamespace)}\s*;", RegexOptions.Multiline))
			{
				updated = Regex.Replace(
					updated,
					@"((?:^\s*using\s+[^\r\n]+[\r\n]+)+)",
					m => m.Value + $"using {dstNamespace};\n",
					RegexOptions.Multiline);
			}

			File.WriteAllText(fullPath, updated);
			AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
			Debug.Log($"[ReplaceComponentsWindow] Updated C# field types in {assetPath}");
			return true;
		}

		// -----------------------------------------------------------------------

		/// <summary>
		/// Schedules a reload of the currently open scene or prefab stage after all pending
		/// asset imports and (if <paramref name="expectDomainReload"/> is <c>true</c>) the
		/// upcoming domain reload have completed.
		/// </summary>
		private static void ReloadCurrentContext(bool expectDomainReload)
		{
			var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
			if (prefabStage != null)
			{
				string path = prefabStage.assetPath;
				if (expectDomainReload)
				{
					// Persist across domain reload; [InitializeOnLoadMethod] fires the reload after.
					SessionState.SetString(k_PendingReloadPath, path);
					SessionState.SetBool(k_PendingReloadIsScene, false);
				}
				else
				{
					EditorApplication.delayCall += () => ApplyPendingContextReload(path, false);
				}
				return;
			}

			var scene = SceneManager.GetActiveScene();
			if (scene.IsValid() && !string.IsNullOrEmpty(scene.path))
			{
				string path = scene.path;
				if (expectDomainReload)
				{
					SessionState.SetString(k_PendingReloadPath, path);
					SessionState.SetBool(k_PendingReloadIsScene, true);
				}
				else
				{
					EditorApplication.delayCall += () => ApplyPendingContextReload(path, true);
				}
			}
		}

		/// <summary>
		/// Called automatically after every domain reload via <see cref="InitializeOnLoadMethodAttribute"/>.
		/// Checks if a context reload was deferred before the domain reload and executes it now.
		/// </summary>
		[InitializeOnLoadMethod]
		private static void OnAfterDomainReload()
		{
			string path = SessionState.GetString(k_PendingReloadPath, null);
			if (string.IsNullOrEmpty(path))
				return;
			SessionState.EraseString(k_PendingReloadPath);
			bool isScene = SessionState.GetBool(k_PendingReloadIsScene, false);
			SessionState.EraseBool(k_PendingReloadIsScene);
			EditorApplication.delayCall += () => ApplyPendingContextReload(path, isScene);
		}

		/// <summary>
		/// Forces Unity to re-read <paramref name="assetPath"/> from disk and then reopens the
		/// active prefab stage or scene so the Inspector reflects the rewritten YAML.
		/// </summary>
		private static void ApplyPendingContextReload(string assetPath, bool isScene)
		{
			AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
			if (isScene)
			{
				EditorSceneManager.OpenScene(assetPath, OpenSceneMode.Single);
			}
			else
			{
				var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
				if (prefabStage != null)
					StageUtility.GoBackToPreviousStage();
				EditorApplication.delayCall += () =>
				{
					var go = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
					if (go != null)
						AssetDatabase.OpenAsset(go);
				};
			}
		}

		/// <summary>
		/// Collects the YAML asset paths to process based on the current scope setting.
		/// For "entire project": all prefabs + scenes under Assets/.
		/// For "current scene/prefab": the active file + all prefabs that contain the source component.
		/// </summary>
		private List<string> CollectAssetPaths(Type srcType)
		{
			if (m_EntireProject)
			{
				var list = new List<string>();
				foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets" }))
					list.Add(AssetDatabase.GUIDToAssetPath(guid));
				foreach (string guid in AssetDatabase.FindAssets("t:Scene", new[] { "Assets" }))
					list.Add(AssetDatabase.GUIDToAssetPath(guid));
				return list;
			}

			// "Current" scope: the open scene/prefab file + all referenced prefab assets.
			var result    = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			var plainObjs = new List<Component>();

			var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
			if (prefabStage != null)
			{
				// Inside prefab edit mode — process just the prefab asset.
				result.Add(prefabStage.assetPath);
			}
			else
			{
				// Active scene: collect referenced prefab paths + mark scene for direct processing.
				var scene = SceneManager.GetActiveScene();
				if (scene.IsValid() && !string.IsNullOrEmpty(scene.path))
					result.Add(scene.path);

				foreach (var comp in Object.FindObjectsOfType(srcType, true))
				{
					var component = comp as Component;
					if (component == null) continue;

					if (PrefabUtility.IsPartOfPrefabInstance(component.gameObject))
					{
						string prefabPath = GetDefiningPrefabPath(component);
						if (!string.IsNullOrEmpty(prefabPath))
							result.Add(prefabPath);
					}
					else
					{
						plainObjs.Add(component);
					}
				}
			}

			return new List<string>(result);
		}

		private static string GetDefiningPrefabPath(Component c)
		{
			var original = PrefabUtility.GetCorrespondingObjectFromOriginalSource(c);
			if (original == null) return null;
			return AssetDatabase.GetAssetPath(original);
		}

		private static int CountOccurrences(string text, string pattern)
		{
			int count = 0, pos = 0;
			while ((pos = text.IndexOf(pattern, pos, StringComparison.Ordinal)) >= 0)
			{
				count++;
				pos += pattern.Length;
			}
			return count;
		}

		private static bool IsValidComponentScript(MonoScript ms)
		{
			if (ms == null) return false;
			var t = ms.GetClass();
			return t != null && typeof(Component).IsAssignableFrom(t) && !t.IsAbstract;
		}

	}
}
