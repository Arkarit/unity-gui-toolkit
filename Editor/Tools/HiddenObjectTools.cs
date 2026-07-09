using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GuiToolkit.Editor
{
	/// <summary>
	/// Diagnostics for hidden scene objects — GameObjects carrying <see cref="HideFlags"/>
	/// (HideInHierarchy / DontSave), which do not appear in the Hierarchy window and are a
	/// common source of "phantom" components (e.g. leaked AudioListeners from editor
	/// previews). One menu item lists them with their flags and components, the other
	/// clears their flags so they become visible and can be inspected or deleted.
	/// </summary>
	public static class HiddenObjectTools
	{
		[MenuItem(StringConstants.DIAGNOSTICS_HEADER + "List Hidden Scene Objects")]
		private static void ListHiddenSceneObjects()
		{
			var hidden = CollectHiddenSceneObjects();
			if (hidden.Count == 0)
			{
				Debug.Log("[HiddenObjectTools] No hidden scene objects found.");
				return;
			}

			var sb = new StringBuilder();
			sb.AppendLine($"[HiddenObjectTools] {hidden.Count} hidden scene object(s):");
			foreach (var go in hidden)
				sb.AppendLine($"  • '{go.name}'  [flags: {go.hideFlags}]  scene: '{go.scene.name}'  components: {DescribeComponents(go)}");

			Debug.Log(sb.ToString());
		}

		[MenuItem(StringConstants.DIAGNOSTICS_HEADER + "Reveal Hidden Scene Objects")]
		private static void RevealHiddenSceneObjects()
		{
			var hidden = CollectHiddenSceneObjects();
			if (hidden.Count == 0)
			{
				Debug.Log("[HiddenObjectTools] No hidden scene objects to reveal.");
				return;
			}

			var dirtyScenes = new HashSet<UnityEngine.SceneManagement.Scene>();
			foreach (var go in hidden)
			{
				go.hideFlags = HideFlags.None;
				EditorUtility.SetDirty(go);
				dirtyScenes.Add(go.scene);
			}

			if (!Application.isPlaying)
			{
				foreach (var scene in dirtyScenes)
				{
					if (scene.IsValid() && scene.isLoaded)
						EditorSceneManager.MarkSceneDirty(scene);
				}
			}

			EditorApplication.RepaintHierarchyWindow();
			Debug.Log($"[HiddenObjectTools] Revealed {hidden.Count} object(s) — they now appear in the Hierarchy and can be inspected or deleted.");
		}

		[MenuItem(StringConstants.DIAGNOSTICS_HEADER + "List Audio Listeners")]
		private static void ListAudioListeners()
		{
			// FindObjectsOfTypeAll also returns disabled and hidden ones (unlike the
			// Hierarchy search), so this catches listeners the Hierarchy won't show.
			var lines = new List<string>();
			int active = 0;
			foreach (var listener in Resources.FindObjectsOfTypeAll<AudioListener>())
			{
				if (EditorUtility.IsPersistent(listener))
					continue;

				var go = listener.gameObject;
				bool effective = listener.isActiveAndEnabled;
				if (effective)
					active++;

				lines.Add($"  • {GetHierarchyPath(go)}  [component enabled: {listener.enabled}, activeInHierarchy: {go.activeInHierarchy} → effectively active: {effective}]  flags: {go.hideFlags}  scene: '{go.scene.name}'");
			}

			if (lines.Count == 0)
			{
				Debug.Log("[HiddenObjectTools] No AudioListeners found.");
				return;
			}

			var sb = new StringBuilder();
			sb.AppendLine($"[HiddenObjectTools] {lines.Count} AudioListener(s) total, {active} effectively active (Unity warns when >1 active):");
			foreach (var line in lines)
				sb.AppendLine(line);

			Debug.Log(sb.ToString());
		}

		private static string GetHierarchyPath( GameObject _go )
		{
			var parts = new List<string>();
			for (var t = _go.transform; t != null; t = t.parent)
				parts.Add(t.name);
			parts.Reverse();
			return string.Join("/", parts);
		}

		private static List<GameObject> CollectHiddenSceneObjects()
		{
			var result = new List<GameObject>();
			foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
			{
				// Skip assets/prefabs, but keep no-scene runtime objects: leaked
				// HideAndDontSave objects report an empty scene, yet are exactly what we hunt.
				if (EditorUtility.IsPersistent(go))
					continue;
				if (go.hideFlags == HideFlags.None)
					continue;

				result.Add(go);
			}
			return result;
		}

		private static string DescribeComponents( GameObject _go )
		{
			var names = new List<string>();
			foreach (var component in _go.GetComponents<Component>())
				names.Add(component == null ? "<missing>" : component.GetType().Name);
			return names.Count > 0 ? string.Join(", ", names) : "<none>";
		}
	}
}
