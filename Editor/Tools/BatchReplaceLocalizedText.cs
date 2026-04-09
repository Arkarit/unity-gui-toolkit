using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GuiToolkit.Editor
{
	/// <summary>
	/// Batch-replaces all <see cref="TextMeshProUGUI"/> components with
	/// <see cref="UiLocalizedTextMeshProUGUI"/> and removes all <see cref="UiAutoLocalize"/>
	/// components, either in the currently active scene/prefab or across the entire project.
	/// <para>
	/// When a TextMeshProUGUI lives on a prefab instance, the replacement is performed in the
	/// source prefab that defines the component, not in the scene file.
	/// </para>
	/// <para>
	/// Uses the same YAML-patching strategy as <see cref="ReplaceWithLocalizedText"/>:
	/// the <c>m_Script</c> GUID is swapped in-place, so all serialized TMP settings and every
	/// external reference to the component remain intact.
	/// </para>
	/// </summary>
	internal static class BatchReplaceLocalizedText
	{
		// -----------------------------------------------------------------------
		// Menu items
		// -----------------------------------------------------------------------

		[MenuItem(StringConstants.LOCA_MISC_BATCH_REPLACE_SCENE_MENU_NAME, priority = Constants.LOCA_MISC_BATCH_REPLACE_SCENE_MENU_PRIORITY)]
		private static void ReplaceInCurrentScene() => RunBatch(entireProject: false);

		[MenuItem(StringConstants.LOCA_MISC_BATCH_REPLACE_PROJECT_MENU_NAME, priority = Constants.LOCA_MISC_BATCH_REPLACE_PROJECT_MENU_PRIORITY)]
		private static void ReplaceInProject() => RunBatch(entireProject: true);

		private struct Stats
		{
			public int tmpReplaced;
			public int autoLocalizeRemoved;
			public int filesProcessed;
			public int errors;
		}


		// -----------------------------------------------------------------------
		// Core batch logic
		// -----------------------------------------------------------------------

		private static void RunBatch(bool entireProject)
		{
			// Look up MonoScript GUIDs once — constant per installed package version.
			string oldGuid = YamlUtility.FindMonoScriptGuid(typeof(TextMeshProUGUI));
			string newGuid = YamlUtility.FindMonoScriptGuid(typeof(UiLocalizedTextMeshProUGUI));

			if (string.IsNullOrEmpty(oldGuid))
			{
				EditorUtility.DisplayDialog("Replace All TMP with Localized Text",
					"Could not locate the TextMeshProUGUI MonoScript GUID.\n" +
					"Make sure TextMesh Pro is imported correctly.", "OK");
				return;
			}
			if (string.IsNullOrEmpty(newGuid))
			{
				EditorUtility.DisplayDialog("Replace All TMP with Localized Text",
					"Could not locate the UiLocalizedTextMeshProUGUI MonoScript GUID.\n" +
					"Make sure the UI Toolkit package is imported correctly.", "OK");
				return;
			}

			bool isInPrefabStage = PrefabStageUtility.GetCurrentPrefabStage() != null;
			string currentAssetPath = isInPrefabStage
				? PrefabStageUtility.GetCurrentPrefabStage().assetPath
				: EditorSceneManager.GetActiveScene().path;

			// Collect the set of prefab asset paths to process and any plain scene objects.
			var prefabPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			var scenePlainTmps = new List<TextMeshProUGUI>();
			var scenePlainAutoLocalizes = new List<UiAutoLocalize>();

			if (entireProject)
			{
				// Add every prefab in the project.
				foreach (string guid in AssetDatabase.FindAssets("t:Prefab"))
					prefabPaths.Add(AssetDatabase.GUIDToAssetPath(guid));

				// Also handle plain (non-instance) objects in the currently open scene.
				if (!isInPrefabStage && !string.IsNullOrEmpty(currentAssetPath))
					GatherPlainSceneObjects(scenePlainTmps, scenePlainAutoLocalizes);
			}
			else
			{
				if (isInPrefabStage)
				{
					// The whole currently open prefab is the scope.
					prefabPaths.Add(currentAssetPath);
				}
				else
				{
					// Scan the scene hierarchy: collect prefab source paths + plain scene objects.
					GatherFromScene(prefabPaths, scenePlainTmps, scenePlainAutoLocalizes);
				}
			}

			// Show confirmation.
			bool hasSceneWork = scenePlainTmps.Count > 0 || scenePlainAutoLocalizes.Count > 0;
			string scope = entireProject
				? "the entire project (all prefabs + plain objects in the current scene)"
				: (isInPrefabStage ? "the currently open prefab" : "the current scene");

			string msg = $"Replace all TextMeshProUGUI → UiLocalizedTextMeshProUGUI in {scope}.\n\n" +
			             $"Prefabs to process: {prefabPaths.Count}\n" +
			             $"Plain scene objects: {scenePlainTmps.Count}\n\n" +
			             "All UiAutoLocalize components will also be removed.\n" +
			             "This operation cannot be undone.\n\nContinue?";

			if (!EditorUtility.DisplayDialog("Replace All TMP with Localized Text", msg, "Replace All", "Cancel"))
				return;

			// Save current state before any file operations.
			if (!string.IsNullOrEmpty(currentAssetPath))
				YamlUtility.SaveCurrentSceneOrPrefab();

			var stats = new Stats();

			try
			{
				int total = prefabPaths.Count + (hasSceneWork ? 1 : 0);
				int done = 0;

				foreach (string prefabPath in prefabPaths)
				{
					EditorUtility.DisplayProgressBar(
						"Replace All TMP with Localized Text",
						$"Processing {Path.GetFileName(prefabPath)}…",
						(float)done / Math.Max(total, 1));
					done++;

					try
					{
						ProcessPrefabFile(prefabPath, oldGuid, newGuid, ref stats);
					}
					catch (Exception ex)
					{
						Debug.LogError($"[BatchReplaceLocalizedText] Error processing {prefabPath}: {ex}");
						stats.errors++;
					}
				}

				if (hasSceneWork)
				{
					EditorUtility.DisplayProgressBar(
						"Replace All TMP with Localized Text",
						"Processing plain scene objects…", 1f);
					ProcessScenePlainObjects(currentAssetPath, scenePlainTmps, scenePlainAutoLocalizes,
						oldGuid, newGuid, ref stats);
				}
			}
			finally
			{
				EditorUtility.ClearProgressBar();
			}

			// Reload the currently active scene/prefab to reflect the patched components.
			ReloadCurrentAsset(isInPrefabStage, currentAssetPath);

			// Summary dialog.
			string summary =
				$"Replaced:  {stats.tmpReplaced} TextMeshProUGUI component(s)\n" +
				$"Removed:   {stats.autoLocalizeRemoved} UiAutoLocalize component(s)\n" +
				$"Files:     {stats.filesProcessed} modified";
			if (stats.errors > 0)
				summary += $"\nErrors:    {stats.errors} (see Console for details)";

			EditorUtility.DisplayDialog("Replace All TMP – Done", summary, "OK");
		}

		// -----------------------------------------------------------------------
		// Gathering helpers
		// -----------------------------------------------------------------------

		/// <summary>
		/// Scans the active scene hierarchy and populates:
		/// <list type="bullet">
		///   <item><paramref name="prefabPaths"/> — source prefab paths that must be processed
		///         (because a TextMeshProUGUI or UiAutoLocalize lives on a prefab instance)</item>
		///   <item><paramref name="plainTmps"/> / <paramref name="plainAutoLocalizes"/> — components
		///         that live directly on plain scene objects (not inside prefab instances)</item>
		/// </list>
		/// </summary>
		private static void GatherFromScene(
			HashSet<string> prefabPaths,
			List<TextMeshProUGUI> plainTmps,
			List<UiAutoLocalize> plainAutoLocalizes)
		{
			foreach (var tmp in Object.FindObjectsOfType<TextMeshProUGUI>(true))
			{
				if (tmp is UiLocalizedTextMeshProUGUI) continue;

				if (PrefabUtility.IsPartOfPrefabInstance(tmp.gameObject))
				{
					string path = GetDefiningPrefabPath(tmp);
					if (!string.IsNullOrEmpty(path)) prefabPaths.Add(path);
				}
				else
				{
					plainTmps.Add(tmp);
				}
			}

#pragma warning disable CS0618
			foreach (var al in Object.FindObjectsOfType<UiAutoLocalize>(true))
			{
				if (PrefabUtility.IsPartOfPrefabInstance(al.gameObject))
				{
					string path = GetDefiningPrefabPath(al);
					if (!string.IsNullOrEmpty(path)) prefabPaths.Add(path);
				}
				else
				{
					plainAutoLocalizes.Add(al);
				}
			}
#pragma warning restore CS0618
		}

		/// <summary>
		/// Collects TextMeshProUGUI and UiAutoLocalize components on plain scene objects
		/// (i.e. not part of any prefab instance) in the currently active scene.
		/// </summary>
		private static void GatherPlainSceneObjects(
			List<TextMeshProUGUI> plainTmps,
			List<UiAutoLocalize> plainAutoLocalizes)
		{
			foreach (var tmp in Object.FindObjectsOfType<TextMeshProUGUI>(true))
			{
				if (tmp is UiLocalizedTextMeshProUGUI) continue;
				if (!PrefabUtility.IsPartOfPrefabInstance(tmp.gameObject))
					plainTmps.Add(tmp);
			}

#pragma warning disable CS0618
			foreach (var al in Object.FindObjectsOfType<UiAutoLocalize>(true))
			{
				if (!PrefabUtility.IsPartOfPrefabInstance(al.gameObject))
					plainAutoLocalizes.Add(al);
			}
#pragma warning restore CS0618
		}

		// -----------------------------------------------------------------------
		// Processing
		// -----------------------------------------------------------------------

		/// <summary>
		/// Processes a single prefab file: removes UiAutoLocalize components, then
		/// YAML-patches TextMeshProUGUI → UiLocalizedTextMeshProUGUI for all non-nested components.
		/// Skips components that belong to nested prefab instances inside this prefab
		/// (those will be handled when their own source prefab is processed).
		/// </summary>
		private static void ProcessPrefabFile(string assetPath, string oldGuid, string newGuid, ref Stats stats)
		{
			var prefabContents = PrefabUtility.LoadPrefabContents(assetPath);
			var patchIds = new List<long>();
			bool hasChanges = false;

			try
			{
				// Collect TextMeshProUGUI components defined in this prefab (not nested instances).
				foreach (var tmp in prefabContents.GetComponentsInChildren<TextMeshProUGUI>(true))
				{
					if (tmp is UiLocalizedTextMeshProUGUI) continue;
					if (PrefabUtility.IsPartOfPrefabInstance(tmp.gameObject)) continue;

					if (YamlUtility.TryGetLocalFileId(tmp, out long localId))
					{
						patchIds.Add(localId);
						stats.tmpReplaced++;
						hasChanges = true;
					}
					else
					{
						Debug.LogWarning($"[BatchReplaceLocalizedText] Could not get local file ID for " +
						                 $"'{tmp.gameObject.name}' in {assetPath} — skipped.");
					}
				}

				// Remove UiAutoLocalize components defined in this prefab (not nested instances).
#pragma warning disable CS0618
				foreach (var al in prefabContents.GetComponentsInChildren<UiAutoLocalize>(true))
				{
					if (PrefabUtility.IsPartOfPrefabInstance(al.gameObject)) continue;
					UnityEngine.Object.DestroyImmediate(al);
					stats.autoLocalizeRemoved++;
					hasChanges = true;
				}
#pragma warning restore CS0618

				if (hasChanges)
				{
					PrefabUtility.SaveAsPrefabAsset(prefabContents, assetPath, out bool saved);
					if (saved)
						stats.filesProcessed++;
					else
						Debug.LogWarning($"[BatchReplaceLocalizedText] SaveAsPrefabAsset returned false for {assetPath}.");
				}
			}
			finally
			{
				PrefabUtility.UnloadPrefabContents(prefabContents);
			}

			// YAML-patch after save so the TMP components are still present in the file.
			if (patchIds.Count > 0)
				ApplyYamlPatches(assetPath, patchIds, oldGuid, newGuid);
		}

		/// <summary>
		/// Processes plain (non-prefab-instance) scene objects: removes UiAutoLocalize,
		/// saves the scene, then YAML-patches TextMeshProUGUI → UiLocalizedTextMeshProUGUI.
		/// </summary>
		private static void ProcessScenePlainObjects(
			string scenePath,
			List<TextMeshProUGUI> tmps,
			List<UiAutoLocalize> autoLocalizes,
			string oldGuid, string newGuid,
			ref Stats stats)
		{
			var patchIds = new List<long>();

			foreach (var tmp in tmps)
			{
				if (YamlUtility.TryGetLocalFileId(tmp, out long localId))
				{
					patchIds.Add(localId);
					stats.tmpReplaced++;
				}
				else
				{
					Debug.LogWarning($"[BatchReplaceLocalizedText] Could not get local file ID for " +
					                 $"'{tmp.gameObject.name}' in scene — skipped.");
				}
			}

#pragma warning disable CS0618
			foreach (var al in autoLocalizes)
			{
				UnityEngine.Object.DestroyImmediate(al);
				stats.autoLocalizeRemoved++;
			}
#pragma warning restore CS0618

			EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
			stats.filesProcessed++;

			if (patchIds.Count > 0)
				ApplyYamlPatches(scenePath, patchIds, oldGuid, newGuid);
		}

		// -----------------------------------------------------------------------
		// Utilities
		// -----------------------------------------------------------------------

		private static void ApplyYamlPatches(string assetPath, IList<long> localIds,
		                                      string oldGuid, string newGuid)
		{
			string fullPath = YamlUtility.AssetPathToFullPath(assetPath);
			if (!File.Exists(fullPath))
			{
				Debug.LogWarning($"[BatchReplaceLocalizedText] File not found: {fullPath}");
				return;
			}

			string yaml = File.ReadAllText(fullPath);
			bool changed = false;

			foreach (long localId in localIds)
			{
				string patched = YamlUtility.PatchYaml(yaml, localId, oldGuid, newGuid);
				if (patched != null)
				{
					yaml = patched;
					changed = true;
				}
				else
				{
					Debug.LogWarning($"[BatchReplaceLocalizedText] YAML patch failed for localId={localId} in {assetPath}.");
				}
			}

			if (changed)
			{
				File.WriteAllText(fullPath, yaml);
				AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
			}
		}

		private static void ReloadCurrentAsset(bool isInPrefabStage, string assetPath)
		{
			if (string.IsNullOrEmpty(assetPath)) return;

			if (isInPrefabStage)
			{
				StageUtility.GoBackToPreviousStage();
				EditorApplication.delayCall += () =>
					AssetDatabase.OpenAsset(AssetDatabase.LoadAssetAtPath<GameObject>(assetPath));
			}
			else
			{
				EditorSceneManager.OpenScene(assetPath, OpenSceneMode.Single);
			}
		}

		/// <summary>
		/// Returns the asset path of the source prefab that actually defines <paramref name="c"/>
		/// (walking the full nesting chain via <see cref="PrefabUtility.GetCorrespondingObjectFromOriginalSource"/>).
		/// Returns <c>null</c> if the component is not part of any prefab instance.
		/// </summary>
		private static string GetDefiningPrefabPath(Component c)
		{
			var original = PrefabUtility.GetCorrespondingObjectFromOriginalSource(c);
			if (original == null) return null;

			string path = AssetDatabase.GetAssetPath(original);
			if (string.IsNullOrEmpty(path))
			{
				Debug.LogWarning($"[BatchReplaceLocalizedText] Could not resolve prefab path for " +
				                 $"'{c.gameObject.name}' (component: {c.GetType().Name}).");
			}
			return string.IsNullOrEmpty(path) ? null : path;
		}
	}
}
