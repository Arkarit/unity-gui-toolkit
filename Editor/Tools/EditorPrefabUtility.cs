using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Editor
{
	public static class EditorPrefabUtility
	{
		public const string PrefabFolder = "Prefabs/";
		
		private class PrefabVariantHierarchy
		{
			public GameObject Asset = null;
			public GameObject Parent = null;
			public readonly List<PrefabVariantHierarchy> Variants = new ();
		}
		
		private static readonly List<GameObject> s_Prefabs = new ();
		private static readonly Dictionary<Component, GameObject> s_Components = new ();
		private static readonly List<PrefabVariantHierarchy> s_Variants = new ();

		private static readonly Dictionary<NullObject<GameObject>, HashSet<GameObject>> s_VariantsByGameObject = new ();
		
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
			BuildPrefabVariantHierarchy();
//			CreateVariants();
//			ChangeVariantReferences();
//			CleanUp();
		}

		private static void BuildPrefabVariantHierarchy()
		{
			var guids = AssetDatabase.FindAssets("t:prefab", new []{ BuiltinPrefabDir }).ToHashSet();
			HashSet<GameObject> variants;
			
			foreach ( var guid in guids )
			{
				var assetPath = AssetDatabase.GUIDToAssetPath( guid );
				var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
				var variantBase = PrefabUtility.GetCorrespondingObjectFromSource(prefab);
				
				if (s_VariantsByGameObject.TryGetValue(variantBase, out variants))
					variants.Add(prefab);
				else
					s_VariantsByGameObject.Add(variantBase, new () {prefab});
			}

			if (!s_VariantsByGameObject.TryGetValue(null, out variants))
				throw new Exception("No base prefabs found!");
			
			s_VariantsByGameObject.Remove(null);

			foreach (var gameObject in variants)
				s_Variants.Add(new PrefabVariantHierarchy() {Asset = gameObject});

foreach (var prefabVariantHierarchy in s_Variants)
Debug.Log(prefabVariantHierarchy.Asset.name);

			while (s_VariantsByGameObject.Count > 0)
			{
				
			}
			
//Debug.Log("---\n\n");			
//			foreach (var kv in s_VariantsByGameObject)
//			{
//				var prefab = kv.Key;
//				var v = kv.Value;
//string s = $"prefab:{(prefab.IsNull ? "<null>" : prefab.Item.name)}:";
//foreach (var variant in v)
//{
//	if (variant != null)
//		s += " " + variant.name;
//}
//Debug.Log(s);
//			}
		}
		
		private static void Bla()
		{
			
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
		public static List<GameObject> GetPrefabVariantHierarchy(string _assetPath)
		{
			var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(_assetPath);
			return GetPrefabVariantHierarchy(prefab);
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
			s_Variants.Clear();
		}
	}
}