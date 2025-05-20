using System;
using System.Collections.Generic;
using System.IO;
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
			public GameObject Clone = null;
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
		private static readonly List<PrefabVariantHierarchy> s_variants = new ();

		private static readonly Dictionary<GameObject, GameObject> s_variantBaseByGameObject = new ();
		private static readonly Dictionary<GameObject, GameObject> s_clonedByVariant = new ();
		
		private static string s_targetDir;
		
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
			s_targetDir = _targetDir;
			CleanUp();
			BuildPrefabVariantHierarchy();
			Clone();
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
		
		private static void Clone() => Clone(s_variants);
		private static void Clone(List<PrefabVariantHierarchy> _list)
		{
			foreach (var variant in _list)
				Clone(variant);
		}

		private static void Clone(PrefabVariantHierarchy _variant)
		{
			// DoClone
			var asset = _variant.Asset;
			var assetPath = AssetDatabase.GetAssetPath(asset);
			var filename = Path.GetFileNameWithoutExtension(assetPath);
			var extension = Path.GetExtension(assetPath);
			var newAssetPath = $"{s_targetDir}/{filename}{extension}";
			var variantName = $"{filename}Variant{extension}";
			var variantPath = $"{s_targetDir}/{variantName}";
			
			if (File.Exists(variantPath))
			{
				var existing = AssetDatabase.LoadAssetAtPath<GameObject>(variantPath);
				_variant.Clone = existing;
				return;
			}
			
			var prefab = PrefabUtility.InstantiatePrefab(asset) as GameObject;
			if (!prefab)
				return;
			
			EditorFileUtility.EnsureFolderExists(s_targetDir);
			var variant = PrefabUtility.SaveAsPrefabAsset(prefab, newAssetPath);
			_variant.Clone = variant;
			
			//TODO: Fix variant base and references
			
			AssetDatabase.RenameAsset(newAssetPath, variantName);
			
			prefab.SafeDestroy();
			
			
			foreach (var v in _variant.Variants)
				Clone(v);
		}

		private static void CleanUp()
		{
			s_prefabs.Clear();
			s_variants.Clear();
			s_variantBaseByGameObject.Clear();
			s_clonedByVariant.Clear();
		}
	}
}