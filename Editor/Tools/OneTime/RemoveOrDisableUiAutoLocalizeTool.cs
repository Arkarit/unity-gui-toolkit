// One-time tool: disables or removes every UiAutoLocalize component in all prefabs and scenes.
// Typical workflow: Disable first (safe, reversible) → audit → Remove in a later run.
// Run once per action, verify, then delete this file.
// Menu: AssetFixing/[One-Time] Disable or Remove All UiAutoLocalize

using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GuiToolkit.Editor
{
	/// <summary>
	/// One-time editor tool to disable or remove all <see cref="UiAutoLocalize"/> components in the project.
	/// Processes all prefabs and scenes in the Assets folder.
	/// Useful for migrating away from the deprecated component to <see cref="UiLocalizedTextMeshProUGUI"/>.
	/// Run once per action, verify results, then delete this file.
	/// Invoked via menu defined in <see cref="StringConstants.DISABLE_OR_REMOVE_UI_AUTO_LOCALIZE"/>.
	/// </summary>
	public static class RemoveOrDisableUiAutoLocalizeTool
	{
		private enum Mode { Disable, Remove }

		private const string MenuPath = "AssetFixing/[One-Time] Disable or Remove All UiAutoLocalize";

		/// <summary>
		/// Displays a dialog prompting the user to disable or remove all <see cref="UiAutoLocalize"/> components.
		/// Processes all prefabs and scenes, updating the components in place and marking assets dirty.
		/// Shows progress bars during execution and a summary dialog on completion.
		/// </summary>
		[MenuItem(StringConstants.DISABLE_OR_REMOVE_UI_AUTO_LOCALIZE)]
		public static void Run()
		{
			// DisplayDialogComplex returns: 0 = ok ("Disable"), 1 = cancel ("Cancel"), 2 = alt ("Remove")
			int choice = EditorUtility.DisplayDialogComplex(
				"Disable or Remove All UiAutoLocalize",
				"Choose an action for every UiAutoLocalize component in all prefabs and scenes:\n\n" +
				"• Disable — sets enabled = false. Components stay in place for auditing and can be re-enabled.\n\n" +
				"• Remove  — permanently destroys the components. Also finds already-disabled ones.\n\n" +
				"Make sure your work is committed to version control first.",
				"Disable", "Cancel", "Remove");

			if (choice == 1)
				return;

			Mode mode = choice == 0 ? Mode.Disable : Mode.Remove;
			string action = mode == Mode.Disable ? "Disabled" : "Removed";

			int total = 0;
			var log = new StringBuilder();

			AssetDatabase.SaveAssets();

			ProcessPrefabs(mode, ref total, log);
			ProcessScenes(mode, ref total, log);

			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();

			string summary = total == 0
				? "No UiAutoLocalize components found."
				: $"{action} {total} UiAutoLocalize component(s) in:\n\n{log}";

			Debug.Log($"[DisableUiAutoLocalizeTool] {summary}");
			EditorUtility.DisplayDialog($"{action} All UiAutoLocalize — Done", summary, "OK");
		}

		private static void ProcessPrefabs(Mode mode, ref int total, StringBuilder log)
		{
			string[] guids = AssetDatabase.FindAssets("t:Prefab");
			int count = guids.Length;
			int i = 0;

			foreach (string guid in guids)
			{
				string path = AssetDatabase.GUIDToAssetPath(guid);
				i++;

				// Skip prefabs inside packages (immutable).
				if (!path.StartsWith("Assets/"))
					continue;

				EditorUtility.DisplayProgressBar(
					$"{mode} UiAutoLocalize — Prefabs",
					$"{i}/{count}: {path}",
					(float)i / count);

				var root = PrefabUtility.LoadPrefabContents(path);
				var components = root.GetComponentsInChildren<UiAutoLocalize>(includeInactive: true);

				if (components.Length > 0)
				{
					foreach (var c in components)
					{
						if (mode == Mode.Disable)
							c.enabled = false;
						else
							Object.DestroyImmediate(c);
					}

					PrefabUtility.SaveAsPrefabAsset(root, path);
					log.AppendLine($"  [{components.Length,2}x]  {path}");
					total += components.Length;
				}

				PrefabUtility.UnloadPrefabContents(root);
			}

			EditorUtility.ClearProgressBar();
		}

		private static void ProcessScenes(Mode mode, ref int total, StringBuilder log)
		{
			string[] guids = AssetDatabase.FindAssets("t:Scene");
			int count = guids.Length;
			int i = 0;

			// Remember which scenes are already open so we do not close them afterwards.
			var alreadyOpenPaths = new HashSet<string>();
			for (int s = 0; s < SceneManager.sceneCount; s++)
				alreadyOpenPaths.Add(SceneManager.GetSceneAt(s).path);

			foreach (string guid in guids)
			{
				string path = AssetDatabase.GUIDToAssetPath(guid);
				i++;

				// Skip scenes inside packages (immutable).
				if (!path.StartsWith("Assets/"))
					continue;

				EditorUtility.DisplayProgressBar(
					$"{mode} UiAutoLocalize — Scenes",
					$"{i}/{count}: {path}",
					(float)i / count);

				bool wasAlreadyOpen = alreadyOpenPaths.Contains(path);
				Scene scene = wasAlreadyOpen
					? SceneManager.GetSceneByPath(path)
					: EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);

				var components = scene.GetRootGameObjects()
					.SelectMany(go => go.GetComponentsInChildren<UiAutoLocalize>(includeInactive: true))
					.ToArray();

				if (components.Length > 0)
				{
					foreach (var c in components)
					{
						if (mode == Mode.Disable)
							c.enabled = false;
						else
							Object.DestroyImmediate(c);
					}

					EditorSceneManager.SaveScene(scene);
					log.AppendLine($"  [{components.Length,2}x]  {path}");
					total += components.Length;
				}

				if (!wasAlreadyOpen)
					EditorSceneManager.CloseScene(scene, removeScene: true);
			}

			EditorUtility.ClearProgressBar();
		}
	}
}
