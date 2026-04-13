using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GuiToolkit.Editor
{
	/// <summary>
	/// Adds a "Replace with Localized Text" entry to the TextMeshProUGUI component context menu.
	/// Swaps the component's script type to <see cref="UiLocalizedTextMeshProUGUI"/> in the
	/// YAML asset file, preserving all serialized TMP settings and all external references.
	/// </summary>
	internal static class ReplaceWithLocalizedText
	{
		private const string MenuPath = "CONTEXT/TextMeshProUGUI/Replace with Localized Text";

		[MenuItem(MenuPath)]
		private static void Execute(MenuCommand command)
		{
			var tmp = command.context as TextMeshProUGUI;
			if (tmp == null || tmp is UiLocalizedTextMeshProUGUI)
				return;

			var go = tmp.gameObject;

			// Refuse on prefab instances — the component data lives in the source prefab,
			// not in the currently open scene file.
			if (YamlUtility.IsPartOfPrefabInstance(go))
			{
				EditorUtility.DisplayDialog(
					"Replace with Localized Text",
					"This GameObject is part of a prefab instance.\n\n" +
					"Open the source prefab directly (double-click it in the Project window) " +
					"and run this command there instead.",
					"OK");
				return;
			}

			// Refuse if the containing scene/prefab has never been saved.
			string assetPath = YamlUtility.GetEditedAssetPath(go);
			if (string.IsNullOrEmpty(assetPath))
			{
				EditorUtility.DisplayDialog(
					"Replace with Localized Text",
					"The current scene has not been saved yet.\n\n" +
					"Save the scene first (Ctrl+S / Cmd+S) and try again.",
					"OK");
				return;
			}

			bool isInPrefabStage = PrefabStageUtility.GetCurrentPrefabStage() != null;

			// Handle a co-existing (deprecated) UiAutoLocalize component.
#pragma warning disable CS0618
			var autoLocalize = go.GetComponent<UiAutoLocalize>();
#pragma warning restore CS0618
			if (autoLocalize != null)
			{
				int choice = EditorUtility.DisplayDialogComplex(
					"UiAutoLocalize Found",
					"This GameObject also has a deprecated UiAutoLocalize component, which conflicts with " +
					"UiLocalizedTextMeshProUGUI. It should be removed.",
					"Remove", "Cancel", "Ignore");

				if (choice == 1) return;                     // Cancel
				if (choice == 0) Undo.DestroyObjectImmediate(autoLocalize); // Remove
				// choice == 2: Ignore → continue
			}

			var forceUnlocalized = go.GetComponent<UiForceUnlocalizedText>();
			if (forceUnlocalized != null)
			{
				if (!EditorUtility.DisplayDialog(
					    "UiForceUnlocalizedText Found",
					    "This GameObject has a UiForceUnlocalizedText marker, which prevents auto-conversion.\n\n" +
					    "It will be removed to allow this conversion.",
					    "Remove and Continue", "Cancel"))
					return;
				Undo.DestroyObjectImmediate(forceUnlocalized);
			}

			// Collect GUIDs before any file operations (component is still alive here).
			string oldGuid = YamlUtility.GetMonoScriptGuid(tmp);
			if (string.IsNullOrEmpty(oldGuid))
			{
				EditorUtility.DisplayDialog("Replace with Localized Text",
					"Could not determine the MonoScript GUID for TextMeshProUGUI.", "OK");
				return;
			}

			string newGuid = YamlUtility.FindMonoScriptGuid(typeof(UiLocalizedTextMeshProUGUI));
			if (string.IsNullOrEmpty(newGuid))
			{
				EditorUtility.DisplayDialog("Replace with Localized Text",
					"Could not find UiLocalizedTextMeshProUGUI in the project.\n\n" +
					"Make sure the UI Toolkit package is imported correctly.", "OK");
				return;
			}

			if (!YamlUtility.TryGetLocalFileId(tmp, out long localId))
			{
				EditorUtility.DisplayDialog("Replace with Localized Text",
					"Could not determine the component's local file identifier.", "OK");
				return;
			}

			// Confirm save — this is the point of no return (no Undo after file write).
			if (!EditorUtility.DisplayDialog(
				    "Replace with Localized Text",
				    "This operation will:\n" +
				    "  1. Save the current scene / prefab\n" +
				    "  2. Patch the YAML file directly\n" +
				    "  3. Reload the scene / prefab\n\n" +
				    "All TMP settings and external references are preserved. " +
				    "The operation cannot be undone.\n\nContinue?",
				    "Save and Replace", "Cancel"))
				return;

			if (!YamlUtility.SaveCurrentSceneOrPrefab())
			{
				EditorUtility.DisplayDialog("Replace with Localized Text",
					"Could not save the scene / prefab.", "OK");
				return;
			}

			if (!YamlUtility.SwapComponentScript(assetPath, localId, oldGuid, newGuid))
			{
				EditorUtility.DisplayDialog("Replace with Localized Text",
					"Failed to patch the asset file. See the Console for details.", "OK");
				return;
			}

			// Reload to pick up the new component type from disk.
			// SaveCurrentSceneOrPrefab() was called just before the patch, so isDirty is false
			// and no "save changes?" dialog will appear.
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

		[MenuItem(MenuPath, true)]
		private static bool Validate(MenuCommand command)
			=> command.context is TextMeshProUGUI tmp && tmp is not UiLocalizedTextMeshProUGUI;
	}
}
