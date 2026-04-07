using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace GuiToolkit.Editor
{
	/// <summary>
	/// EditorWindow that batch-converts UI text components to <see cref="UiLocalizedTextMeshProUGUI"/>
	/// with configurable include/exclude paths and a dry-run mode.
	/// <para>
	/// Supported actions:
	/// <list type="bullet">
	///   <item>Legacy <c>Text</c> → <see cref="UiLocalizedTextMeshProUGUI"/> (delegates to
	///         <see cref="LegacyTextToLocalizedTmpConverter"/>)</item>
	///   <item><c>TextMeshProUGUI</c> → <see cref="UiLocalizedTextMeshProUGUI"/> (YAML GUID-swap +
	///         field injection)</item>
	/// </list>
	/// </para>
	/// </summary>
	public class LocalizedTmpConverterWindow : EditorWindow
	{
		private enum ConversionAction
		{
			TextToUiLocalizedTmp,
			TmpToUiLocalizedTmp,
		}

		[SerializeField] private ConversionAction m_action;

		[PathField(true)]
		[SerializeField] private List<PathField> m_includePaths = new();

		[PathField(true)]
		[SerializeField] private List<PathField> m_excludePaths = new();

		// Survives domain reloads via EditorWindow serialization.
		[SerializeField] private List<string> m_cachedFiles   = new();
		[SerializeField] private string       m_cachedPathsKey = "";

		private SerializedObject   m_so;
		private SerializedProperty m_includeProp;
		private SerializedProperty m_excludeProp;
		private Vector2            m_scroll;

		[MenuItem(StringConstants.LOCA_MISC_CONVERTER_WINDOW_MENU_NAME, false, Constants.LOCA_MISC_CONVERTER_WINDOW_MENU_PRIORITY)]
		public static void Open()
		{
			var wnd = GetWindow<LocalizedTmpConverterWindow>("Localized TMP Converter");
			wnd.minSize = new Vector2(420, 300);
			wnd.Show();
		}

		private void OnEnable()
		{
			m_so          = new SerializedObject(this);
			m_includeProp = m_so.FindProperty("m_includePaths");
			m_excludeProp = m_so.FindProperty("m_excludePaths");
		}

		private void OnGUI()
		{
			if (m_so == null)
				OnEnable();

			m_so.Update();
			m_scroll = EditorGUILayout.BeginScrollView(m_scroll);

			EditorGUILayout.LabelField("Conversion Action", EditorStyles.boldLabel);
			m_action = (ConversionAction)EditorGUILayout.EnumPopup("Action", m_action);

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Include Paths", EditorStyles.boldLabel);
			EditorGUILayout.HelpBox("Leave empty to search all of Assets/.", MessageType.None);
			DrawPathList(m_includeProp);

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Exclude Paths", EditorStyles.boldLabel);
			DrawPathList(m_excludeProp);

			m_so.ApplyModifiedProperties();

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("File Cache", EditorStyles.boldLabel);
			bool   cacheStale   = m_cachedFiles.Count > 0 && m_cachedPathsKey != ComputePathsKey();
			string cacheStatus  = m_cachedFiles.Count == 0
				? "Not built"
				: cacheStale
					? $"{m_cachedFiles.Count} file(s) cached  [paths changed — will rebuild]"
					: $"{m_cachedFiles.Count} prefab(s) cached";
			EditorGUILayout.LabelField(cacheStatus, EditorStyles.miniLabel);

			using (new GUILayout.HorizontalScope())
			{
				if (GUILayout.Button("Build Cache"))
					BuildCache();

				using (new EditorGUI.DisabledScope(m_cachedFiles.Count == 0))
				{
					if (GUILayout.Button("Clear Cache"))
						ClearCache();
				}
			}

			EditorGUILayout.Space();
			using (new GUILayout.HorizontalScope())
			{
				if (GUILayout.Button("Dry Run"))
					RunDryRun();

				if (GUILayout.Button("Execute"))
					RunExecute();
			}

			EditorGUILayout.EndScrollView();
		}

		// -----------------------------------------------------------------------
		// Path list drawing
		// -----------------------------------------------------------------------

		private void DrawPathList(SerializedProperty listProp)
		{
			int toRemove = -1;

			for (int i = 0; i < listProp.arraySize; i++)
			{
				using (new GUILayout.HorizontalScope())
				{
					EditorGUILayout.PropertyField(listProp.GetArrayElementAtIndex(i), GUIContent.none);

					if (GUILayout.Button("×", GUILayout.Width(22)))
						toRemove = i;
				}
			}

			if (toRemove >= 0)
				listProp.DeleteArrayElementAtIndex(toRemove);

			if (GUILayout.Button("+", GUILayout.Width(30)))
				listProp.InsertArrayElementAtIndex(listProp.arraySize);
		}

		// -----------------------------------------------------------------------
		// Actions
		// -----------------------------------------------------------------------

		private void RunDryRun()
		{
			var files = GetFiles();
			var log   = new List<string>();
			log.Add($"=== Dry Run — {ActionLabel()} ===");
			log.Add($"Files: {files.Count}");

			if (files.Count == 0)
			{
				WriteLog(log);
				return;
			}

			if (m_action == ConversionAction.TextToUiLocalizedTmp)
				LegacyTextToLocalizedTmpConverter.ScanForTextComponents(files, log);
			else
				ScanForTmpComponents(files, log);

			WriteLog(log);
		}

		private void RunExecute()
		{
			var files = GetFiles();

			if (files.Count == 0)
			{
				Debug.Log("[LocalizedTmpConverterWindow] No matching prefabs found.");
				return;
			}

			if (!EditorUtility.DisplayDialog(
				    "Convert",
				    $"Action: {ActionLabel()}\nPrefabs: {files.Count}\n\nThis cannot be undone. Continue?",
				    "Convert", "Cancel"))
				return;

			var log = new List<string>();
			log.Add($"=== Execute — {ActionLabel()} ===");
			log.Add($"Files: {files.Count}");

			if (m_action == ConversionAction.TextToUiLocalizedTmp)
				LegacyTextToLocalizedTmpConverter.ConvertFiles(files, log);
			else
				ConvertTmpFiles(files, log);

			WriteLog(log);
		}

		private string ActionLabel() => m_action switch
		{
			ConversionAction.TextToUiLocalizedTmp => "Text → UiLocalizedTextMeshProUGUI",
			ConversionAction.TmpToUiLocalizedTmp  => "TextMeshProUGUI → UiLocalizedTextMeshProUGUI",
			_                                     => m_action.ToString(),
		};

		// -----------------------------------------------------------------------
		// File cache
		// -----------------------------------------------------------------------

		private IReadOnlyList<string> GetFiles()
		{
			string currentKey = ComputePathsKey();
			if (m_cachedFiles.Count == 0 || m_cachedPathsKey != currentKey)
				BuildCache();
			return m_cachedFiles;
		}

		private void BuildCache()
		{
			var files = CollectFiles();
			m_cachedFiles.Clear();
			m_cachedFiles.AddRange(files);
			m_cachedPathsKey = ComputePathsKey();
			EditorUtility.SetDirty(this);
			Repaint();
			Debug.Log($"[LocalizedTmpConverterWindow] Cache built: {m_cachedFiles.Count} file(s).");
		}

		private void ClearCache()
		{
			m_cachedFiles.Clear();
			m_cachedPathsKey = "";
			EditorUtility.SetDirty(this);
			Repaint();
		}

		private string ComputePathsKey()
		{
			var sb = new System.Text.StringBuilder();
			foreach (var p in m_includePaths)
				sb.Append(p.Path ?? "").Append('|');
			sb.Append("||");
			foreach (var p in m_excludePaths)
				sb.Append(p.Path ?? "").Append('|');
			return sb.ToString();
		}

		// -----------------------------------------------------------------------
		// File collection
		// -----------------------------------------------------------------------

		private IReadOnlyList<string> CollectFiles()
		{
			// Build include-folder list.
			var includeFolders = new List<string>();
			foreach (var p in m_includePaths)
			{
				string ap = ToAssetPath(p);
				if (!string.IsNullOrEmpty(ap))
					includeFolders.Add(ap);
			}
			if (includeFolders.Count == 0)
				includeFolders.Add("Assets");

			// Build exclude set.
			var excludePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (var p in m_excludePaths)
			{
				string ap = ToAssetPath(p);
				if (!string.IsNullOrEmpty(ap))
					excludePaths.Add(ap.TrimEnd('/'));
			}

			var result = new List<string>();

			// Prefabs
			foreach (string guid in AssetDatabase.FindAssets("t:Prefab", includeFolders.ToArray()))
			{
				string path = AssetDatabase.GUIDToAssetPath(guid);
				if (string.IsNullOrEmpty(path)) continue;
				if (IsExcluded(path, excludePaths)) continue;
				result.Add(path);
			}

			// Scenes (TMP -> UiLocalizedTMP supports scene objects)
			foreach (string guid in AssetDatabase.FindAssets("t:Scene", includeFolders.ToArray()))
			{
				string path = AssetDatabase.GUIDToAssetPath(guid);
				if (string.IsNullOrEmpty(path)) continue;
				if (!path.EndsWith(".unity", StringComparison.OrdinalIgnoreCase)) continue;
				if (IsExcluded(path, excludePaths)) continue;
				result.Add(path);
			}

			return result;
		}

		private static bool IsExcluded(string assetPath, HashSet<string> excludePaths)
		{
			foreach (string excl in excludePaths)
			{
				if (assetPath.StartsWith(excl, StringComparison.OrdinalIgnoreCase))
					return true;
			}
			return false;
		}

		private static string ToAssetPath(PathField p)
		{
			string raw = p.Path?.Replace('\\', '/');
			if (string.IsNullOrWhiteSpace(raw))
				return null;

			// Convert absolute path to Unity asset path (relative to project root).
			string projectRoot = Application.dataPath.Replace("/Assets", "");
			if (!projectRoot.EndsWith("/"))
				projectRoot += "/";

			string result = raw.TrimEnd('/');
			if (result.StartsWith(projectRoot, StringComparison.OrdinalIgnoreCase))
				result = result.Substring(projectRoot.Length);

			// Must start with "Assets" to be a valid Unity path.
			if (!result.StartsWith("Assets", StringComparison.OrdinalIgnoreCase))
				return null;

			return result;
		}

		// -----------------------------------------------------------------------
		// TMP → UiLocalizedTMP dry-run scan
		// -----------------------------------------------------------------------

		private static void ScanForTmpComponents(IReadOnlyList<string> files, List<string> log)
		{
			int totalFiles      = 0;
			int totalComponents = 0;

			for (int i = 0; i < files.Count; i++)
			{
				string path = files[i];
				EditorUtility.DisplayProgressBar(
					"Scanning for TextMeshProUGUI",
					$"Scanning {Path.GetFileName(path)}\u2026",
					(float)i / Math.Max(files.Count, 1));

				try
				{
					int count = IsScenePath(path)
						? ScanSceneForTmpCount(path)
						: ScanPrefabForTmpCount(path);
					if (count > 0)
					{
						log?.Add($"  {path} ({count} TextMeshProUGUI)");
						totalFiles++;
						totalComponents += count;
					}
				}
				catch (Exception ex)
				{
					log?.Add($"  ERROR: {path}: {ex.Message}");
				}
			}

			EditorUtility.ClearProgressBar();
			log?.Add($"Dry Run: {totalFiles} file(s) contain {totalComponents} TextMeshProUGUI component(s)");
		}

		// -----------------------------------------------------------------------
		// TMP → UiLocalizedTMP conversion
		// -----------------------------------------------------------------------

		private static void ConvertTmpFiles(IReadOnlyList<string> files, List<string> log)
		{
			string oldGuid = YamlUtility.FindMonoScriptGuid(typeof(TextMeshProUGUI));
			string newGuid = YamlUtility.FindMonoScriptGuid(typeof(UiLocalizedTextMeshProUGUI));

			if (string.IsNullOrEmpty(oldGuid) || string.IsNullOrEmpty(newGuid))
			{
				log?.Add("ERROR: Could not resolve TextMeshProUGUI or UiLocalizedTextMeshProUGUI GUID.");
				return;
			}

			// Build cross-file reference index once for the whole batch so we can correctly
			// determine whether a TMP component has its .text set from another file's script.
			var crossFileIndex = LegacyTextToLocalizedTmpConverter.BuildProjectWideCrossFileIndex();

			int fileCount      = 0;
			int componentCount = 0;
			int errors         = 0;

			// Process prefabs in a batch (fast import deferral).
			AssetDatabase.StartAssetEditing();
			try
			{
				for (int i = 0; i < files.Count; i++)
				{
					string path = files[i];
					if (IsScenePath(path)) continue;

					EditorUtility.DisplayProgressBar(
						"Convert TextMeshProUGUI \u2192 UiLocalizedTextMeshProUGUI",
						$"Processing {Path.GetFileName(path)}\u2026",
						(float)i / Math.Max(files.Count, 1));

					try
					{
						int converted = ProcessTmpPrefab(path, oldGuid, newGuid, crossFileIndex);
						if (converted > 0)
						{
							log?.Add($"  {path} ({converted} converted)");
							fileCount++;
							componentCount += converted;
						}
					}
					catch (Exception ex)
					{
						Debug.LogError($"[LocalizedTmpConverterWindow] Error in {path}: {ex}");
						log?.Add($"  ERROR: {path}: {ex.Message}");
						errors++;
					}
				}
			}
			finally
			{
				EditorUtility.ClearProgressBar();
				AssetDatabase.StopAssetEditing();
			}

			// Process scenes outside the batch (scene open/save must not overlap with StartAssetEditing).
			for (int i = 0; i < files.Count; i++)
			{
				string path = files[i];
				if (!IsScenePath(path)) continue;

				EditorUtility.DisplayProgressBar(
					"Convert TextMeshProUGUI \u2192 UiLocalizedTextMeshProUGUI",
					$"Processing scene {Path.GetFileName(path)}\u2026",
					(float)i / Math.Max(files.Count, 1));

				try
				{
					int converted = ProcessTmpScene(path, oldGuid, newGuid, crossFileIndex);
					if (converted > 0)
					{
						log?.Add($"  {path} ({converted} converted)");
						fileCount++;
						componentCount += converted;
					}
				}
				catch (Exception ex)
				{
					Debug.LogError($"[LocalizedTmpConverterWindow] Error in {path}: {ex}");
					log?.Add($"  ERROR: {path}: {ex.Message}");
					errors++;
				}
			}

			EditorUtility.ClearProgressBar();
			log?.Add($"Converted: {componentCount} component(s) in {fileCount} file(s)"
			       + (errors > 0 ? $", {errors} error(s) — see Console" : ""));
		}

		/// <summary>
		/// Loads the prefab at <paramref name="assetPath"/>, collects all non-nested
		/// <see cref="TextMeshProUGUI"/> component local IDs, saves the prefab (so YAML is current),
		/// then YAML-patches the <c>m_Script</c> GUID and injects the three extra fields that
		/// <see cref="UiLocalizedTextMeshProUGUI"/> declares.  The <c>m_autoLocalize</c> value is
		/// determined per-component: it is disabled when any C# script sets <c>.text</c> directly
		/// on the component, or when the current text value looks like a runtime-generated value.
		/// </summary>
		private static int ProcessTmpPrefab(string assetPath, string oldGuid, string newGuid,
		                                     Dictionary<(string, long), Dictionary<string, HashSet<string>>> crossFileIndex)
		{
			// localId → text content at time of conversion (needed for IsObviouslyRuntimeValue check).
			var patchIds   = new List<long>();
			var textValues = new Dictionary<long, string>();

			var prefabContents = PrefabUtility.LoadPrefabContents(assetPath);

			try
			{
				bool needsSave = false;

				foreach (var tmp in prefabContents.GetComponentsInChildren<TextMeshProUGUI>(true))
				{
					if (tmp is UiLocalizedTextMeshProUGUI) continue;
					if (PrefabUtility.IsPartOfPrefabInstance(tmp.gameObject)) continue;

					if (YamlUtility.TryGetLocalFileId(tmp, out long localId))
					{
						patchIds.Add(localId);
						textValues[localId] = tmp.text ?? "";
						needsSave = true;
					}
					else
					{
						Debug.LogWarning($"[LocalizedTmpConverterWindow] Could not get local file ID for " +
						                 $"'{tmp.gameObject.name}' in {assetPath} — skipped.");
					}
				}

				if (needsSave)
					PrefabUtility.SaveAsPrefabAsset(prefabContents, assetPath);
			}
			finally
			{
				PrefabUtility.UnloadPrefabContents(prefabContents);
			}

			if (patchIds.Count == 0)
				return 0;

			// YAML-patch after save so the TMP components are still present in the file.
			string fullPath = YamlUtility.AssetPathToFullPath(assetPath);
			if (!File.Exists(fullPath))
			{
				Debug.LogWarning($"[LocalizedTmpConverterWindow] File not found after save: {fullPath}");
				return 0;
			}

			string yaml    = File.ReadAllText(fullPath);

			// Build the same-file referencing-script map, then merge cross-file refs.
			var convertedIds    = new HashSet<long>(patchIds);
			var perComponentMap = LegacyTextToLocalizedTmpConverter.FindReferencingScriptFieldsPerComponent(yaml, convertedIds);
			LegacyTextToLocalizedTmpConverter.MergeCrossFileRefs(assetPath, convertedIds, crossFileIndex, perComponentMap);

			bool changed = false;

			foreach (long localId in patchIds)
			{
				bool hasSetter     = LegacyTextToLocalizedTmpConverter.CheckForTextPropertySetters(localId, perComponentMap);
				bool obviousRuntime = LegacyTextToLocalizedTmpConverter.IsObviouslyRuntimeValue(
					textValues.TryGetValue(localId, out string tv) ? tv : "");
				bool autoLocalize  = !hasSetter && !obviousRuntime;

				string patched = PatchYamlAndInjectFields(yaml, localId, oldGuid, newGuid, autoLocalize);
				if (patched != null)
				{
					yaml    = patched;
					changed = true;
				}
				else
				{
					Debug.LogWarning($"[LocalizedTmpConverterWindow] YAML patch failed for id={localId} in {assetPath}.");
				}
			}

			if (changed)
			{
				File.WriteAllText(fullPath, yaml);
				AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
			}

			return patchIds.Count;
		}

		/// <summary>
		/// Swaps the <c>m_Script</c> GUID in the MonoBehaviour block identified by
		/// <paramref name="localFileId"/> and appends the three fields that
		/// <see cref="UiLocalizedTextMeshProUGUI"/> adds on top of <see cref="TextMeshProUGUI"/>:
		/// <c>m_autoLocalize</c>, <c>m_group</c>, and <c>m_locaKey</c>.
		/// Returns <c>null</c> if the block or the old GUID was not found.
		/// </summary>
		private static string PatchYamlAndInjectFields(string yaml, long localFileId,
		                                               string oldGuid, string newGuid, bool autoLocalize)
		{
			const int monoBehaviourClassId = 114;
			string blockMarker = $"--- !u!{monoBehaviourClassId} &{localFileId}";

			int blockStart = yaml.IndexOf(blockMarker, StringComparison.Ordinal);
			if (blockStart < 0)
				return null;

			int searchFrom = blockStart + blockMarker.Length;
			int nextBlock  = yaml.IndexOf("\n---", searchFrom, StringComparison.Ordinal);
			int blockEnd   = nextBlock < 0 ? yaml.Length : nextBlock;

			string block = yaml.Substring(blockStart, blockEnd - blockStart);

			// Swap only the m_Script GUID (include ", type:" to avoid false matches).
			string oldToken = $"guid: {oldGuid}, type:";
			string newToken = $"guid: {newGuid}, type:";
			if (!block.Contains(oldToken))
				return null;

			block = block.Replace(oldToken, newToken);

			// Append the three UiLocalizedTextMeshProUGUI-specific fields at the end of the block.
			int autoLocalizeValue = autoLocalize ? 1 : 0;
			block = block.TrimEnd('\n') + $"\n  m_autoLocalize: {autoLocalizeValue}\n  m_group: \n  m_locaKey: \n";

			return yaml.Substring(0, blockStart) + block + yaml.Substring(blockEnd);
		}

		// -----------------------------------------------------------------------
		// Log output
		// -----------------------------------------------------------------------

		private static void WriteLog(IList<string> log)
		{
			foreach (string line in log)
				Debug.Log($"[LocalizedTmpConverterWindow] {line}");

			string outputPath = Path.Combine(Application.dataPath, "../Temp/conversion.txt");
			File.WriteAllLines(outputPath, log);
			Debug.Log($"[LocalizedTmpConverterWindow] Log written to: {outputPath}");
		}

		// -----------------------------------------------------------------------
		// Scene helpers
		// -----------------------------------------------------------------------

		private static bool IsScenePath(string path)
			=> path.EndsWith(".unity", StringComparison.OrdinalIgnoreCase);

		/// <summary>Opens a scene additively if not already loaded. Returns the scene and whether it was newly opened.</summary>
		private static (Scene scene, bool opened) OpenSceneForProcessing(string assetPath)
		{
			var existing = EditorSceneManager.GetSceneByPath(assetPath);
			if (existing.IsValid() && existing.isLoaded)
				return (existing, false);
			return (EditorSceneManager.OpenScene(assetPath, OpenSceneMode.Additive), true);
		}

		private static int ScanPrefabForTmpCount(string path)
		{
			var go = PrefabUtility.LoadPrefabContents(path);
			try
			{
				int count = 0;
				foreach (var tmp in go.GetComponentsInChildren<TextMeshProUGUI>(true))
				{
					if (tmp is UiLocalizedTextMeshProUGUI) continue;
					if (!PrefabUtility.IsPartOfPrefabInstance(tmp.gameObject))
						count++;
				}
				return count;
			}
			finally
			{
				PrefabUtility.UnloadPrefabContents(go);
			}
		}

		private static int ScanSceneForTmpCount(string assetPath)
		{
			var (scene, opened) = OpenSceneForProcessing(assetPath);
			try
			{
				if (!scene.IsValid() || !scene.isLoaded) return 0;
				int count = 0;
				foreach (var root in scene.GetRootGameObjects())
				{
					foreach (var tmp in root.GetComponentsInChildren<TextMeshProUGUI>(true))
					{
						if (tmp is UiLocalizedTextMeshProUGUI) continue;
						if (!PrefabUtility.IsPartOfPrefabInstance(tmp.gameObject))
							count++;
					}
				}
				return count;
			}
			finally
			{
				if (opened && scene.IsValid())
					EditorSceneManager.CloseScene(scene, true);
			}
		}

		/// <summary>
		/// Scene equivalent of <see cref="ProcessTmpPrefab"/>: opens the scene additively,
		/// collects non-nested TextMeshProUGUI components, saves the scene, then YAML-patches
		/// the m_Script GUID and injects the UiLocalizedTextMeshProUGUI fields.
		/// </summary>
		private static int ProcessTmpScene(string assetPath, string oldGuid, string newGuid,
		                                    Dictionary<(string, long), Dictionary<string, HashSet<string>>> crossFileIndex)
		{
			var patchIds   = new List<long>();
			var textValues = new Dictionary<long, string>();

			var (scene, opened) = OpenSceneForProcessing(assetPath);
			try
			{
				if (!scene.IsValid() || !scene.isLoaded) return 0;

				foreach (var root in scene.GetRootGameObjects())
				{
					foreach (var tmp in root.GetComponentsInChildren<TextMeshProUGUI>(true))
					{
						if (tmp is UiLocalizedTextMeshProUGUI) continue;
						if (PrefabUtility.IsPartOfPrefabInstance(tmp.gameObject)) continue;

						if (YamlUtility.TryGetLocalFileId(tmp, out long localId))
						{
							patchIds.Add(localId);
							textValues[localId] = tmp.text ?? "";
						}
						else
						{
							Debug.LogWarning($"[LocalizedTmpConverterWindow] Could not get local file ID for " +
							                 $"'{tmp.gameObject.name}' in {assetPath} — skipped.");
						}
					}
				}

				if (patchIds.Count > 0)
					EditorSceneManager.SaveScene(scene);
			}
			finally
			{
				if (opened && scene.IsValid())
					EditorSceneManager.CloseScene(scene, true);
			}

			if (patchIds.Count == 0)
				return 0;

			string fullPath = YamlUtility.AssetPathToFullPath(assetPath);
			if (!File.Exists(fullPath))
			{
				Debug.LogWarning($"[LocalizedTmpConverterWindow] File not found after save: {fullPath}");
				return 0;
			}

			string yaml = File.ReadAllText(fullPath);
			var convertedIds    = new HashSet<long>(patchIds);
			var perComponentMap = LegacyTextToLocalizedTmpConverter.FindReferencingScriptFieldsPerComponent(yaml, convertedIds);
			LegacyTextToLocalizedTmpConverter.MergeCrossFileRefs(assetPath, convertedIds, crossFileIndex, perComponentMap);

			bool changed = false;
			foreach (long localId in patchIds)
			{
				bool hasSetter      = LegacyTextToLocalizedTmpConverter.CheckForTextPropertySetters(localId, perComponentMap);
				bool obviousRuntime = LegacyTextToLocalizedTmpConverter.IsObviouslyRuntimeValue(
					textValues.TryGetValue(localId, out string tv) ? tv : "");
				bool autoLocalize   = !hasSetter && !obviousRuntime;

				string patched = PatchYamlAndInjectFields(yaml, localId, oldGuid, newGuid, autoLocalize);
				if (patched != null)
				{
					yaml    = patched;
					changed = true;
				}
				else
				{
					Debug.LogWarning($"[LocalizedTmpConverterWindow] YAML patch failed for id={localId} in {assetPath}.");
				}
			}

			if (changed)
			{
				File.WriteAllText(fullPath, yaml);
				AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
			}

			return patchIds.Count;
		}

	}
}