using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GuiToolkit.Editor
{
	/// <summary>
	/// Right-click context menu tool that renames a Project-window folder and,
	/// optionally, every file asset inside it whose name contains the old folder name.
	/// </summary>
	public static class RenameFolder
	{
		private const int MenuPriority = -849;

		[MenuItem(StringConstants.RENAME_FOLDER, false, MenuPriority)]
		private static void Execute()
		{
			string srcFolder = GetSelectedFolderPath();
			if (srcFolder == null)
			{
				EditorUtility.DisplayDialog("Rename Folder",
					"Please select a single folder in the Project window first.", "OK");
				return;
			}

			string parentFolder = Path.GetDirectoryName(srcFolder)?.Replace('\\', '/') ?? "Assets";
			string oldName      = Path.GetFileName(srcFolder);

			bool renameAssets = true;
			string input = EditorInputDialog.Show(
				"Rename Folder",
				"Enter new name:",
				oldName,
				_ => { renameAssets = EditorGUILayout.ToggleLeft("Rename assets containing the folder name", renameAssets); });

			if (input == null)   // user cancelled
				return;

			string newName = input.Trim();

			if (string.IsNullOrWhiteSpace(newName))
			{
				EditorUtility.DisplayDialog("Rename Folder",
					"Folder name cannot be empty.", "OK");
				return;
			}

			if (newName == oldName)
				return;  // nothing to do

			string destFolder = $"{parentFolder}/{newName}";
			if (AssetDatabase.IsValidFolder(destFolder))
			{
				EditorUtility.DisplayDialog("Rename Folder",
					$"A folder named '{newName}' already exists at that location.", "OK");
				return;
			}

			string error = AssetDatabase.RenameAsset(srcFolder, newName);
			if (!string.IsNullOrEmpty(error))
			{
				EditorUtility.DisplayDialog("Rename Folder",
					$"Could not rename folder: {error}", "OK");
				return;
			}

			if (renameAssets)
				CloneFolder.RenameMatchingAssets(destFolder, oldName, newName);

			// Ping & select the renamed folder
			var folderObj = AssetDatabase.LoadAssetAtPath<Object>(destFolder);
			if (folderObj != null)
			{
				Selection.activeObject = folderObj;
				EditorGUIUtility.PingObject(folderObj);
			}
		}

		[MenuItem(StringConstants.RENAME_FOLDER, true, MenuPriority)]
		private static bool Validate() => true;

		/// <summary>Returns the asset path of the selected folder, or null if none.</summary>
		private static string GetSelectedFolderPath()
		{
			Object obj = Selection.activeObject;

			if (obj == null)
			{
				var guids = Selection.assetGUIDs;
				if (guids != null && guids.Length > 0)
					obj = AssetDatabase.LoadAssetAtPath<Object>(AssetDatabase.GUIDToAssetPath(guids[0]));
			}

			if (obj == null && Selection.objects != null && Selection.objects.Length > 0)
				obj = Selection.objects[0];

			if (obj == null)
				return null;

			string path = AssetDatabase.GetAssetPath(obj);
			return AssetDatabase.IsValidFolder(path) ? path : null;
		}
	}
}
