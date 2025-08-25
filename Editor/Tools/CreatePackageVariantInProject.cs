using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
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

			if (!absFolder.StartsWith(projectRoot, StringComparison.OrdinalIgnoreCase))
			{
				Debug.LogError("Folder must be inside this Unity project (under 'Assets/').");
				return;
			}

			// 2) Convert absolute folder path to relative Unity path ("Assets/...")
			var relFolder = absFolder.ToLogicalPath();

			// 3) Ensure the folder exists (create if needed)
			EditorFileUtility.EnsureFolderExists(absFolder);

			// 4) Create prefab variant for each selected prefab or from selected folders
			foreach (var prefab in EnumerateSourcePrefabsFromSelection())
			{
				CreateVariant(prefab, ( _, defaultName ) => Path.Combine(relFolder, defaultName).Replace("\\", "/"));
			}
		}

		// --------------------------------------------------------------------
		// 1) Prompt for each file
		// --------------------------------------------------------------------
		[MenuItem(SelectEachPath, false, SelectEachPriority)]
		private static void SelectEachPathExec( MenuCommand cmd )
		{
			foreach (var prefab in EnumerateSourcePrefabsFromSelection())
			{
				CreateVariant(prefab, ( _, defaultName ) =>
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
			foreach (var prefab in EnumerateSourcePrefabsFromSelection())
			{
				CreateVariant(prefab, ( _, defaultName ) => $"Assets/{defaultName}");
			}
		}

		// --------------------------------------------------------------------
		// 3) Mirror Package Hierarchy
		// --------------------------------------------------------------------
		[MenuItem(MirrorHierarchy, false, MirrorHierarchyPriority)]
		private static void MirrorHierarchyExec( MenuCommand cmd )
		{
			foreach (var prefab in EnumerateSourcePrefabsFromSelection())
			{
				CreateVariant(prefab, ( src, defaultName ) =>
				{
					var sourcePath = AssetDatabase.GetAssetPath(src);           // "Packages/com.foo.bar/Prefabs/My.prefab"
					var relative = sourcePath.Substring("Packages/".Length);    // "com.foo.bar/Prefabs/My.prefab"
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
				Debug.LogError($"!Can not create variant of '{sourcePath}'; needs to reside in 'Packages'.");
				return;
			}

			var defaultName = $"{prefab.name} Variant.prefab";
			var targetPath = getTargetPath(prefab, defaultName);

			if (string.IsNullOrEmpty(targetPath))
				return; // user canceled

			// Ensure Unity knows the target folder (in case it was created externally)
			var targetFolder = Path.GetDirectoryName(targetPath)?.Replace("\\", "/") ?? "Assets";
			if (!AssetDatabase.IsValidFolder(targetFolder))
			{
				AssetDatabase.Refresh();
				EditorApplication.delayCall += () => CreateVariant(prefab, getTargetPath);
				return;
			}

			targetPath = AssetDatabase.GenerateUniqueAssetPath(targetPath);

			// 1) Instantiate prefab temporarily
			var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;

			try
			{
				// 2) Save instance as prefab variant
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
				// 3) Clean up
				Object.DestroyImmediate(instance);
			}
		}

		// --------------------------------------------------------------------
		// New: selection-to-prefabs expansion (supports folders, recursive)
		// --------------------------------------------------------------------
		private static IEnumerable<GameObject> EnumerateSourcePrefabsFromSelection()
		{
			// 1) Directly selected prefab assets
			foreach (var go in Selection.objects.OfType<GameObject>())
			{
				if (IsEligiblePrefabAsset(go))
					yield return go;
			}

			// 2) For any selected folder, collect all prefab assets recursively
			foreach (var obj in Selection.objects)
			{
				var path = AssetDatabase.GetAssetPath(obj);
				if (string.IsNullOrEmpty(path))
					continue;

				if (!AssetDatabase.IsValidFolder(path))
					continue;

				foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { path }))
				{
					var assetPath = AssetDatabase.GUIDToAssetPath(guid);
					var go = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
					if (go != null && IsEligiblePrefabAsset(go))
						yield return go;
				}
			}
		}

		private static bool IsEligiblePrefabAsset(GameObject prefab)
		{
			var path = AssetDatabase.GetAssetPath(prefab);
			var type = PrefabUtility.GetPrefabAssetType(prefab);

			// We accept Regular and Variant (skip Model prefabs etc.)
			bool typeOk = (type == PrefabAssetType.Regular || type == PrefabAssetType.Variant);

			// Only from Packages/ (tool contract)
			bool pathOk = path.StartsWith("Packages/", StringComparison.OrdinalIgnoreCase);

			return typeOk && pathOk;
		}

		private static bool ValidateSelection()
		{
			// Any eligible prefab directly selected?
			if (Selection.objects.OfType<GameObject>().Any(IsEligiblePrefabAsset))
				return true;

			// Any selected folder that contains at least one eligible prefab?
			foreach (var obj in Selection.objects)
			{
				var path = AssetDatabase.GetAssetPath(obj);
				if (string.IsNullOrEmpty(path) || !AssetDatabase.IsValidFolder(path))
					continue;

				// Cheap probe: stop at first hit
				foreach (var guid in AssetDatabase.FindAssets("t:Prefab", new[] { path }))
				{
					var assetPath = AssetDatabase.GUIDToAssetPath(guid);
					var go = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
					if (go != null && IsEligiblePrefabAsset(go))
						return true;
				}
			}

			return false;
		}
	}
}
