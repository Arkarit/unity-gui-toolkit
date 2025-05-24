using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Lachee.UYAML;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.VersionControl;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GuiToolkit.Editor
{
	public static class EditorPrefabUtility
	{
		public const string PrefabFolder = "Prefabs/";
		
		private class AssetEntry
		{
			public GameObject Asset = null;
			public string Guid = string.Empty;
			public long Id = 0;
		}

		private class VariantRecord
		{
			public AssetEntry Asset = null;
			public AssetEntry Clone = null;
			public VariantRecord Base = null;
			public bool IsRootVariant => Base == null;
			
			
			public readonly List<VariantRecord> VariantRecordsBasedOnThis = new ();
			
			public string GetDumpString(int _numTabs = 0)
			{
				var result = $"{new string('\t', _numTabs)}Original:{Asset.Asset.name} Guid:{Asset.Guid} FileId:{Asset.Id} IsRootVariant:{IsRootVariant}\n";
				if (Clone != null)
					result += $"{new string('\t', _numTabs)}Clone:{Clone.Asset.name} Guid:{Clone.Guid} FileId:{Clone.Id}\n";
				else
					result += $"{new string('\t', _numTabs)}Clone:<null>\n";

				foreach (var record in VariantRecordsBasedOnThis)
					result += record.GetDumpString(_numTabs + 1);
				
				return result;
			}
		}
		
		private static readonly List<VariantRecord> s_variantRecords = new ();
		private static readonly Dictionary<GameObject, GameObject> s_baseByPrefab = new ();
		
		private static string s_sourceDir;
		private static string s_targetDir;
		private static bool s_addVariantNamePart;
		
		public static string BuiltinPrefabDir
		{
			get
			{
				string rootProjectDir = UiToolkitConfiguration.Instance.GetUiToolkitRootProjectDir();
				return rootProjectDir + PrefabFolder;
			}
		}
		
		public static void CreatePrefabVariants(string _sourceDir, string _targetDir, bool _addVariantNamePart = true)
		{
			s_addVariantNamePart = _addVariantNamePart;
			s_sourceDir = string.IsNullOrEmpty(_sourceDir) ? BuiltinPrefabDir : _sourceDir;
			s_targetDir = _targetDir;
			CleanUp();
			BuildPrefabVariantHierarchy();
			Clone();
			ChangeParents();
			
Debug.Log($"{GetVariantRecordsDumpString()}");

//AssetDatabase.DeleteAsset(s_targetDir);
			CleanUp();
		}

		private static void ChangeParents()
		{
			foreach (var record in s_variantRecords)
			{
				// We need not change parents of root variants
				if (record.IsRootVariant)
					continue;
			}
		}

		private static void BuildPrefabVariantHierarchy()
		{
			var guids = AssetDatabase.FindAssets("t:prefab", new []{ s_sourceDir }).ToHashSet();
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
					s_variantRecords.Add(new VariantRecord() {Asset = CreateAssetEntry(kv.Key)});
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
		}
		
		private static bool TryInsertRecord(List<VariantRecord> _list, GameObject _gameObject, GameObject _base, HashSet<GameObject> _done)
		{
			foreach (var record in _list)
			{
				if (record.Asset.Asset == _base)
				{
					_done.Add(_gameObject);

					record.VariantRecordsBasedOnThis.Add(new ()
					{
						Asset = CreateAssetEntry(_gameObject), 
						Base = record
					});
					return true;
				}
				
				if (TryInsertRecord(record.VariantRecordsBasedOnThis, _gameObject, _base, _done))
					return true;
			}
			
			return false;
		}

		private static AssetEntry CreateAssetEntry(GameObject _gameObject)
		{
			if (_gameObject == null)
				throw new NullReferenceException($"Asset entry can't be created with null game object");

			if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(_gameObject, out string guid, out long id))
				throw new ArgumentException($"Internal Exception: '{_gameObject}' is not a disk-based asset! Please create asset first");

			return new AssetEntry()
			{
				Asset = _gameObject,
				Guid = guid,
				Id = id
			};
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
				Clone(record, false);
		}

		private static void Clone(VariantRecord _record, bool _asVariant)
		{
			var baseSourceAsset = GetSourceAssetAndPaths(_record, out var targetDir, out var newAssetPath, out var variantName, out var variantPath);

			if (File.Exists(variantPath))
			{
				var existing = AssetDatabase.LoadAssetAtPath<GameObject>(variantPath);
				_record.Clone = CreateAssetEntry(existing);

				return;
			}

			EditorFileUtility.EnsureFolderExists(targetDir);
			Debug.Log(DumpYamlString(baseSourceAsset));

			if (_asVariant)
			{
				var assetPath = AssetDatabase.GetAssetPath(baseSourceAsset);
				var yamlText = File.ReadAllText(assetPath);
				var yaml = Parser.Parse(yamlText);
				var newYamlText = Writer.Build(yaml);
				File.WriteAllText(variantPath, newYamlText);
				AssetDatabase.ImportAsset(variantPath);
				var clone = AssetDatabase.LoadAssetAtPath<GameObject>(variantPath);
				_record.Clone = CreateAssetEntry(clone);
			}
			else
			{
				var clone = PrefabUtility.InstantiatePrefab(baseSourceAsset) as GameObject;
				var variant = PrefabUtility.SaveAsPrefabAsset(clone, variantPath);
				_record.Clone = CreateAssetEntry(variant);
			}

			foreach (var v in _record.VariantRecordsBasedOnThis)
				Clone(v, true);
		}

		public static string DumpYamlString(GameObject _gameObject)
		{
			string result = $"YAML for for '{_gameObject.name}':\n----------------------------------------------------------------\n";
			var assetPath = AssetDatabase.GetAssetPath(_gameObject);
			if (string.IsNullOrEmpty(assetPath))
				return result + "\t<Asset Path Not Found>";

			var yamlText = File.ReadAllText(assetPath);
			var yaml = Parser.Parse(yamlText);

			foreach (var uComponent in yaml)
			{
				result += $"\t{uComponent.name}:{uComponent.classID}:{uComponent.fileID}\n";
				var root = uComponent.root;
				foreach (var kv in root)
				{
					result += $"\t\t{kv.Key}:{kv.Value}\n";
				}
			}

			return result;
		}

		public static string DumpAllProperties(SerializedObject _serObj)
		{
			string result = $"Properties for '{_serObj.targetObject.name}':\n----------------------------------------------------------------\n";
			EditorGeneralUtility.ForeachPropertyHierarchical(_serObj, property =>
			{
				result += $"\t{property.propertyPath}:{property.prefabOverride}\n";
			});
			
			return result;
		}
		
		public static string DumpOverridesString(GameObject _asset, string _what)
		{
			var sourcePropertyModifications = PrefabUtility.GetPropertyModifications(_asset);
			var sourceObjectOverrides = PrefabUtility.GetObjectOverrides(_asset);
			var addedComponents = PrefabUtility.GetAddedComponents(_asset);
			var addedGameObjects = PrefabUtility.GetAddedGameObjects(_asset);
			var removedComponents = PrefabUtility.GetRemovedComponents(_asset);
			var removedGameObjects = PrefabUtility.GetRemovedGameObjects(_asset);

			string result = $"{_what}: '{AssetDatabase.GetAssetPath(_asset)}':\n\n";
			
			result += $"\t{_what} Property Modifications ({sourcePropertyModifications.Length})\n";
			foreach (var modification in sourcePropertyModifications)
				result += $"\t\t'{modification.value}':'{modification.propertyPath}':'{modification.objectReference}':'{modification.target}', id:{modification.target.GetInstanceID()}\n";
			result += "\n\t";
			
			result += $"\t{_what} Object Overrides\n";
			foreach (var sourceObjectOverride in sourceObjectOverrides)
				result += $"\t\t'{sourceObjectOverride.coupledOverride}':'{sourceObjectOverride.instanceObject}'\n";
			result += "\n\t";
			
			result += $"\t{_what} Added Components\n";
			foreach (var addedComponent in addedComponents)
				result += $"\t\t'{addedComponent.instanceComponent.name}'\n";
			result += "\n\t";
			
			result += $"\t{_what} Added Game Objects\n";
			foreach (var addedGameObject in addedGameObjects)
				result += $"\t\t'{addedGameObject.instanceGameObject.name}':'{addedGameObject.siblingIndex}'\n";
			result += "\n\t";
			
			result += $"\t{_what} Removed Components\n";
			foreach (var removedComponent in removedComponents)
				result += $"\t\t'{removedComponent.assetComponent.name}'\n";
			result += "\n\t";
			
			result += $"\t{_what} Removed Game Objects\n";
			foreach (var removedGameObject in removedGameObjects)
				result += $"\t\t'{removedGameObject.assetGameObject.name}'\n";
			result += "\n\t";
			
			return result;
		}
		
		private static GameObject GetSourceAssetAndPaths
		(
			VariantRecord _record, 
			out string _targetDir, 
			out string _newAssetPath,
			out string _variantName, 
			out string _variantPath
		)
		{
			var asset = _record.Asset.Asset;
			var assetPath = AssetDatabase.GetAssetPath(asset);
			var basename = Path.GetFileNameWithoutExtension(assetPath);
			var extension = Path.GetExtension(assetPath);
			var filename = Path.GetFileName(assetPath);

			_targetDir = s_targetDir + assetPath.Replace(s_sourceDir, "").Replace(filename, "");
			_newAssetPath = $"{_targetDir}/{basename}{extension}";
			_variantName = s_addVariantNamePart ? $"{basename} ClonedVariant{extension}" : $"{basename}{extension}";
			_variantPath = $"{_targetDir}/{_variantName}";
			return asset;
		}

		private static void CleanUp()
		{
			s_variantRecords.Clear();
			s_baseByPrefab.Clear();
		}
	}
}