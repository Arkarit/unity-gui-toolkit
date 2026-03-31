using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace GuiToolkit.Editor
{
	/// <summary>
	/// Editor window that scans all prefabs (and optionally open scenes) for broken or null
	/// serialized component references.
	/// <para>
	/// Optionally filters by the declared field type: e.g. set the filter to
	/// <c>UnityEngine.UI.Text</c> to find all <c>Text</c> fields that became null after a
	/// Legacy Text → TMP conversion.  Without a filter every serialized
	/// <see cref="Object"/> reference is checked.
	/// </para>
	/// </summary>
	[EditorAware]
	internal class MissingReferencesFinder : EditorWindow
	{
		private const string WINDOW_TITLE = "Missing References Finder";

		[SerializeField] private MonoScript m_FieldTypeScript;
		[SerializeField] private bool       m_IncludeNull;
		[SerializeField] private bool       m_IncludeOpenScenes;

		private readonly List<Finding> m_Findings = new();
		private Vector2 m_Scroll;
		private string  m_StatusLine = "Press \"Scan\" to start.";
		private bool    m_HasScanned;

		// -----------------------------------------------------------------------

		private struct Finding
		{
			public string AssetPath;
			public string HierarchyPath;
			public string ComponentTypeName;
			public string FieldPath;       // SerializedProperty path (may include [0] for arrays)
			public string FieldTypeName;
			public bool   IsMissing;       // true = instance ID != 0 but value resolved to null
			public Object PingTarget;      // asset to ping / select on click
		}

		// -----------------------------------------------------------------------

		[MenuItem(StringConstants.MISSING_REFERENCES_FINDER_MENU_NAME,
		          priority = Constants.MISSING_REFERENCES_FINDER_MENU_PRIORITY)]
		public static void ShowWindow()
		{
			var w = GetWindow<MissingReferencesFinder>(WINDOW_TITLE);
			w.minSize = new Vector2(560, 340);
		}

		// -----------------------------------------------------------------------

		private void OnGUI()
		{
			EditorGUILayout.LabelField("Missing Component References Finder", EditorStyles.boldLabel);
			EditorGUILayout.Space(4);

			m_FieldTypeScript = (MonoScript)EditorGUILayout.ObjectField(
				new GUIContent("Field Type filter",
					"Optional: drag a script here to restrict the scan to fields of that type. " +
					"E.g. drag UnityEngine.UI.Text to find all Text fields that are broken. " +
					"Leave empty to scan ALL serialized Object-reference fields."),
				m_FieldTypeScript, typeof(MonoScript), false);

			if (m_FieldTypeScript != null)
			{
				var t = m_FieldTypeScript.GetClass();
				if (t == null || !typeof(Object).IsAssignableFrom(t))
				{
					EditorGUILayout.HelpBox(
						"Selected script's class must derive from UnityEngine.Object.",
						MessageType.Warning);
				}
				else
				{
					EditorGUILayout.HelpBox(
						$"Filter: fields declared as {t.FullName} (or a subtype).",
						MessageType.Info);
				}
			}
			else
			{
				EditorGUILayout.HelpBox(
					"No filter: all serialized UnityEngine.Object reference fields will be checked.",
					MessageType.Info);
			}

			EditorGUILayout.Space(4);

			m_IncludeNull = EditorGUILayout.ToggleLeft(
				new GUIContent("Include unassigned (null) fields",
					"Also report fields that are intentionally null (instance ID = 0). " +
					"Without this, only definitively broken references (non-zero instance ID) are shown."),
				m_IncludeNull);

			m_IncludeOpenScenes = EditorGUILayout.ToggleLeft(
				new GUIContent("Also scan currently open scenes",
					"Scans GameObjects in all scenes that are currently loaded in the editor."),
				m_IncludeOpenScenes);

			EditorGUILayout.Space(8);

			Type filterType = m_FieldTypeScript?.GetClass();
			bool filterValid = filterType == null || typeof(Object).IsAssignableFrom(filterType);

			EditorGUILayout.BeginHorizontal();
			using (new EditorGUI.DisabledScope(!filterValid))
			{
				if (GUILayout.Button("Scan", GUILayout.Height(24)))
					RunScan(filterType);
			}
			if (m_HasScanned && GUILayout.Button("Clear", GUILayout.Height(24), GUILayout.Width(60)))
			{
				m_Findings.Clear();
				m_StatusLine = "Cleared.";
				Repaint();
			}
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.Space(4);
			EditorGUILayout.LabelField(m_StatusLine, EditorStyles.miniLabel);

			if (m_HasScanned && m_Findings.Count > 0)
			{
				EditorGUILayout.Space(2);
				m_Scroll = EditorGUILayout.BeginScrollView(m_Scroll);

				foreach (var f in m_Findings)
				{
					string icon  = f.IsMissing ? "\u26a0" : "\u25cb";   // ⚠ or ○
					string label =
						$"{icon}  [{(f.IsMissing ? "MISSING" : "null")}]  " +
						$"{Path.GetFileNameWithoutExtension(f.AssetPath)}  ›  " +
						$"{f.HierarchyPath}  ·  " +
						$"{f.ComponentTypeName}.{f.FieldPath}  :  {f.FieldTypeName}";

					if (GUILayout.Button(label, EditorStyles.linkLabel))
					{
						if (f.PingTarget != null)
						{
							EditorGUIUtility.PingObject(f.PingTarget);
							Selection.activeObject = f.PingTarget;
						}
					}
				}

				EditorGUILayout.EndScrollView();
			}
		}

		// -----------------------------------------------------------------------

		private void RunScan(Type filterType)
		{
			m_Findings.Clear();
			m_HasScanned = false;

			int prefabCount = 0;

			try
			{
				string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");
				int total = prefabGuids.Length;

				for (int i = 0; i < total; i++)
				{
					if (i % 5 == 0)
					{
						string shortName = Path.GetFileName(AssetDatabase.GUIDToAssetPath(prefabGuids[i]));
						bool cancelled = EditorUtility.DisplayCancelableProgressBar(
							WINDOW_TITLE,
							$"Scanning {shortName}… ({i}/{total})",
							(float)i / total);
						if (cancelled)
						{
							m_StatusLine = "Scan cancelled.";
							m_HasScanned = true;
							return;
						}
					}

					ScanPrefab(AssetDatabase.GUIDToAssetPath(prefabGuids[i]), filterType);
					prefabCount++;
				}

				if (m_IncludeOpenScenes)
				{
					for (int s = 0; s < SceneManager.sceneCount; s++)
					{
						var scene = SceneManager.GetSceneAt(s);
						if (scene.isLoaded)
							ScanScene(scene, filterType);
					}
				}
			}
			finally
			{
				EditorUtility.ClearProgressBar();
			}

			int missing  = 0;
			int nullOnly = 0;
			foreach (var f in m_Findings)
			{
				if (f.IsMissing) missing++;
				else             nullOnly++;
			}

			m_StatusLine = m_Findings.Count == 0
				? $"No issues found in {prefabCount} prefab(s)."
				: $"Found {missing} missing + {nullOnly} null  =  {m_Findings.Count} total  (in {prefabCount} prefab(s))";

			foreach (var f in m_Findings)
			{
				string kind = f.IsMissing ? "MISSING" : "null";
				Debug.LogWarning(
					$"[MissingRefs] [{kind}]  {f.AssetPath}  ›  {f.HierarchyPath}" +
					$"  ·  {f.ComponentTypeName}.{f.FieldPath} : {f.FieldTypeName}",
					f.PingTarget);
			}

			if (m_Findings.Count == 0)
				Debug.Log($"[MissingRefs] Scan complete — no missing references found ({prefabCount} prefabs checked).");
			else
				Debug.Log($"[MissingRefs] Scan complete — {m_StatusLine}");

			m_HasScanned = true;
			Repaint();
		}

		// -----------------------------------------------------------------------

		private void ScanPrefab(string assetPath, Type filterType)
		{
			var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
			if (prefabAsset == null)
				return;

			GameObject contents = null;
			try
			{
				contents = PrefabUtility.LoadPrefabContents(assetPath);
				foreach (var comp in contents.GetComponentsInChildren<Component>(true))
				{
					if (comp == null)
						continue;
					CheckComponent(comp, assetPath, GetHierarchyPath(comp.transform), filterType, prefabAsset);
				}
			}
			catch (Exception ex)
			{
				Debug.LogWarning($"[MissingRefs] Error scanning {assetPath}: {ex.Message}");
			}
			finally
			{
				if (contents != null)
					PrefabUtility.UnloadPrefabContents(contents);
			}
		}

		private void ScanScene(UnityEngine.SceneManagement.Scene scene, Type filterType)
		{
			foreach (var root in scene.GetRootGameObjects())
			{
				foreach (var comp in root.GetComponentsInChildren<Component>(true))
				{
					if (comp == null)
						continue;
					CheckComponent(comp, scene.path, GetHierarchyPath(comp.transform), filterType, null);
				}
			}
		}

		// -----------------------------------------------------------------------

		private void CheckComponent(Component component, string assetPath,
		                            string hierarchyPath, Type filterType, Object pingTarget)
		{
			var compType = component.GetType();

			// When a filter is set, build the set of field-name roots that match the filter type.
			// This avoids checking every property and is used to gate iterator traversal below.
			HashSet<string> allowedRoots = null;
			if (filterType != null)
			{
				allowedRoots = BuildAllowedRoots(compType, filterType);
				if (allowedRoots.Count == 0)
					return;
			}

			using var so = new SerializedObject(component);
			var prop = so.GetIterator();
			bool enterChildren = true;

			while (prop.Next(enterChildren))
			{
				// Don't enter into strings (Unity treats them as char arrays internally).
				enterChildren = prop.propertyType != SerializedPropertyType.String;

				if (prop.propertyType != SerializedPropertyType.ObjectReference)
					continue;

				if (allowedRoots != null && !allowedRoots.Contains(GetRootName(prop.propertyPath)))
					continue;

				bool isMissing = prop.objectReferenceValue == null
				              && prop.objectReferenceInstanceIDValue != 0;
				bool isNull    = prop.objectReferenceValue == null
				              && prop.objectReferenceInstanceIDValue == 0;

				if (!isMissing && (!isNull || !m_IncludeNull))
					continue;

				m_Findings.Add(new Finding
				{
					AssetPath         = assetPath,
					HierarchyPath     = hierarchyPath,
					ComponentTypeName = compType.Name,
					FieldPath         = prop.propertyPath,
					FieldTypeName     = GetFieldTypeName(compType, GetRootName(prop.propertyPath)),
					IsMissing         = isMissing,
					PingTarget        = pingTarget,
				});
			}
		}

		// -----------------------------------------------------------------------

		/// <summary>
		/// Collects the names of all serialized fields in <paramref name="compType"/>
		/// (including inherited) whose element type is assignable from <paramref name="filterType"/>.
		/// </summary>
		private static HashSet<string> BuildAllowedRoots(Type compType, Type filterType)
		{
			var result = new HashSet<string>();
			var type = compType;

			while (type != null && type != typeof(MonoBehaviour) &&
			       type != typeof(Behaviour) && type != typeof(Component))
			{
				foreach (var f in type.GetFields(
					BindingFlags.Instance | BindingFlags.Public |
					BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
				{
					if (f.IsDefined(typeof(NonSerializedAttribute), false))
						continue;
					if (!f.IsPublic && !f.IsDefined(typeof(SerializeField), false))
						continue;

					if (filterType.IsAssignableFrom(GetElementType(f.FieldType)))
						result.Add(f.Name);
				}
				type = type.BaseType;
			}

			return result;
		}

		/// <summary>Returns the root field name from a serialized property path.</summary>
		private static string GetRootName(string propertyPath)
		{
			int dotIdx = propertyPath.IndexOf('.');
			return dotIdx >= 0 ? propertyPath.Substring(0, dotIdx) : propertyPath;
		}

		/// <summary>
		/// Looks up the declared C# field matching <paramref name="fieldName"/> in the type
		/// hierarchy and returns its type's short name for display purposes.
		/// </summary>
		private static string GetFieldTypeName(Type compType, string fieldName)
		{
			var type = compType;
			while (type != null && type != typeof(MonoBehaviour) &&
			       type != typeof(Behaviour) && type != typeof(Component))
			{
				var f = type.GetField(fieldName,
					BindingFlags.Instance | BindingFlags.Public |
					BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
				if (f != null)
					return f.FieldType.Name;
				type = type.BaseType;
			}
			return "Object";
		}

		/// <summary>Returns the element type for arrays / generic lists; otherwise the type itself.</summary>
		private static Type GetElementType(Type t)
		{
			if (t.IsArray)
				return t.GetElementType() ?? t;
			if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(List<>))
				return t.GetGenericArguments()[0];
			return t;
		}

		/// <summary>Builds the full GameObject path from root to <paramref name="t"/>.</summary>
		private static string GetHierarchyPath(Transform t)
		{
			var parts = new List<string>();
			while (t != null)
			{
				parts.Insert(0, t.name);
				t = t.parent;
			}
			return string.Join("/", parts);
		}
	}
}
