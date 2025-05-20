using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Editor
{
	public static class EditorPrefabUtility
	{
		public const string PrefabFolder = "Prefabs/";
		
		private static readonly List<GameObject> s_Prefabs = new ();
		private static readonly Dictionary<Component, GameObject> s_Components = new ();
		private static string s_TargetDir;
		
		public static string BuiltinPrefabDir
		{
			get
			{
				string rootProjectDir = UiToolkitConfiguration.Instance.GetUiToolkitRootProjectDir();
				return rootProjectDir + PrefabFolder;
			}
		}
		
		public static void CreatePrefabVariants(string _targetDir)
		{
			s_TargetDir = _targetDir;
			CreateVariants();
			ChangeVariantReferences();
			CleanUp();
		}

		private static void CreateVariants()
		{
			EditorFileUtility.EnsureFolderExists(s_TargetDir);
			var guids = AssetDatabase.FindAssets("t:prefab", new []{ BuiltinPrefabDir });
			foreach ( var guid in guids )
			{
				var assetPath = AssetDatabase.GUIDToAssetPath( guid );
			
Debug.Log($"{assetPath}:\n{DumpPrefab(assetPath)}");
			}
		}

		public static string DumpPrefab(string _assetPath)
		{
			var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(_assetPath);
			return DumpPrefab(prefab);
		}

		public static string DumpPrefab(GameObject _prefab)
		{
			string result = $"prefab.name:{_prefab.name}\n";
			result += $"variant hierarchy:{GetPrefabVariantNamesHierarchy(_prefab)}\n";

			return result;
		}
		
		public static List<GameObject> GetPrefabVariantHierarchy(GameObject _prefab)
		{
			List<GameObject> result = new () { _prefab };
			var variantBase = _prefab;
			for(;;)
			{
				variantBase = PrefabUtility.GetCorrespondingObjectFromSource(variantBase);
				if (variantBase == null)
					break;
				
				result.Add(variantBase);
			}

			return result;
		}
		
		public static string GetPrefabVariantNamesHierarchy(GameObject _prefab)
		{
			var hierarchy = GetPrefabVariantHierarchy(_prefab);
			string result = string.Empty;
			
			for (int i= 0; i < hierarchy.Count; i++)
			{
				var variant = hierarchy[i];
				result += variant.name;
				if (i < hierarchy.Count - 1)
					result += "/";
			}
			
			return result;
		}

		private static void ChangeVariantReferences()
		{
		}

		private static void CleanUp()
		{
			s_Prefabs.Clear();
			s_Components.Clear();
		}
	}
}