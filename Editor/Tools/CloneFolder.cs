using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GuiToolkit.Editor
{
	/// <summary>
	/// Right-click context menu tool that deep-clones a Project-window folder.
	/// External asset references are kept intact; references between assets
	/// inside the cloned folder are rewired to point to their cloned counterparts.
	/// </summary>
	public static class CloneFolder
	{
		private const int MenuPriority = -850;

		// -------------------------------------------------------------------------
		// Menu item
		// -------------------------------------------------------------------------

		[MenuItem(StringConstants.CLONE_FOLDER, false, MenuPriority)]
		private static void Execute()
		{
			string srcFolder = GetSelectedFolderPath();
			if (srcFolder == null)
			{
				EditorUtility.DisplayDialog("Clone Folder",
					"Please select a single folder in the Project window first.", "OK");
				return;
			}

			string parentFolder = Path.GetDirectoryName(srcFolder)?.Replace('\\', '/') ?? "Assets";
			string folderName   = Path.GetFileName(srcFolder);

			// --- Ask for name (with optional rename-assets toggle) ---
			bool renameAssets = false;
			string input = EditorInputDialog.Show(
				"Clone Folder",
				$"Enter name: (Leave empty for auto-name)",
				"",
				_ => { renameAssets = UnityEditor.EditorGUILayout.ToggleLeft("Rename assets containing the folder name", renameAssets); });

			if (input == null)   // user cancelled
				return;

			string destFolder;
			if (string.IsNullOrWhiteSpace(input))
			{
				destFolder = AssetDatabase.GenerateUniqueAssetPath($"{parentFolder}/{folderName}");
			}
			else
			{
				destFolder = $"{parentFolder}/{input.Trim()}";
				if (AssetDatabase.IsValidFolder(destFolder))
				{
					EditorUtility.DisplayDialog(
						"Clone Folder",
						$"A folder named '{input.Trim()}' already exists at that location.",
						"OK");
					return;
				}
			}

			// --- Copy + rewire ---
			if (!Clone(srcFolder, destFolder))
				return;

			// --- Optionally rename files that contain the original folder name ---
			if (renameAssets)
				RenameMatchingAssets(destFolder, folderName, Path.GetFileName(destFolder));

			// Ping & select the new folder
			var folderObj = AssetDatabase.LoadAssetAtPath<Object>(destFolder);
			if (folderObj != null)
			{
				Selection.activeObject = folderObj;
				EditorGUIUtility.PingObject(folderObj);
			}
		}

		[MenuItem(StringConstants.CLONE_FOLDER, true, MenuPriority)]
		private static bool Validate() => true;

		/// <summary>
		/// Clones <paramref name="srcFolder"/> to <paramref name="destFolder"/>,
		/// rewiring internal cross-references and leaving external references intact.
		/// Shows a cancellable progress bar during the operation.
		/// </summary>
		/// <returns>True on success, false if the user cancelled during copy.</returns>
		public static bool Clone(string srcFolder, string destFolder)
		{
			try
			{
				var guidRemap = CopyFolderRecursive(srcFolder, destFolder);
				if (guidRemap == null)
					return false;

				RewireInternalReferences(destFolder, guidRemap);
			}
			finally
			{
				EditorUtility.ClearProgressBar();
			}

			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
			return true;
		}

		/// <summary>
		/// Renames every file asset inside <paramref name="folder"/> whose filename (without
		/// extension) contains <paramref name="oldName"/> as a substring, replacing it with
		/// <paramref name="newName"/>.  Sub-folder names are not changed.
		/// </summary>
		public static void RenameMatchingAssets(string folder, string oldName, string newName)
		{
			string[] guids = AssetDatabase.FindAssets("", new[] { folder });
			foreach (string guid in guids)
			{
				string path = AssetDatabase.GUIDToAssetPath(guid);
				if (AssetDatabase.IsValidFolder(path))
					continue;

				string baseName = Path.GetFileNameWithoutExtension(path);
				if (!baseName.Contains(oldName))
					continue;

				string renamed = baseName.Replace(oldName, newName);
				string error   = AssetDatabase.RenameAsset(path, renamed);
				if (!string.IsNullOrEmpty(error))
					Debug.LogWarning($"[CloneFolder] Could not rename '{path}': {error}");
			}

			AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
		}

		// -------------------------------------------------------------------------
		// Step 1– recursive copy; returns old→new GUID map, or null on cancel
		// -------------------------------------------------------------------------

		/// <summary>
		/// Copies <paramref name="src"/> into <paramref name="dest"/> recursively.
		/// Returns a map of original-GUID → clone-GUID for every copied file asset,
		/// or null if the user cancelled via the progress bar.
		/// </summary>
		private static Dictionary<string, string> CopyFolderRecursive(string src, string dest)
		{
			// Collect all file assets under source (FindAssets returns GUIDs)
			string[] guids = AssetDatabase.FindAssets("", new[] { src });

			var guidRemap = new Dictionary<string, string>(guids.Length);

			// First pass: create all necessary sub-folders top-down, then copy files
			int total = guids.Length;
			for (int i = 0; i < total; i++)
			{
				string origGuid  = guids[i];
				string origPath  = AssetDatabase.GUIDToAssetPath(origGuid);

				// Skip if this is a folder entry itself (FindAssets can return folder GUIDs)
				if (AssetDatabase.IsValidFolder(origPath))
					continue;

				// Build destination path by replacing the source prefix
				string clonePath = dest + origPath.Substring(src.Length);

				// Ensure parent directory exists
				string cloneDir = Path.GetDirectoryName(clonePath)?.Replace('\\', '/');
				if (!string.IsNullOrEmpty(cloneDir))
					EditorFileUtility.EnsureUnityFolderExists(cloneDir);

				if (EditorUtility.DisplayCancelableProgressBar(
					    "Clone Folder – Copying",
					    Path.GetFileName(origPath),
					    (float)i / total))
				{
					// User cancelled – undo by deleting whatever we created
					if (AssetDatabase.IsValidFolder(dest))
						AssetDatabase.DeleteAsset(dest);
					return null;
				}

				if (!AssetDatabase.CopyAsset(origPath, clonePath))
				{
					Debug.LogWarning($"[CloneFolder] Failed to copy '{origPath}' → '{clonePath}'");
					continue;
				}

				AssetDatabase.ImportAsset(clonePath, ImportAssetOptions.ForceSynchronousImport);

				string newGuid = AssetDatabase.AssetPathToGUID(clonePath);
				if (!string.IsNullOrEmpty(newGuid))
					guidRemap[origGuid] = newGuid;
			}

			return guidRemap;
		}

		// -------------------------------------------------------------------------
		// Step 2 – rewire internal ObjectReference properties
		// -------------------------------------------------------------------------

		private static void RewireInternalReferences(string destFolder, Dictionary<string, string> guidRemap)
		{
			string[] guids = AssetDatabase.FindAssets("", new[] { destFolder });
			int total = guids.Length;

			for (int i = 0; i < total; i++)
			{
				string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);

				// Skip folders and scripts (scripts are text; rewiring them would corrupt code)
				if (AssetDatabase.IsValidFolder(assetPath))
					continue;
				if (assetPath.EndsWith(".cs") || assetPath.EndsWith(".js") || assetPath.EndsWith(".shader"))
					continue;

				if (EditorUtility.DisplayCancelableProgressBar(
					    "Clone Folder – Rewiring",
					    Path.GetFileName(assetPath),
					    (float)i / total))
					break; // keep copies, just stop rewiring

				Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
				if (assets == null)
					continue;

				foreach (Object asset in assets)
				{
					if (asset == null)
						continue;

					var so = new SerializedObject(asset);
					SerializedProperty prop = so.GetIterator();
					bool changed = false;

					while (prop.Next(true))
					{
						if (prop.propertyType != SerializedPropertyType.ObjectReference)
							continue;

						Object referenced = prop.objectReferenceValue;
						if (referenced == null)
							continue;

						// Get the GUID of the referenced asset
						if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(referenced, out string refGuid, out long localId))
							continue;

						if (!guidRemap.TryGetValue(refGuid, out string newGuid))
							continue;

						// Load the cloned counterpart
						string newPath = AssetDatabase.GUIDToAssetPath(newGuid);
						Object remapped = FindAssetWithLocalId(newPath, referenced, localId);
						if (remapped == null)
							continue;

						prop.objectReferenceValue = remapped;
						changed = true;
					}

					if (changed)
					{
						so.ApplyModifiedPropertiesWithoutUndo();
						AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
					}
				}
			}
		}

		// -------------------------------------------------------------------------
		// Helpers
		// -------------------------------------------------------------------------

		/// <summary>
		/// Loads all assets at <paramref name="path"/> and returns the one whose
		/// local file ID matches <paramref name="localId"/>, or the main asset as fallback.
		/// </summary>
		private static Object FindAssetWithLocalId(string path, Object fallback, long localId)
		{
			Object[] all = AssetDatabase.LoadAllAssetsAtPath(path);
			if (all == null || all.Length == 0)
				return null;

			foreach (Object a in all)
			{
				if (a == null)
					continue;
				if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(a, out _, out long id) && id == localId)
					return a;
			}

			// Sub-asset not found by ID – return main asset (covers renamed sub-assets)
			return all[0];
		}

		/// <summary>Returns the asset path of the selected folder, or null if none.</summary>
		private static string GetSelectedFolderPath()
		{
			// activeObject is null when right-clicking the tree view without a prior left-click.
			// Fall through all available selection APIs as fallbacks.
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
