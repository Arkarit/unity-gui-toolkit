using TMPro;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Editor
{
	/// <summary>
	/// Adds a "Replace with Localized Text" entry to the TextMeshProUGUI component context menu.
	/// Replaces the component with UiLocalizedTextMeshProUGUI while preserving all TMP settings
	/// (font, size, color, alignment, text, etc.).
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

			// Capture all serialized TMP state before destroying the component.
			// EditorJsonUtility serializes by field name, so all TMP fields that exist
			// in the subclass (by inheritance) will be restored correctly.
			string json = EditorJsonUtility.ToJson(tmp);

			Undo.IncrementCurrentGroup();
			int group = Undo.GetCurrentGroup();

			Undo.DestroyObjectImmediate(tmp);
			var localized = Undo.AddComponent<UiLocalizedTextMeshProUGUI>(go);

			// Restore TMP settings onto the new component.
			// UiLocalizedTextMeshProUGUI-specific fields (LocaKey, Group, AutoLocalize)
			// are not present in the JSON and keep their defaults.
			EditorJsonUtility.FromJsonOverwrite(json, localized);

			Undo.SetCurrentGroupName("Replace with Localized Text");
			Undo.CollapseUndoOperations(group);

			EditorUtility.SetDirty(go);
		}

		[MenuItem(MenuPath, true)]
		private static bool Validate(MenuCommand command)
			=> command.context is TextMeshProUGUI tmp && tmp is not UiLocalizedTextMeshProUGUI;
	}
}
