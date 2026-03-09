using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Editor
{
	public static class UiLocalizedTextMigrationTool
	{
		[MenuItem("Tools/Loca/Migrate UiAutoLocalize (Find candidates)")]
		public static void FindMigrationCandidates()
		{
			var candidates = new List<string>();

			string[] guids = AssetDatabase.FindAssets("t:Prefab");
			foreach (string guid in guids)
			{
				string path = AssetDatabase.GUIDToAssetPath(guid);
				GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
				if (prefab == null)
					continue;

#pragma warning disable CS0618
				foreach (var comp in prefab.GetComponentsInChildren<UiAutoLocalize>(true))
					candidates.Add($"{path} → {comp.gameObject.name}");
#pragma warning restore CS0618
			}

			if (candidates.Count == 0)
			{
				Debug.Log("[Loca Migration] No UiAutoLocalize components found. Nothing to migrate.");
				EditorUtility.DisplayDialog("Loca Migration", "No UiAutoLocalize components found in project prefabs.", "OK");
				return;
			}

			string report = $"Found {candidates.Count} UiAutoLocalize component(s):\n\n" +
			                string.Join("\n", candidates) +
			                "\n\nReplace TextMeshProUGUI with UiLocalizedTextMeshProUGUI manually on each.";

			Debug.Log("[Loca Migration] " + report);
			EditorUtility.DisplayDialog("Loca Migration Candidates",
				$"Found {candidates.Count} UiAutoLocalize component(s). See Console for details.", "OK");
		}
	}
}
