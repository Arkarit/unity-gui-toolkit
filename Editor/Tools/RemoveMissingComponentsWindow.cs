using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Editor
{
	/// <summary>
	/// Editor window that removes missing (broken) script components from all prefabs
	/// inside a chosen project folder, recursively.
	/// </summary>
	[EditorAware]
	internal class RemoveMissingComponentsWindow : EditorWindow
	{
		private const string WINDOW_TITLE = "Remove Missing Components";
		private const string PREFS_KEY    = "RemoveMissingComponents_Folder";

		[SerializeField] private DefaultAsset m_folder;

		private Vector2 m_scroll;
		private readonly List<string> m_log = new List<string>();

		[MenuItem(StringConstants.REMOVE_MISSING_COMPONENTS)]
		public static void Open()
		{
			var window = GetWindow<RemoveMissingComponentsWindow>(WINDOW_TITLE);
			window.minSize = new Vector2(420, 260);
			window.Show();
		}

		private void OnEnable()
		{
			string savedPath = EditorPrefs.GetString(PREFS_KEY, "");
			if (!string.IsNullOrEmpty(savedPath))
			{
				m_folder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(savedPath);
			}
		}

		private void OnGUI()
		{
			EditorGUILayout.Space(6);
			EditorGUI.BeginChangeCheck();
			m_folder = (DefaultAsset) EditorGUILayout.ObjectField("Folder", m_folder, typeof(DefaultAsset), false);
			if (EditorGUI.EndChangeCheck())
			{
				string path = m_folder != null ? AssetDatabase.GetAssetPath(m_folder) : "";
				EditorPrefs.SetString(PREFS_KEY, path);
			}

			EditorGUILayout.Space(4);

			bool hasFolder = m_folder != null && IsFolder(AssetDatabase.GetAssetPath(m_folder));

			using (new EditorGUI.DisabledScope(!hasFolder))
			{
				if (GUILayout.Button("Remove Missing Components", GUILayout.Height(30)))
				{
					Run();
				}
			}

			if (!hasFolder && m_folder != null)
			{
				EditorGUILayout.HelpBox("Please select a folder, not an asset.", MessageType.Warning);
			}

			if (m_log.Count > 0)
			{
				EditorGUILayout.Space(6);
				EditorGUILayout.LabelField("Results", EditorStyles.boldLabel);
				m_scroll = EditorGUILayout.BeginScrollView(m_scroll);
				foreach (string entry in m_log)
				{
					EditorGUILayout.LabelField(entry, EditorStyles.wordWrappedLabel);
				}
				EditorGUILayout.EndScrollView();
			}
		}

		private void Run()
		{
			m_log.Clear();

			string folderPath = AssetDatabase.GetAssetPath(m_folder);
			string[] guids    = AssetDatabase.FindAssets("t:Prefab", new[] { folderPath });

			if (guids.Length == 0)
			{
				m_log.Add("No prefabs found in the selected folder.");
				return;
			}

			int totalRemoved  = 0;
			int prefabsChanged = 0;

			try
			{
				AssetDatabase.StartAssetEditing();

				for (int i = 0; i < guids.Length; i++)
				{
					string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);

					EditorUtility.DisplayProgressBar(
						WINDOW_TITLE,
						assetPath,
						(float) i / guids.Length);

					int removed = ProcessPrefab(assetPath);
					if (removed > 0)
					{
						totalRemoved += removed;
						prefabsChanged++;
						m_log.Add($"  [{removed}] {assetPath}");
					}
				}
			}
			finally
			{
				EditorUtility.ClearProgressBar();
				AssetDatabase.StopAssetEditing();
				AssetDatabase.SaveAssets();
				AssetDatabase.Refresh();
			}

			string summary = totalRemoved == 0
				? $"Done. No missing components found in {guids.Length} prefab(s)."
				: $"Done. Removed {totalRemoved} missing component(s) from {prefabsChanged}/{guids.Length} prefab(s).";

			m_log.Insert(0, summary);
			m_log.Insert(1, "---");

			Debug.Log($"[RemoveMissingComponents] {summary}");
		}

		private static int ProcessPrefab(string assetPath)
		{
			GameObject root = PrefabUtility.LoadPrefabContents(assetPath);
			try
			{
				int removed = RemoveRecursive(root);
				if (removed > 0)
				{
					PrefabUtility.SaveAsPrefabAsset(root, assetPath);
				}
				return removed;
			}
			finally
			{
				PrefabUtility.UnloadPrefabContents(root);
			}
		}

		private static int RemoveRecursive(GameObject go)
		{
			int count = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
			foreach (Transform child in go.transform)
			{
				count += RemoveRecursive(child.gameObject);
			}
			return count;
		}

		private static bool IsFolder(string path)
		{
			return !string.IsNullOrEmpty(path) && System.IO.Directory.Exists(path);
		}
	}
}
