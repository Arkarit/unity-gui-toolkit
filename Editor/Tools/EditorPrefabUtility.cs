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
		
		private class VariantRecord
		{
			public GameObject Asset = null;
			public VariantRecord Base = null;
			
			public GameObject Clone = null;
			
			public readonly List<VariantRecord> VariantRecordsBasedOnThis = new ();
			
			public string GetDumpString(int _numTabs = 0)
			{
				var result = new string('\t', _numTabs) + Asset.name + "\n";
				foreach (var record in VariantRecordsBasedOnThis)
					result += record.GetDumpString(_numTabs + 1);
				
				return result;
			}
		}
		
		private static readonly List<VariantRecord> s_variantRecords = new ();
		private static readonly Dictionary<GameObject, GameObject> s_baseByPrefab = new ();
		private static readonly Dictionary<GameObject, GameObject> s_clonedByPrefab = new ();
		
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
				s_baseByPrefab.Add(prefab, variantBase);
			}

			foreach (var kv in s_baseByPrefab)
			{
				if (kv.Value == null)
				{
					done.Add(kv.Key);
					s_variantRecords.Add(new VariantRecord() {Asset = kv.Key});
				}
			}

			foreach (var gameObject in done)
				s_baseByPrefab.Remove(gameObject);
			
			while (s_baseByPrefab.Count > 0)
			{
				done.Clear();
				
				foreach (var kv in s_baseByPrefab)
					TryInsertRecord(s_variantRecords, kv.Key, kv.Value, done);
				
				// Should not happen - avoid endless loop
				if (done.Count == 0)
					throw new Exception("Internal Exception: no bases found for prefabs");

				foreach (var go in done)
					s_baseByPrefab.Remove(go);
			}
			
Debug.Log($"{GetVariantRecordsDumpString()}");
		}
		
		private static bool TryInsertRecord(List<VariantRecord> _list, GameObject _gameObject, GameObject _base, HashSet<GameObject> _done)
		{
			foreach (var record in _list)
			{
				if (record.Asset == _base)
				{
					_done.Add(_gameObject);
					record.VariantRecordsBasedOnThis.Add(new () {Asset = _gameObject, Base = record});
					return true;
				}
				
				if (TryInsertRecord(record.VariantRecordsBasedOnThis, _gameObject, _base, _done))
					return true;
			}
			
			return false;
		}

		private static string GetVariantRecordsDumpString()
		{
			string result = string.Empty;
			foreach (var record in s_variantRecords)
				result += record.GetDumpString();
			
			return result;
		}
		
		private static void Clone() => Clone(s_variantRecords);
		private static void Clone(List<VariantRecord> _list)
		{
			foreach (var record in _list)
				Clone(record);
		}

		private static void Clone(VariantRecord _record)
		{
			var asset = _record.Asset;
			var assetPath = AssetDatabase.GetAssetPath(asset);
			var filename = Path.GetFileNameWithoutExtension(assetPath);
			var extension = Path.GetExtension(assetPath);
			var newAssetPath = $"{s_targetDir}/{filename}{extension}";
			var variantName = $"{filename}Variant{extension}";
			var variantPath = $"{s_targetDir}/{variantName}";
			
			if (File.Exists(variantPath))
			{
				var existing = AssetDatabase.LoadAssetAtPath<GameObject>(variantPath);
				_record.Clone = existing;
				return;
			}
			
			var prefab = PrefabUtility.InstantiatePrefab(asset) as GameObject;
			if (!prefab)
				return;
			
			EditorFileUtility.EnsureFolderExists(s_targetDir);
			var variant = PrefabUtility.SaveAsPrefabAsset(prefab, newAssetPath);
			_record.Clone = variant;
			
			//TODO: Fix variant base and references
			
			AssetDatabase.RenameAsset(newAssetPath, variantName);
			
			prefab.SafeDestroy();
			
			foreach (var v in _record.VariantRecordsBasedOnThis)
				Clone(v);
		}

		private static void CleanUp()
		{
			s_variantRecords.Clear();
			s_baseByPrefab.Clear();
			s_clonedByPrefab.Clear();
		}
	}
}