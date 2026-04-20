using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GuiToolkit.Editor
{
	/// <summary>
	/// Adds a "Replace with Plain TMP Text" entry to the <see cref="UiLocalizedTextMeshProUGUI"/>
	/// component context menu. Swaps the component's script type back to
	/// <see cref="TextMeshProUGUI"/> in the YAML asset file, removing the localization-specific
	/// fields (<c>m_group</c>, <c>m_locaKey</c>) while preserving all other TMP settings and
	/// external references. Optionally adds a <see cref="UiForceUnlocalizedText"/> marker to
	/// prevent re-conversion by batch routines.
	/// </summary>
	internal static class ReplaceWithPlainTmpText
	{
		private const string MenuPath = "CONTEXT/UiLocalizedTextMeshProUGUI/Replace with Plain TMP Text";

		[MenuItem(MenuPath)]
		private static void Execute(MenuCommand command)
		{
			var localized = command.context as UiLocalizedTextMeshProUGUI;
			if (localized == null)
				return;

			var go = localized.gameObject;

			if (YamlUtility.IsPartOfPrefabInstance(go))
			{
				EditorUtility.DisplayDialog(
					"Replace with Plain TMP Text",
					"This GameObject is part of a prefab instance.\n\n" +
					"Open the source prefab directly (double-click it in the Project window) " +
					"and run this command there instead.",
					"OK");
				return;
			}

			string assetPath = YamlUtility.GetEditedAssetPath(go);
			if (string.IsNullOrEmpty(assetPath))
			{
				EditorUtility.DisplayDialog(
					"Replace with Plain TMP Text",
					"The current scene has not been saved yet.\n\n" +
					"Save the scene first (Ctrl+S / Cmd+S) and try again.",
					"OK");
				return;
			}

			bool isInPrefabStage = PrefabStageUtility.GetCurrentPrefabStage() != null;

			string oldGuid = YamlUtility.GetMonoScriptGuid(localized);
			if (string.IsNullOrEmpty(oldGuid))
			{
				EditorUtility.DisplayDialog("Replace with Plain TMP Text",
					"Could not determine the MonoScript GUID for UiLocalizedTextMeshProUGUI.", "OK");
				return;
			}

			string newGuid = YamlUtility.FindMonoScriptGuid(typeof(TextMeshProUGUI));
			if (string.IsNullOrEmpty(newGuid))
			{
				EditorUtility.DisplayDialog("Replace with Plain TMP Text",
					"Could not find TextMeshProUGUI in the project.", "OK");
				return;
			}

			if (!YamlUtility.TryGetLocalFileId(localized, out long localId))
			{
				EditorUtility.DisplayDialog("Replace with Plain TMP Text",
					"Could not determine the component's local file identifier.", "OK");
				return;
			}

			bool addForceUnlocalized = EditorUtility.DisplayDialog(
				"Replace with Plain TMP Text",
				"Add a UiForceUnlocalizedText marker component?\n\n" +
				"This prevents the text from being auto-converted back to UiLocalizedTextMeshProUGUI " +
				"by batch conversion routines.",
				"Yes (Recommended)", "No");

			if (!EditorUtility.DisplayDialog(
				    "Replace with Plain TMP Text",
				    "This operation will:\n" +
				    "  1. Save the current scene / prefab\n" +
				    "  2. Patch the YAML file directly\n" +
				    "  3. Reload the scene / prefab\n\n" +
				    "All TMP settings and external references are preserved. " +
				    "The operation cannot be undone.\n\nContinue?",
				    "Save and Replace", "Cancel"))
				return;

			if (addForceUnlocalized)
				go.AddComponent<UiForceUnlocalizedText>();

			if (!YamlUtility.SaveCurrentSceneOrPrefab())
			{
				EditorUtility.DisplayDialog("Replace with Plain TMP Text",
					"Could not save the scene / prefab.", "OK");
				return;
			}

			if (!YamlUtility.SwapComponentScriptAndStripFields(
				    assetPath, localId, oldGuid, newGuid, "m_group", "m_locaKey"))
			{
				EditorUtility.DisplayDialog("Replace with Plain TMP Text",
					"Failed to patch the asset file. See the Console for details.", "OK");
				return;
			}

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
			=> command.context is UiLocalizedTextMeshProUGUI;
	}
}
