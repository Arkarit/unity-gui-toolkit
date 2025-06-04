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
using Random = UnityEngine.Random;

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
			public AssetEntry AssetEntry = null;
			public AssetEntry CloneEntry = null;
			public VariantRecord Base = null;
			public bool IsRootVariant => Base == null;
			
			public readonly List<VariantRecord> VariantRecordsBasedOnThis = new ();
			
			public string GetDumpString(int _numTabs = 0)
			{
				var result = $"{new string('\t', _numTabs)}Original: __ {AssetEntry.Asset.name} __ Guid:{AssetEntry.Guid} FileId:{AssetEntry.Id} IsRootVariant:{IsRootVariant}\n";
				if (CloneEntry != null)
					result += $"{new string('\t', _numTabs)}Clone: __ {CloneEntry.Asset.name} __ Guid:{CloneEntry.Guid} FileId:{CloneEntry.Id}\n";
				else
					result += $"{new string('\t', _numTabs)}Clone:<null>\n";

				foreach (var record in VariantRecordsBasedOnThis)
					result += record.GetDumpString(_numTabs + 1);
				
				return result;
			}
		}
		
		private static readonly List<VariantRecord> s_variantRecords = new ();
		private static readonly Dictionary<GameObject, GameObject> s_baseByPrefab = new ();
		private static readonly Dictionary<string, string> s_guidMapping = new ();
		private static readonly Dictionary<long, long> s_fileIdMapping = new ();
		private static readonly List<GameObject> s_objectsToDelete = new();
		
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
//			ChangeParents();
			
//Debug.Log($"{GetVariantRecordsDumpString()}");

//AssetDatabase.DeleteAsset(s_targetDir);
			CleanUp();
		}

		private static void ChangeParents()
		{
			foreach (var record in s_variantRecords)
				ChangeParents(record);
		}

		private static void ChangeParents(VariantRecord record)
		{
			// We need not change parents of root variants
			if (!record.IsRootVariant)
				ChangeParents(record.CloneEntry.Asset);

			foreach (var basedOnThis in record.VariantRecordsBasedOnThis)
				ChangeParents(basedOnThis);
		}

		private static void ChangeParents(GameObject _go)
		{
Debug.Log(DumpMapping());
			HandleAsYaml(_go, yaml =>
			{
				foreach (var uComponent in yaml)
					uComponent.rootProperty.ReplaceGUIDAndFileID(s_guidMapping, s_fileIdMapping);

				return true;
			});
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
					s_variantRecords.Add(new VariantRecord() {AssetEntry = CreateAssetEntry(kv.Key)});
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
				if (record.AssetEntry.Asset == _base)
				{
					_done.Add(_gameObject);

					record.VariantRecordsBasedOnThis.Add(new ()
					{
						AssetEntry = CreateAssetEntry(_gameObject), 
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
				_record.CloneEntry = CreateAssetEntry(existing);

				return;
			}

			EditorFileUtility.EnsureFolderExists(targetDir);
			Debug.Log(DumpYamlString(baseSourceAsset));

			if (_asVariant)
			{
				if (!TryReadYaml(baseSourceAsset, out List<UComponent> yaml))
					return;

				if (!TryWriteYaml(yaml, variantPath))
					return;

				var clone = AssetDatabase.LoadAssetAtPath<GameObject>(variantPath);
				_record.CloneEntry = CreateAssetEntry(clone);
				AddToMapping(baseSourceAsset, clone);
			}
			else
			{
				var clone = PrefabUtility.InstantiatePrefab(baseSourceAsset) as GameObject;
				var variant = PrefabUtility.SaveAsPrefabAsset(clone, variantPath);
				AddToMapping(baseSourceAsset, variant);

				_record.CloneEntry = CreateAssetEntry(variant);
				s_objectsToDelete.Add(clone);
			}

			foreach (var v in _record.VariantRecordsBasedOnThis)
				Clone(v, true);
		}

		private static void AddToMapping(GameObject baseSourceAsset, GameObject variant)
		{
			if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(baseSourceAsset, out string originalGuid,
				    out long originalId) &&
			    AssetDatabase.TryGetGUIDAndLocalFileIdentifier(variant, out string variantGuid,
				    out long variantId))
			{
				s_guidMapping.Add(originalGuid, variantGuid);
				s_fileIdMapping.Add(originalId, variantId);
				return;
			}

			Debug.LogError($"Could not add mapping for '{baseSourceAsset.GetPath()}' -> {variant.GetPath()}");
		}

		private static bool TryReadYaml(GameObject _go, out List<UComponent> _yaml)
		{
			_yaml = new List<UComponent>();

			try
			{
				var assetPath = AssetDatabase.GetAssetPath(_go);
				if (string.IsNullOrEmpty(assetPath))
					return false;
	
				var yamlText = File.ReadAllText(assetPath);
				if (string.IsNullOrEmpty(yamlText))
					return false;
	
				_yaml = Parser.Parse(yamlText);
				return true;
			}
			catch
			{
				return false;
			}
		}

		private static bool TryWriteYaml(List<UComponent> _yaml, string _variantPath)
		{
			GameObject go = null;
			try
			{
				go = new GameObject("UnitySucks");
				PrefabUtility.SaveAsPrefabAsset(go, "Assets/UnitySucks.prefab");
				var ass = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/UnitySucks.prefab");
				AssetDatabase.TryGetGUIDAndLocalFileIdentifier(ass, out string _, out long id);
				_yaml[0].fileID = id;
				var newYamlText = Writer.Build(_yaml);
				File.WriteAllText(_variantPath, newYamlText);
				AssetDatabase.ImportAsset(_variantPath);
				return true;
			}
			catch
			{
				return false;
			}
			finally
			{
				AssetDatabase.DeleteAsset("Assets/UnitySucks.prefab");
				go.SafeDestroy();
			}
		}

		private static bool HandleAsYaml(GameObject _gameObject, Func<List<UComponent>, bool> _handler)
		{
			try
			{
				if (!_gameObject)
					return false;

				var assetPath = AssetDatabase.GetAssetPath(_gameObject);
				if (_handler == null)
					return false;

				if (!TryReadYaml(_gameObject, out List<UComponent> _yaml))
					return false;

				if (_handler.Invoke(_yaml))
					return TryWriteYaml(_yaml, assetPath);

				return true;
			}
			catch
			{
				return false;
			}
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

		private static string DumpMapping()
		{
			Debug.Assert(s_fileIdMapping.Count == s_guidMapping.Count);
			string result = "File Id/GUID mapping:\n";

			List<string> guidSrc = new ();
			List<string> guidDst = new ();
			List<long> idSrc = new ();
			List<long> idDst = new ();
			foreach (var kv in s_guidMapping)
			{
				guidSrc.Add(kv.Key);
				guidDst.Add(kv.Value);
			}
			foreach (var kv in s_fileIdMapping)
			{
				idSrc.Add(kv.Key);
				idDst.Add(kv.Value);
			}

			for (int i = 0; i < s_guidMapping.Count; i++)
				result += $"{idSrc[i]}, {guidSrc[i]} -> {idDst[i]}, {guidDst[i]}\n";

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
			var asset = _record.AssetEntry.Asset;
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
			s_guidMapping.Clear();
			s_fileIdMapping.Clear();
			foreach (var gameObject in s_objectsToDelete)
				gameObject.SafeDestroy();
			s_objectsToDelete.Clear();
		}
	}
}