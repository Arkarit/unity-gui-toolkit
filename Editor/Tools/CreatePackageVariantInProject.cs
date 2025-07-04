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
		
		private const string SelectEachPath      = Prefix + "Select each Path";
		private const int    SelectEachPriority  = -800;

		private const string FlatInAssets        = Prefix + "Flat in Assets";
		private const int    FlatInAssetsPriority = -810;

		private const string MirrorHierarchy     = Prefix + "Mirror Package Hierarchy";
		private const int    MirrorHierarchyPriority = -820;

		// --------------------------------------------------------------------
		// 1) Prompt for each file
		// --------------------------------------------------------------------
		[MenuItem(SelectEachPath, false, SelectEachPriority)]
		private static void SelectEachPathExec(MenuCommand cmd)
		{
			foreach (var obj in Selection.objects.OfType<GameObject>())
			{
				CreateVariant(obj, (_, defaultName) =>
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
		private static void FlatInAssetsExec(MenuCommand cmd)
		{
			foreach (var obj in Selection.objects.OfType<GameObject>())
			{
				CreateVariant(obj, (_, defaultName) =>
				{
					// Always create directly under Assets/, ensure unique name.
					var assetPath = $"Assets/{defaultName}";
					return AssetDatabase.GenerateUniqueAssetPath(assetPath);
				});
			}
		}

		// --------------------------------------------------------------------
		// 3) Mirror Package Hierarchy
		// --------------------------------------------------------------------
		[MenuItem(MirrorHierarchy, false, MirrorHierarchyPriority)]
		private static void MirrorHierarchyExec(MenuCommand cmd)
		{
			foreach (var obj in Selection.objects.OfType<GameObject>())
			{
				CreateVariant(obj, (src, defaultName) =>
				{
					var sourcePath = AssetDatabase.GetAssetPath(src);              // "Packages/com.foo.bar/Prefabs/My.prefab"
					var relative   = sourcePath.Substring("Packages/".Length);     // "com.foo.bar/Prefabs/My.prefab"
					var relFolder  = Path.GetDirectoryName(relative) ?? string.Empty;

					// Target: Assets/PackageVariants/com.foo.bar/Prefabs/
					var destFolder = Path.Combine("Assets/PackageVariants", relFolder).Replace("\\", "/");

					// Ensure folder exists (absolute system path required)
					var absolute = Path.GetFullPath(Path.Combine(Application.dataPath, "..", destFolder));
					EditorFileUtility.EnsureFolderExists(absolute);

					var destPath = Path.Combine(destFolder, defaultName).Replace("\\", "/");
					return AssetDatabase.GenerateUniqueAssetPath(destPath);
				});
			}
		}

		// --------------------------------------------------------------------
		// Validation (shared by all three menu items)
		// --------------------------------------------------------------------
		[MenuItem(SelectEachPath, true, SelectEachPriority)]
		[MenuItem(FlatInAssets,  true, FlatInAssetsPriority)]
		[MenuItem(MirrorHierarchy, true, MirrorHierarchyPriority)]
		private static bool MenuValidate() => ValidateSelection();

		// --------------------------------------------------------------------
		// Core helper
		// --------------------------------------------------------------------
		private static void CreateVariant(GameObject prefab,
			Func<GameObject, string, string> getTargetPath)
		{
			var sourcePath = AssetDatabase.GetAssetPath(prefab);
			if (!sourcePath.StartsWith("Packages/"))
				return;

			var defaultName = $"{prefab.name} Variant.prefab";
			var targetPath  = getTargetPath(prefab, defaultName);

			if (string.IsNullOrEmpty(targetPath))
				return; // user cancelled

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

		private static bool ValidateSelection()
		{
			return Selection.objects.OfType<GameObject>().Any(obj =>
			{
				var path = AssetDatabase.GetAssetPath(obj);
				var type = PrefabUtility.GetPrefabAssetType(obj);
				return path.StartsWith("Packages/") &&
				       (type == PrefabAssetType.Regular || type == PrefabAssetType.Variant);
			});
		}
	}
}
