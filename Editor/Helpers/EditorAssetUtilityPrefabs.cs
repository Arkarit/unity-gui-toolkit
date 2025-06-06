using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GuiToolkit.Editor
{
	public static partial class EditorAssetUtility
	{
		public const string PrefabFolder = "Prefabs/";

		public class PrefabVariantsOptions
		{
			public string Postfix = string.Empty;
		}
		
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
		
		private static readonly List<VariantRecord> s_variantRecords = new();
		private static readonly Dictionary<GameObject, GameObject> s_baseByPrefab = new();
		private static readonly List<GameObject> s_objectsToDelete = new();
		private static readonly Dictionary<GameObject, GameObject> s_cloneByOriginal = new();
		
		private static string s_sourceDir;
		private static string s_targetDir;
		private static PrefabVariantsOptions s_options;
		
		public static string BuiltinPrefabDir
		{
			get
			{
				string rootProjectDir = UiToolkitConfiguration.Instance.GetUiToolkitRootProjectDir();
				return rootProjectDir + PrefabFolder;
			}
		}

		// Untested
		public static bool ExecuteInPrefab(GameObject _prefab, Func<GameObject, bool> _callback, EErrorType _errorType = EErrorType.None)
		{
			if (!PrefabUtility.IsAnyPrefabInstanceRoot(_prefab))
				return ShowError
				(
					$"{nameof(ExecuteInPrefab)} works only with prefab instance roots, but " + 
				    $"'{_prefab.GetPath(1)}' isn't such.\nFull path:'{_prefab.GetPath()}'",
					_errorType
				);

			var prefabRoot = PrefabUtility.GetCorrespondingObjectFromSource(_prefab);
			if (prefabRoot == null)
				return ShowError($"Prefab Root for '{_prefab.GetPath(1)}' not found.\nFull path:'{_prefab.GetPath()}'", _errorType);

			if (_callback.Invoke(prefabRoot))
			{
				EditorUtility.SetDirty(prefabRoot);
				AssetDatabase.SaveAssetIfDirty(prefabRoot);

				return true;
			}

			return false;
		}

		public static void CreatePrefabVariants(string _sourceDir, string _targetDir, PrefabVariantsOptions _options = null)
		{

			try
			{
				s_options = _options ?? new PrefabVariantsOptions();
				s_sourceDir = string.IsNullOrEmpty(_sourceDir) ? BuiltinPrefabDir : _sourceDir;
				s_targetDir = _targetDir;
				CleanUp();
				BuildPrefabVariantHierarchy();
				Clone();
			}
			finally
			{
				CleanUp();
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
AssetDatabase.TryGetGUIDAndLocalFileIdentifier(prefab, out string tguid, out long id);
Debug.Log($"---::: Original: {prefab.name}:  {id}  :  {tguid}\n{DumpOverridesString(prefab, prefab.name)}");

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

		private static string GetCloneByOriginalDumpString()
		{
			string result = "Original -> Clone:\n_________________________\n";

			foreach (var kv in s_cloneByOriginal)
			{
				result += $"{kv.Key.GetPath(1)} -> {kv.Value.GetPath(1)}\n";
			}

			return result;
		}

		private static void Clone()
		{
			Clone(s_variantRecords);
Debug.Log(GetCloneByOriginalDumpString());
		}

		private static void Clone(List<VariantRecord> _list)
		{
			foreach (var record in _list)
				Clone(record, null);
		}

		private static void Clone(VariantRecord _record, GameObject parent)
		{
			bool isRoot = parent == null;
			var baseSourceAsset = GetSourceAssetAndPaths(_record, out var targetDir, out var newAssetPath, out var variantName, out var variantPath);
			var sourceGameObject = isRoot ? baseSourceAsset : parent;

			if (File.Exists(variantPath))
			{
				var existing = AssetDatabase.LoadAssetAtPath<GameObject>(variantPath);
				_record.CloneEntry = CreateAssetEntry(existing);

				return;
			}

			// First step is to create a chain of prefab variants, starting with a variant of the base object
			EditorFileUtility.EnsureFolderExists(targetDir);
			var clone = PrefabUtility.InstantiatePrefab(sourceGameObject) as GameObject;
			s_objectsToDelete.Add(clone);

			if (!isRoot)
			{
Debug.Log($"---::: original: {DumpOverridesString(_record.AssetEntry.Asset, _record.AssetEntry.Asset.name)}");
				CloneRemovedAndAdded(_record.AssetEntry.Asset, clone);
				CloneOverrides(_record.AssetEntry.Asset, clone);
Debug.Log($"---::: after CloneOverrides: {DumpOverridesString(clone, clone.name)}");
			}

			FixReferencesInClone(sourceGameObject, clone);
			var clonedVariant = PrefabUtility.SaveAsPrefabAssetAndConnect(clone, variantPath, InteractionMode.AutomatedAction);
			_record.CloneEntry = CreateAssetEntry(clonedVariant);
			s_cloneByOriginal.Add(baseSourceAsset, clonedVariant);

			foreach (var v in _record.VariantRecordsBasedOnThis)
				Clone(v, clonedVariant);
		}

		private static void CloneRemovedAndAdded(GameObject _originalAsset, GameObject _clonedAsset)
		{
			var addedGameObjects = PrefabUtility.GetAddedGameObjects(_originalAsset);
			var removedGameObjects = PrefabUtility.GetRemovedGameObjects(_originalAsset);
			var addedComponents = PrefabUtility.GetAddedComponents(_originalAsset);
			var removedComponents = PrefabUtility.GetRemovedComponents(_originalAsset);
string s = $"---::: Processing {addedGameObjects.Count} added game objects\n";
			foreach (var addedGameObject in addedGameObjects)
			{
				var addedGo = addedGameObject.instanceGameObject;
s += $"\tCloning {addedGo.name}...\n";
				var originalParent = addedGo.transform.parent;
				if (originalParent == null)
				{
					// Error msg?
s += "\toriginalParent is null\n\n";
					continue;
				}

				var clonedParent = FindMatchingComponent(_clonedAsset, originalParent);
				if (clonedParent == null)
				{
					// Error msg?
s += "\tclonedParent is null\n\n";
					continue;
				}

//				var clone = GameObject.Instantiate(addedGo, clonedParent);
				var clone = addedGo.PrefabAwareClone(clonedParent);
				
				clone.name = addedGo.name;
				clone.transform.SetSiblingIndex(addedGameObject.siblingIndex);
s += $"\tcloned {clone.name}\n\n";

			}

s += $"---::: Processing {removedGameObjects.Count} removed game objects\n";
			foreach (var removedGameObject in removedGameObjects)
			{
				var removedGo = removedGameObject.assetGameObject;
s += $"\tRemoving {removedGo.name}...\n";

				var clonedRemovedGo = FindMatchingGameObject(_clonedAsset, removedGo);
				if (clonedRemovedGo == null)
				{
					// Error msg?
s += "\tclonedRemovedGo is null\n\n";
					continue;
				}

s += $"\tRemoving {clonedRemovedGo.name}\n\n";
				clonedRemovedGo.SafeDestroy();
			}

s += $"---::: Processing {addedComponents.Count} added components\n";
			foreach (var addedComponent in addedComponents)
			{
				//TODO: Component index not yet supported!

				var sourceComponent = addedComponent.instanceComponent;
				var sourceGameObject = sourceComponent.gameObject;

				var clonedGameObject = FindMatchingGameObject(_clonedAsset, sourceGameObject);
				if (clonedGameObject == null)
				{
					// Error msg?
s += "\tclonedGameObject is null\n\n";
					continue;
				}

				var clonedComponent = clonedGameObject.AddComponent(sourceComponent.GetType());
				EditorUtility.CopySerializedManagedFieldsOnly(sourceComponent, clonedComponent);
s += $"\tcloned component {clonedComponent.GetType().Name} on {clonedGameObject.name}\n\n";
			}

s += $"---::: Processing {removedComponents.Count} removed components\n";
			foreach (var removedComponent in removedComponents)
			{
				var sourceComponent = removedComponent.assetComponent;
				var targetComponent = FindMatchingComponent(_clonedAsset, sourceComponent);
				if (targetComponent == null)
				{
s += "\ttargetComponent is null\n\n";
					continue;
				}

s += $"\tRemoving {targetComponent.name}\n\n";
				targetComponent.SafeDestroy();
			}

Debug.Log(s);
		}

		private static void CloneOverrides(GameObject _originalAsset, GameObject _clonedAsset)
		{
			var sourcePropertyModifications = PrefabUtility.GetPropertyModifications(_originalAsset);
			List<PropertyModification> targetPropertyModifications = new();

string s = $"---::: Overrides mapping ({sourcePropertyModifications} modifications):\n";

			foreach (var propertyModification in sourcePropertyModifications)
			{
				Object originalTarget = propertyModification.target;
				var clonedTarget = FindMatchingObject(_clonedAsset, originalTarget);
				if (clonedTarget != null)
				{
s += $"\t{propertyModification.propertyPath}:\n\t{DumpCorrespondingObjectFromSource(originalTarget)} ->\n\t{DumpCorrespondingObjectFromSource(clonedTarget)}\n\n";

					PropertyModification targetPropertyModification =  propertyModification.ShallowClone();
					targetPropertyModification.target = PrefabUtility.GetCorrespondingObjectFromSource(clonedTarget);
					targetPropertyModifications.Add(targetPropertyModification);
				}
				else
				{
s += $"\tNo matching object found for {DumpCorrespondingObjectFromSource(originalTarget)}\n\n";
				}
			}


			PrefabUtility.SetPropertyModifications(_clonedAsset, targetPropertyModifications.ToArray());
Debug.Log(s);
Debug.Log($"---::: Set {targetPropertyModifications.Count} modifications");
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
			if (!PrefabUtility.IsPartOfVariantPrefab(_asset))
				return string.Empty;

			var sourcePropertyModifications = PrefabUtility.GetPropertyModifications(_asset);
			var sourceObjectOverrides = PrefabUtility.GetObjectOverrides(_asset);
			var addedComponents = PrefabUtility.GetAddedComponents(_asset);
			var addedGameObjects = PrefabUtility.GetAddedGameObjects(_asset);
			var removedComponents = PrefabUtility.GetRemovedComponents(_asset);
			var removedGameObjects = PrefabUtility.GetRemovedGameObjects(_asset);

			string result = $"{_what}: '{AssetDatabase.GetAssetPath(_asset)}':\n\n";
			
			result += $"\t{_what} Property Modifications ({sourcePropertyModifications.Length})\n";
			foreach (var modification in sourcePropertyModifications)
				result += $"\t\t'{modification.value}':'{modification.propertyPath}':'{modification.objectReference}':'{modification.target}', id:{modification.target.GetInstanceID()}{DumpCorrespondingObjectFromSource(modification.target)}\n";
			result += "\n\t";
			
			result += $"\t{_what} Object Overrides\n";
			foreach (var sourceObjectOverride in sourceObjectOverrides)
				result += $"\t\t'{sourceObjectOverride.coupledOverride}':'{sourceObjectOverride.instanceObject}'{DumpCorrespondingObjectFromSource(sourceObjectOverride.instanceObject)}\n";

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

		private static string DumpCorrespondingObjectFromSource(Object _obj)
		{
			string result = string.Empty;
			var cofs = PrefabUtility.GetCorrespondingObjectFromSource(_obj);
			if (cofs != null)
			{
				result = $" cofs:{cofs.name} type:{cofs.GetType()}";
				if (AssetDatabase.TryGetGUIDAndLocalFileIdentifier(cofs, out string guid, out long id))
					result += $" guid:{guid} fileid:{id}";
			}

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
			_variantName = $"{basename}{s_options.Postfix}{extension}";
			_variantPath = $"{_targetDir}/{_variantName}";
			return asset;
		}

		private static void CleanUp()
		{
			s_variantRecords.Clear();
			s_baseByPrefab.Clear();
			s_cloneByOriginal.Clear();
			foreach (var gameObject in s_objectsToDelete)
				gameObject.SafeDestroy();
			s_objectsToDelete.Clear();
		}
	}
}