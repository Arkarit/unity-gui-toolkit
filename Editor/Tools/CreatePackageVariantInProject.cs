using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GuiToolkit.Editor
{
	public static class CreatePackageVariantInProject
	{
		private const string Prefix = "Assets/Create Variant/";
		private const string SelectEachPath     = Prefix + "Select each Path";
		private const int    SelectEachPathPriority = -800;

		/// <summary>
		/// Creates a prefab variant from a selected prefab inside a package folder.
		/// The variant will be saved inside the 'Assets' folder.
		/// </summary>
		[MenuItem(SelectEachPath, false, SelectEachPathPriority)]
		public static void SelectEachPathExec(MenuCommand cmd)
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

		/// <summary>
		/// Enables the menu item only if one or more selected objects are regular prefabs inside a package.
		/// </summary>
		[MenuItem(SelectEachPath, true, SelectEachPathPriority)]
		public static bool CreatePackageVariantValidate() => Validate();

		private static void CreateVariant(GameObject obj, Func<GameObject, string, string> _getPath)
		{
			var sourcePath = AssetDatabase.GetAssetPath(obj);
			if (!sourcePath.StartsWith("Packages/"))
				return;

			// Ask user for destination path
			var defaultName = $"{obj.name} Variant.prefab";
			var targetPath = _getPath(obj, defaultName);

			
			if (string.IsNullOrEmpty(targetPath))
				return; // User cancelled

			// 1) Instantiate prefab temporarily
			var instance = PrefabUtility.InstantiatePrefab(obj) as GameObject;

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
				// 3) Clean up temporary instance
				Object.DestroyImmediate(instance);
			}
			
		}
		
		private static bool Validate()
		{
			return Selection.objects.OfType<GameObject>().Any(obj =>
			{
				var path = AssetDatabase.GetAssetPath(obj);
				return path.StartsWith("Packages/") &&
				       (PrefabUtility.GetPrefabAssetType(obj) == PrefabAssetType.Regular ||
				        PrefabUtility.GetPrefabAssetType(obj) == PrefabAssetType.Variant);
			});
		}
	}
}
