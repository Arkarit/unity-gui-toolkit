using System;
using System.Collections.Generic;
using System.IO;
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
		[Serializable]
		private struct FieldMapping
		{
			public string SourceField;
			public string TargetField;
		}

		private MonoScript m_SourceScript;
		private MonoScript m_TargetScript;
		private bool m_EntireProject = false;
		[SerializeField] private List<FieldMapping> m_FieldMappings = new List<FieldMapping>();
		private Vector2 m_MappingScroll;

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
			DrawFieldMappings();

			EditorGUILayout.Space();

			using (new EditorGUI.DisabledScope(!IsValidComponentScript(m_SourceScript) ||
			                                   !IsValidComponentScript(m_TargetScript)))
			{
				if (GUILayout.Button("Replace Now"))
					DoReplace();
			}
		}

		// -----------------------------------------------------------------------

		private void DrawFieldMappings()
		{
			EditorGUILayout.LabelField("Field Mappings  (Source → Target)", EditorStyles.boldLabel);

			// Column headers
			using (new EditorGUILayout.HorizontalScope())
			{
				EditorGUILayout.LabelField("Source Field", EditorStyles.miniLabel, GUILayout.MinWidth(80));
				EditorGUILayout.LabelField("Target Field", EditorStyles.miniLabel, GUILayout.MinWidth(80));
				GUILayout.Space(26); // reserved for remove button
			}

			// Scrollable rows
			int removeIndex = -1;
			float rowH     = EditorGUIUtility.singleLineHeight + 2;
			float listH    = m_FieldMappings.Count == 0 ? rowH : rowH * m_FieldMappings.Count;
			m_MappingScroll = EditorGUILayout.BeginScrollView(
				m_MappingScroll, GUILayout.Height(Math.Min(listH, rowH * 5)));

			for (int i = 0; i < m_FieldMappings.Count; i++)
			{
				var mapping = m_FieldMappings[i];
				using (new EditorGUILayout.HorizontalScope())
				{
					mapping.SourceField = EditorGUILayout.TextField(mapping.SourceField ?? "", GUILayout.MinWidth(80));
					EditorGUILayout.LabelField("→", GUILayout.Width(18));
					mapping.TargetField = EditorGUILayout.TextField(mapping.TargetField ?? "", GUILayout.MinWidth(80));
					if (GUILayout.Button("−", GUILayout.Width(24)))
						removeIndex = i;
				}
				m_FieldMappings[i] = mapping;
			}

			EditorGUILayout.EndScrollView();

			if (removeIndex >= 0)
				m_FieldMappings.RemoveAt(removeIndex);

			using (new EditorGUILayout.HorizontalScope())
			{
				GUILayout.FlexibleSpace();
				if (GUILayout.Button("+ Add Mapping", GUILayout.Width(120)))
					m_FieldMappings.Add(new FieldMapping());
			}
		}

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

			int validMappings = 0;
			foreach (var m in m_FieldMappings)
			{
				if (!string.IsNullOrWhiteSpace(m.SourceField) && !string.IsNullOrWhiteSpace(m.TargetField))
					validMappings++;
			}

			string scope = m_EntireProject ? "entire project" : "current scene/prefab";
			string mappingNote = validMappings > 0
				? $"Field mappings: {validMappings} rename(s) will be applied within each replaced component block.\n"
				: "Fields with matching name and type are transferred automatically by Unity.\n" +
					"Non-matching fields will use the target component's default values.\n";
			string msg = $"Replace all '{srcType.Name}' → '{dstType.Name}' in {scope}.\n" +
				mappingNote + "\n" +
				"This operation modifies YAML files directly and cannot be undone. " +
				"Make sure you have a clean working copy.\n\nContinue?";

			if (!EditorUtility.DisplayDialog("Replace Components", msg, "Replace", "Cancel"))
				return;

			var assetPaths = CollectAssetPaths(srcType);
			if (assetPaths.Count == 0)
			{
				EditorUtility.DisplayDialog("Replace Components", "No assets found for the selected scope.", "OK");
				return;
			}

			// Pattern: the m_Script reference to the source type inside any MonoBehaviour YAML block.
			// The GUID is unique per script, so replacing it in m_Script context is safe.
			string searchPattern = $"guid: {srcGuid}, type: 3";
			string replaceWith   = $"guid: {dstGuid}, type: 3";

			int filesModified      = 0;
			int componentsReplaced = 0;
			int errors             = 0;

			// Batch all file writes behind StartAssetEditing so Unity does not try to reimport
			// and re-serialize individual assets mid-loop (which causes SerializedProperty
			// iterator errors because script GUIDs change under open inspectors).
			AssetDatabase.StartAssetEditing();
			try
			{
				for (int i = 0; i < assetPaths.Count; i++)
				{
					string assetPath = assetPaths[i];
					EditorUtility.DisplayProgressBar("Replace Components",
						Path.GetFileName(assetPath), (float)i / assetPaths.Count);

					try
					{
						string fullPath = YamlUtility.AssetPathToFullPath(assetPath);
						string yaml     = File.ReadAllText(fullPath);

						if (!yaml.Contains(searchPattern))
							continue;

						int count      = CountOccurrences(yaml, searchPattern);
						// Apply field renames inside the src-component blocks first (src GUID still present),
						// then swap the GUID.
						string newYaml = ApplyFieldMappings(yaml, srcGuid, m_FieldMappings);
						newYaml        = newYaml.Replace(searchPattern, replaceWith);

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
				AssetDatabase.StopAssetEditing(); // triggers all pending reimports at once
			}

			string summary = $"Replaced: {componentsReplaced} '{srcType.Name}' component(s) → '{dstType.Name}'\n" +
			                 $"Files:    {filesModified} modified";
			if (validMappings > 0)
			summary += $"\nField renames: {validMappings} mapping(s) applied per component block";
			if (errors > 0)
				summary += $"\nErrors:  {errors} (see Console for details)";

			EditorUtility.DisplayDialog("Replace Components – Done", summary, "OK");

			// Force-reload the current scene or prefab so the editor's in-memory
			// representation matches the rewritten YAML and no stale state remains.
			ReloadCurrentContext();
		}

		/// <summary>
		/// Reloads the currently open scene or prefab stage so the editor's in-memory
		/// serialized data reflects the rewritten YAML files.
		/// </summary>
		private static void ReloadCurrentContext()
		{
			EditorApplication.delayCall += () =>
			{
				var prefabStage = PrefabStageUtility.GetCurrentPrefabStage();
				if (prefabStage != null)
				{
					string assetPath = prefabStage.assetPath;
					StageUtility.GoBackToPreviousStage();
					EditorApplication.delayCall +=
						() => AssetDatabase.OpenAsset(AssetDatabase.LoadAssetAtPath<GameObject>(assetPath));
				}
				else
				{
					var scene = SceneManager.GetActiveScene();
					if (scene.IsValid() && !string.IsNullOrEmpty(scene.path))
						EditorSceneManager.OpenScene(scene.path, OpenSceneMode.Single);
				}
			};
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

		// -----------------------------------------------------------------------
		// YAML helpers
		// -----------------------------------------------------------------------

		/// <summary>
		/// Renames top-level serialized fields (2-space indent) within every YAML document
		/// block that contains the given source script GUID. Field renames are applied
		/// <em>before</em> the GUID is swapped so the src GUID still identifies the blocks.
		/// </summary>
		private static string ApplyFieldMappings(string yaml, string srcGuid,
		                                          IReadOnlyList<FieldMapping> mappings)
		{
			if (mappings == null || mappings.Count == 0)
				return yaml;

			// Normalise to LF for block splitting; restore CRLF at the end if needed.
			bool hasCRLF = yaml.Contains("\r\n");
			if (hasCRLF)
				yaml = yaml.Replace("\r\n", "\n");

			var blocks = SplitYamlDocuments(yaml);

			for (int i = 0; i < blocks.Count; i++)
			{
				if (!blocks[i].Contains($"guid: {srcGuid}, type: 3"))
					continue;

				string block = blocks[i];
				foreach (var m in mappings)
				{
					if (string.IsNullOrWhiteSpace(m.SourceField) || string.IsNullOrWhiteSpace(m.TargetField))
						continue;
					// Top-level MonoBehaviour fields are indented with exactly 2 spaces in Unity YAML.
					block = block.Replace($"\n  {m.SourceField}:", $"\n  {m.TargetField}:");
				}
				blocks[i] = block;
			}

			string result = string.Join("", blocks);
			if (hasCRLF)
				result = result.Replace("\n", "\r\n");
			return result;
		}

		/// <summary>
		/// Splits a Unity YAML string into individual document blocks at "---" separators.
		/// Reassembling with <c>string.Join("", blocks)</c> reproduces the original exactly.
		/// </summary>
		private static List<string> SplitYamlDocuments(string yaml)
		{
			var result = new List<string>();
			int start  = 0;
			while (true)
			{
				int pos = yaml.IndexOf("\n---", start);
				if (pos < 0)
					break;
				result.Add(yaml.Substring(start, pos - start + 1)); // keep the '\n' before "---"
				start = pos + 1;                                     // next block starts at "---"
			}
			result.Add(yaml.Substring(start));
			return result;
		}
	}
}