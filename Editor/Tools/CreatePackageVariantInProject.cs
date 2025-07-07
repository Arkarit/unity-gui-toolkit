using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GuiToolkit.Editor
{
	public static class CreatePackageVariantInProject
	{
		// --------------------------------------------------------------------
		// Menu labels & priorities
		// --------------------------------------------------------------------
		private const string Prefix = "Assets/Create Variant/";

		private const string SelectCommonPath = Prefix + "Select common Path";
		private const int SelectCommonPriority = -790;

		private const string SelectEachPath = Prefix + "Select each Path";
		private const int SelectEachPriority = -800;

		private const string FlatInAssets = Prefix + "Flat in Assets";
		private const int FlatInAssetsPriority = -810;

		private const string MirrorHierarchy = Prefix + "Mirror Package Hierarchy (in folder 'Assets\\PackageVariants\\')";
		private const int MirrorHierarchyPriority = -820;

		// --------------------------------------------------------------------
		// 0) All variants into one common folder (new)
		// --------------------------------------------------------------------
		[MenuItem(SelectCommonPath, false, SelectCommonPriority)]
		private static void SelectCommonPathExec( MenuCommand cmd )
		{
			// 1) Ask user to select a target folder (must be inside the project)
			var absFolder = EditorUtility.OpenFolderPanel(
				"Choose target folder for Prefab Variants",
				Application.dataPath, // Start browsing from Assets/
				"");

			if (string.IsNullOrEmpty(absFolder))
				return; // User cancelled

			absFolder = absFolder.Replace("\\", "/");
			var projectRoot = Path.GetFullPath(Path.Combine(Application.dataPath, ".."))
								  .Replace("\\", "/");

			if (!absFolder.StartsWith(projectRoot))
			{
				Debug.LogError("Folder must be inside this Unity project (under 'Assets/').");
				return;
			}

			// 2) Convert absolute folder path to relative Unity path ("Assets/...")
			var relFolder = absFolder.ToLogicalPath();

			// 3) Ensure the folder exists (create if needed)
			EditorFileUtility.EnsureFolderExists(absFolder);

			// 4) Create prefab variant for each selected object into the same folder
			foreach (var obj in Selection.objects.OfType<GameObject>())
			{
				CreateVariant(obj, ( _, defaultName ) => Path.Combine(relFolder, defaultName).Replace("\\", "/"));
			}
		}

		// --------------------------------------------------------------------
		// 1) Prompt for each file
		// --------------------------------------------------------------------
		[MenuItem(SelectEachPath, false, SelectEachPriority)]
		private static void SelectEachPathExec( MenuCommand cmd )
		{
			foreach (var obj in Selection.objects.OfType<GameObject>())
			{
				CreateVariant(obj, ( _, defaultName ) =>
				{
					return EditorUtility.SaveFilePanelInProject(
						"Save Prefab Variant",
						defaultName,
						"prefab",
						"Choose location for the Prefab Variant",
						"Assets");
				});
			}
		}

		// --------------------------------------------------------------------
		// 2) Flat in Assets
		// --------------------------------------------------------------------
		[MenuItem(FlatInAssets, false, FlatInAssetsPriority)]
		private static void FlatInAssetsExec( MenuCommand cmd )
		{
			foreach (var obj in Selection.objects.OfType<GameObject>())
			{
				CreateVariant(obj, ( _, defaultName ) => $"Assets/{defaultName}");
			}
		}

		// --------------------------------------------------------------------
		// 3) Mirror Package Hierarchy
		// --------------------------------------------------------------------
		[MenuItem(MirrorHierarchy, false, MirrorHierarchyPriority)]
		private static void MirrorHierarchyExec( MenuCommand cmd )
		{
			foreach (var obj in Selection.objects.OfType<GameObject>())
			{
				CreateVariant(obj, ( src, defaultName ) =>
				{
					var sourcePath = AssetDatabase.GetAssetPath(src);              // "Packages/com.foo.bar/Prefabs/My.prefab"
					var relative = sourcePath.Substring("Packages/".Length);     // "com.foo.bar/Prefabs/My.prefab"
					var relFolder = Path.GetDirectoryName(relative) ?? string.Empty;

					// Target: Assets/PackageVariants/com.foo.bar/Prefabs/
					var destFolder = Path.Combine("Assets/PackageVariants", relFolder).Replace("\\", "/");

					// Ensure folder exists (absolute system path required)
					var absolute = Path.GetFullPath(Path.Combine(Application.dataPath, "..", destFolder));
					EditorFileUtility.EnsureFolderExists(absolute);

					return Path.Combine(destFolder, defaultName).Replace("\\", "/");
				});
			}
		}

		// --------------------------------------------------------------------
		// Validation (shared by all menu items)
		// --------------------------------------------------------------------
		[MenuItem(SelectCommonPath, true, SelectCommonPriority)]
		[MenuItem(SelectEachPath, true, SelectEachPriority)]
		[MenuItem(FlatInAssets, true, FlatInAssetsPriority)]
		[MenuItem(MirrorHierarchy, true, MirrorHierarchyPriority)]
		private static bool MenuValidate() => ValidateSelection();

		// --------------------------------------------------------------------
		// Core helper
		// --------------------------------------------------------------------
		private static void CreateVariant(
			GameObject prefab,
			Func<GameObject, string, string> getTargetPath )
		{
			var sourcePath = AssetDatabase.GetAssetPath(prefab);
			if (!sourcePath.StartsWith("packages/", StringComparison.OrdinalIgnoreCase))
			{
				Debug.LogError($"!Can not create variant of '{sourcePath}'; needs to reside in 'Packages'");
				return;
			}

			var defaultName = $"{prefab.name} Variant.prefab";
			var targetPath = getTargetPath(prefab, defaultName);

			if (string.IsNullOrEmpty(targetPath))
				return; // user canceled

			// ----------------------------------------------------------------
			// Ensure Unity knows the target folder (user might have created it
			// via the file dialog -> not yet imported).
			// ----------------------------------------------------------------
			var targetFolder = Path.GetDirectoryName(targetPath)?.Replace("\\", "/") ?? "Assets";
			if (!AssetDatabase.IsValidFolder(targetFolder))
			{
				// Force AssetDatabase to import the new folder
				AssetDatabase.Refresh();

				// Defer the actual save to the next editor tick
				EditorApplication.delayCall += () => CreateVariant(prefab, getTargetPath);
				return;
			}
			
			targetPath = AssetDatabase.GenerateUniqueAssetPath(targetPath);

			// ----------------------------------------------------------------
			// 1) Instantiate prefab temporarily
			// ----------------------------------------------------------------
			var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;

			try
			{
				// ----------------------------------------------------------------
				// 2) Save instance as prefab variant
				// ----------------------------------------------------------------
				PrefabUtility.SaveAsPrefabAsset(instance, targetPath, out bool success);

				if (success)
				{
					Debug.Log($"Prefab Variant saved to: {targetPath}");
					Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(targetPath);
					EditorGUIUtility.PingObject(Selection.activeObject);
				}
				else
				{
					Debug.LogError($"Failed to save Prefab Variant to: {targetPath}");
				}
			}
			finally
			{
				// ----------------------------------------------------------------
				// 3) Clean up
				// ----------------------------------------------------------------
				Object.DestroyImmediate(instance);
			}
		}

		private static bool ValidateSelection()
		{
			return Selection.objects.OfType<GameObject>().Any(obj =>
			{
				var path = AssetDatabase.GetAssetPath(obj);
				var type = PrefabUtility.GetPrefabAssetType(obj);
				bool pathValid = path.StartsWith("packages/", StringComparison.OrdinalIgnoreCase);
				return pathValid && (type == PrefabAssetType.Regular || type == PrefabAssetType.Variant);
			});
		}
	}
}
