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
		// 0) All variants into one common folder
		// --------------------------------------------------------------------
		[MenuItem(SelectCommonPath, false, SelectCommonPriority)]
		private static void SelectCommonPathExec(MenuCommand cmd)
		{
			var absFolder = EditorUtility.OpenFolderPanel(
				"Choose target folder for Prefab Variants",
				Application.dataPath,
				"");

			if (string.IsNullOrEmpty(absFolder))
				return;

			absFolder = absFolder.Replace("\\", "/");

			var projectRoot = GetProjectRoot();
			if (!absFolder.StartsWith(projectRoot, StringComparison.OrdinalIgnoreCase))
			{
				UiLog.LogError("Folder must be inside this Unity project (under 'Assets/').");
				return;
			}

			var relFolder = absFolder.ToLogicalPath(); // your helper
			EditorFileUtility.EnsureFolderExists(absFolder);

			var sources = EnumerateSourcePrefabsFromSelection().ToList();
			if (sources.Count == 0)
			{
				UiLog.LogWarning("No eligible prefabs found in the selection.");
				return;
			}

			string lastCreated = null;
			foreach (var prefab in sources)
			{
				CreateVariant(prefab, (_, defaultName) =>
					Path.Combine(relFolder, defaultName).Replace("\\", "/"),
					onCreatedPath => lastCreated = onCreatedPath);
			}

			// Optional: danach EIN Objekt selektieren/pingen
			if (!string.IsNullOrEmpty(lastCreated))
			{
				var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(lastCreated);
				if (obj != null)
				{
					Selection.activeObject = obj;
					EditorGUIUtility.PingObject(obj);
				}
			}
		}

		// --------------------------------------------------------------------
		// 1) Prompt for each file
		// --------------------------------------------------------------------
		[MenuItem(SelectEachPath, false, SelectEachPriority)]
		private static void SelectEachPathExec(MenuCommand cmd)
		{
			var sources = EnumerateSourcePrefabsFromSelection().ToList();
			foreach (var prefab in sources)
			{
				CreateVariant(prefab, (_, defaultName) =>
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
			var sources = EnumerateSourcePrefabsFromSelection().ToList();
			foreach (var prefab in sources)
			{
				CreateVariant(prefab, (_, defaultName) => $"Assets/{defaultName}");
			}
		}

		// --------------------------------------------------------------------
		// 3) Mirror Package Hierarchy
		// --------------------------------------------------------------------
		[MenuItem(MirrorHierarchy, false, MirrorHierarchyPriority)]
		private static void MirrorHierarchyExec(MenuCommand cmd)
		{
			var sources = EnumerateSourcePrefabsFromSelection().ToList();
			foreach (var prefab in sources)
			{
				CreateVariant(prefab, (src, defaultName) =>
				{
					var sourcePath = AssetDatabase.GetAssetPath(src);           // "Packages/com.foo.bar/Prefabs/My.prefab"
					var relative = sourcePath.Substring("Packages/".Length);    // "com.foo.bar/Prefabs/My.prefab"
					var relFolder = Path.GetDirectoryName(relative) ?? string.Empty;

					var destFolder = Path.Combine("Assets/PackageVariants", relFolder).Replace("\\", "/");

					// Ensure folder exists (absolute path)
					var absolute = Path.GetFullPath(Path.Combine(GetProjectRoot(), destFolder));
					EditorFileUtility.EnsureFolderExists(absolute);

					return Path.Combine(destFolder, defaultName).Replace("\\", "/");
				});
			}
		}

		// --------------------------------------------------------------------
		// Validation
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
			Func<GameObject, string, string> getTargetPath,
			Action<string> onCreated = null)
		{
			var sourcePath = AssetDatabase.GetAssetPath(prefab);
			if (!sourcePath.StartsWith("packages/", StringComparison.OrdinalIgnoreCase))
			{
				UiLog.LogError($"!Can not create variant of '{sourcePath}'; needs to reside in 'Packages'.");
				return;
			}

			var defaultName = $"{prefab.name} Variant.prefab";
			var targetPath = getTargetPath(prefab, defaultName);

			if (string.IsNullOrEmpty(targetPath))
				return; // user canceled

			var targetFolder = Path.GetDirectoryName(targetPath)?.Replace("\\", "/") ?? "Assets";
			if (!AssetDatabase.IsValidFolder(targetFolder))
			{
				// Might be a just-created folder via file dialog; force import
				AssetDatabase.Refresh();
				// Defer this single item (NOT the whole batch)
				EditorApplication.delayCall += () => CreateVariant(prefab, getTargetPath, onCreated);
				return;
			}

			targetPath = AssetDatabase.GenerateUniqueAssetPath(targetPath);

			var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
			try
			{
				PrefabUtility.SaveAsPrefabAsset(instance, targetPath, out bool success);
				if (success)
				{
					UiLog.LogInternal($"Prefab Variant saved to: {targetPath}");
					onCreated?.Invoke(targetPath);
				}
				else
				{
					UiLog.LogError($"Failed to save Prefab Variant to: {targetPath}");
				}
			}
			finally
			{
				Object.DestroyImmediate(instance);
			}
		}

		// --------------------------------------------------------------------
		// Selection expansion (folders + direct prefabs), robust for Packages/
		// --------------------------------------------------------------------
		private static IEnumerable<GameObject> EnumerateSourcePrefabsFromSelection()
		{
			// Snapshot selection ONCE to avoid live changes influencing enumeration
			var selected = Selection.objects != null ? Selection.objects.ToArray() : Array.Empty<UnityEngine.Object>();

			// 1) Directly selected prefab assets
			foreach (var go in selected.OfType<GameObject>())
			{
				if (IsEligiblePrefabAsset(go))
					yield return go;
			}

			// 2) Selected folders (Assets/ or Packages/)
			foreach (var obj in selected)
			{
				var path = AssetDatabase.GetAssetPath(obj);
				if (string.IsNullOrEmpty(path))
					continue;

				if (!IsFolderPath(path))
					continue;

				foreach (var go in EnumeratePrefabsInFolder(path))
				{
					if (go != null && IsEligiblePrefabAsset(go))
						yield return go;
				}
			}
		}

		private static IEnumerable<GameObject> EnumeratePrefabsInFolder(string folderRelPath)
		{
			// Try AssetDatabase.FindAssets in that folder (works for Assets/, and in modernen Unitys auch für Packages/)
			IEnumerable<string> guids = null;
			try
			{
				guids = AssetDatabase.FindAssets("t:Prefab", new[] { folderRelPath });
			}
			catch
			{
				guids = null;
			}

			if (guids != null)
			{
				foreach (var guid in guids)
				{
					var assetPath = AssetDatabase.GUIDToAssetPath(guid);
					var go = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
					if (go != null)
						yield return go;
				}
				yield break;
			}

			// Fallback: manual file scan
			var abs = ToAbsolutePath(folderRelPath);
			if (!string.IsNullOrEmpty(abs) && Directory.Exists(abs))
			{
				foreach (var file in Directory.EnumerateFiles(abs, "*.prefab", SearchOption.AllDirectories))
				{
					var rel = ToProjectRelative(file);
					var go = AssetDatabase.LoadAssetAtPath<GameObject>(rel);
					if (go != null)
						yield return go;
				}
			}
		}

		private static bool IsEligiblePrefabAsset(GameObject prefab)
		{
			var path = AssetDatabase.GetAssetPath(prefab);
			var type = PrefabUtility.GetPrefabAssetType(prefab);
			bool typeOk = (type == PrefabAssetType.Regular || type == PrefabAssetType.Variant);
			bool pathOk = path.StartsWith("Packages/", StringComparison.OrdinalIgnoreCase);
			return typeOk && pathOk;
		}

		private static bool ValidateSelection()
		{
			var selected = Selection.objects != null ? Selection.objects.ToArray() : Array.Empty<UnityEngine.Object>();

			// Any eligible prefab directly selected?
			if (selected.OfType<GameObject>().Any(IsEligiblePrefabAsset))
				return true;

			// Any selected folder (Assets/ or Packages/) that contains at least one eligible prefab?
			foreach (var obj in selected)
			{
				var path = AssetDatabase.GetAssetPath(obj);
				if (string.IsNullOrEmpty(path) || !IsFolderPath(path))
					continue;

				// Cheap probe: stop at first eligible hit
				foreach (var go in EnumeratePrefabsInFolder(path))
				{
					if (go != null && IsEligiblePrefabAsset(go))
						return true;
				}
			}

			return false;
		}

		// --------------------------------------------------------------------
		// Path helpers (Assets/ + Packages/)
		// --------------------------------------------------------------------
		private static string GetProjectRoot()
		{
			return Path.GetFullPath(Path.Combine(Application.dataPath, "..")).Replace("\\", "/");
		}

		private static bool IsFolderPath(string relPath)
		{
			// Accept Unity's folder check for Assets/, and manual check for Packages/
			if (AssetDatabase.IsValidFolder(relPath))
				return true;

			if (relPath.StartsWith("Packages/", StringComparison.OrdinalIgnoreCase))
			{
				var abs = ToAbsolutePath(relPath);
				return !string.IsNullOrEmpty(abs) && Directory.Exists(abs);
			}

			return false;
		}

		private static string ToAbsolutePath(string relPath)
		{
			if (string.IsNullOrEmpty(relPath))
				return null;

			var root = GetProjectRoot();
			var abs = Path.GetFullPath(Path.Combine(root, relPath)).Replace("\\", "/");
			return abs;
		}

		private static string ToProjectRelative(string absPath)
		{
			absPath = absPath.Replace("\\", "/");
			var root = GetProjectRoot();

			if (absPath.StartsWith(root + "/", StringComparison.OrdinalIgnoreCase))
				return absPath.Substring(root.Length + 1);

			return absPath;
		}
	}
}
