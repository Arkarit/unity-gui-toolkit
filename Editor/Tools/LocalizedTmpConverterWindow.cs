using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
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
		[SerializeField] private List<string> m_cachedFiles = new();

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
			string cacheStatus = m_cachedFiles.Count == 0
				? "Not built"
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
			log.Add($"Prefabs: {files.Count}");

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
			log.Add($"Prefabs: {files.Count}");

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
			if (m_cachedFiles.Count == 0)
				BuildCache();
			return m_cachedFiles;
		}

		private void BuildCache()
		{
			var files = CollectFiles();
			m_cachedFiles.Clear();
			m_cachedFiles.AddRange(files);
			EditorUtility.SetDirty(this);
			Repaint();
			Debug.Log($"[LocalizedTmpConverterWindow] Cache built: {m_cachedFiles.Count} prefab(s).");
		}

		private void ClearCache()
		{
			m_cachedFiles.Clear();
			EditorUtility.SetDirty(this);
			Repaint();
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

			// Prefabs only — scenes have complex open/close requirements that the
			// existing menu-item tools handle.
			var guids  = AssetDatabase.FindAssets("t:Prefab", includeFolders.ToArray());
			var result = new List<string>(guids.Length);

			foreach (string guid in guids)
			{
				string path = AssetDatabase.GUIDToAssetPath(guid);
				if (string.IsNullOrEmpty(path)) continue;
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
			if (!p.IsFolder) return null;

			string fullPath = p.FullPath?.Replace('\\', '/');
			if (string.IsNullOrEmpty(fullPath)) return null;

			// Convert absolute path to Unity asset path (relative to project root).
			string projectRoot = Application.dataPath.Replace("/Assets", "");
			if (!projectRoot.EndsWith("/"))
				projectRoot += "/";

			if (fullPath.StartsWith(projectRoot, StringComparison.OrdinalIgnoreCase))
				fullPath = fullPath.Substring(projectRoot.Length);

			return fullPath.TrimEnd('/');
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

						if (count > 0)
						{
							log?.Add($"  {path} ({count} TextMeshProUGUI)");
							totalFiles++;
							totalComponents += count;
						}
					}
					finally
					{
						PrefabUtility.UnloadPrefabContents(go);
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

			int fileCount      = 0;
			int componentCount = 0;
			int errors         = 0;

			AssetDatabase.StartAssetEditing();
			try
			{
				for (int i = 0; i < files.Count; i++)
				{
					string path = files[i];
					EditorUtility.DisplayProgressBar(
						"Convert TextMeshProUGUI \u2192 UiLocalizedTextMeshProUGUI",
						$"Processing {Path.GetFileName(path)}\u2026",
						(float)i / Math.Max(files.Count, 1));

					try
					{
						int converted = ProcessTmpPrefab(path, oldGuid, newGuid);
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

			log?.Add($"Converted: {componentCount} component(s) in {fileCount} file(s)"
			       + (errors > 0 ? $", {errors} error(s) — see Console" : ""));
		}

		/// <summary>
		/// Loads the prefab at <paramref name="assetPath"/>, collects all non-nested
		/// <see cref="TextMeshProUGUI"/> component local IDs, saves the prefab (so YAML is current),
		/// then YAML-patches the <c>m_Script</c> GUID and injects the three extra fields that
		/// <see cref="UiLocalizedTextMeshProUGUI"/> declares.
		/// </summary>
		private static int ProcessTmpPrefab(string assetPath, string oldGuid, string newGuid)
		{
			var patchIds       = new List<long>();
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
			bool   changed = false;

			foreach (long localId in patchIds)
			{
				string patched = PatchYamlAndInjectFields(yaml, localId, oldGuid, newGuid);
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
		                                               string oldGuid, string newGuid)
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
			block = block.TrimEnd('\n') + "\n  m_autoLocalize: 1\n  m_group: \n  m_locaKey: \n";

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
	}
}
