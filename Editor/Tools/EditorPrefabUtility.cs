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
			
			public string GetDumpString(int _numTabs = 0)
			{
				var result = new string('\t', _numTabs) + Asset.name + "\n";
				foreach (var variant in Variants)
					result += variant.GetDumpString(_numTabs + 1);
				return result;
			}
		}
		
		private static readonly List<GameObject> s_prefabs = new ();
		private static readonly Dictionary<Component, GameObject> s_components = new ();
		private static readonly List<PrefabVariantHierarchy> s_variants = new ();

		private static readonly Dictionary<GameObject, GameObject> s_variantBaseByGameObject = new ();
		
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
			CleanUp();
			BuildPrefabVariantHierarchy();
//			CreateVariants();
//			ChangeVariantReferences();
//			CleanUp();
		}

		private static void BuildPrefabVariantHierarchy()
		{
			var guids = AssetDatabase.FindAssets("t:prefab", new []{ BuiltinPrefabDir }).ToHashSet();
			HashSet<GameObject> done = new ();
			
			foreach ( var guid in guids )
			{
				var assetPath = AssetDatabase.GUIDToAssetPath( guid );
				var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
				var variantBase = PrefabUtility.GetCorrespondingObjectFromSource(prefab);
				s_variantBaseByGameObject.Add(prefab, variantBase);
			}

			foreach (var kv in s_variantBaseByGameObject)
			{
				if (kv.Value == null)
				{
					done.Add(kv.Key);
					s_variants.Add(new PrefabVariantHierarchy() {Asset = kv.Key});
				}
			}

			foreach (var gameObject in done)
				s_variantBaseByGameObject.Remove(gameObject);
			
			while (s_variantBaseByGameObject.Count > 0)
			{
				done.Clear();
				
				foreach (var kv in s_variantBaseByGameObject)
					TryInsert(s_variants, kv.Key, kv.Value, done);
				
				// Should not happen - avoid endless loop
				if (done.Count == 0)
					throw new Exception("Internal Exception: no bases found for prefabs");

				foreach (var gameObject in done)
					s_variantBaseByGameObject.Remove(gameObject);
			}
			
Debug.Log($"{GetVariantsDumpString()}");
		}
		
		private static bool TryInsert(List<PrefabVariantHierarchy> _list, GameObject _gameObject, GameObject _base, HashSet<GameObject> _done)
		{
			foreach (var variant in _list)
			{
				if (variant.Asset == _base)
				{
					_done.Add(_gameObject);
					variant.Variants.Add(new () {Asset = _gameObject, Parent = variant.Asset});
					return true;
				}
				
				if (TryInsert(variant.Variants, _gameObject, _base, _done))
					return true;
			}
			
			return false;
		}

		private static string GetVariantsDumpString()
		{
			string result = string.Empty;
			foreach (var variant in s_variants)
				result += variant.GetDumpString();
			
			return result;
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
			s_prefabs.Clear();
			s_components.Clear();
			s_variants.Clear();
			s_variantBaseByGameObject.Clear();
		}
	}
}