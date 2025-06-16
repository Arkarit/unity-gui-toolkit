using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GuiToolkit.Debugging;
using GuiToolkit.Editor.Internal;
using UnityEditor;
using UnityEditor.iOS;
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
		private static readonly List<GameObject> s_originals = new();
		private static readonly List<GameObject> s_clones = new();
		private static readonly Dictionary<GameObject, GameObject> s_clonesByOriginals = new();
		private static readonly Dictionary<GameObject, GameObject> s_originalsByClones = new();
		
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

		public static bool ExecuteInPrefab(GameObject _prefab, Func<GameObject, bool> _callback, EErrorType _errorType = EErrorType.None)
		{
			GameObject temporaryClone = null;

			try
			{
				if (!PrefabUtility.IsAnyPrefabInstanceRoot(_prefab))
					return ShowError
					(
						$"{nameof(ExecuteInPrefab)} works only with prefab instance roots, but " + 
					    $"'{_prefab.GetPath(1)}' isn't such.\nFull path:'{_prefab.GetPath()}'",
						_errorType
					);
	
				var assetPath = AssetDatabase.GetAssetPath(_prefab);
				temporaryClone = (GameObject) PrefabUtility.InstantiatePrefab(_prefab);
				temporaryClone.name = _prefab.name;

				if (_callback.Invoke(temporaryClone))
				{
					PrefabUtility.SaveAsPrefabAssetAndConnect(temporaryClone, assetPath, InteractionMode.AutomatedAction);
					return true;
				}
			}
			finally
			{
				temporaryClone.SafeDestroy();
			}

			return false;
		}

		public static void SortByPrefabHierarchy(List<GameObject> _gameObjectList)
		{
DebugUtility.Log("Before", _gameObjectList, DebugUtility.DumpFeatures.None);
			List<GameObject> toDo = new List<GameObject>(_gameObjectList);
			List<GameObject> done = new ();

			List<Transform[]> transforms = new();
			foreach (var gameObject in toDo)
				transforms.Add(gameObject.GetComponentsInChildren<Transform>());

			for (int i = toDo.Count - 1; i >= 0; i--)
			{
				GameObject current = toDo[i];

				if (PrefabUtility.IsPartOfRegularPrefab(current))
				{
Debug.Log($"Add base {current.GetPath()}");
					done.Add(current);
					toDo.RemoveAt(i);
					transforms.RemoveAt(i);
				}
			}

			while (toDo.Count > 0)
			{
				for (int i = 0; i < toDo.Count; i++)
				{
					GameObject current = toDo[i];

					bool containsUnhandledPrefabs = false;
					foreach (var t in transforms[i])
					{
						GameObject currentDescendant = t.gameObject;
						if (currentDescendant == current)
							continue;

						if (!PrefabUtility.IsAnyPrefabInstanceRoot(currentDescendant))
							continue;

						var prefab = PrefabUtility.GetCorrespondingObjectFromOriginalSource(currentDescendant);
						if (toDo.Contains(prefab))
						{
Debug.Log($"Found {prefab.GetPath()}");
							containsUnhandledPrefabs = true;
							break;
						}
					}

					if (containsUnhandledPrefabs)
						continue;

Debug.Log($"Add {current.GetPath()}");
					done.Add(current);
					toDo.RemoveAt(i);
					transforms.RemoveAt(i);
					break;
				}
			}

			_gameObjectList.Clear();
			_gameObjectList.AddRange(done);
DebugUtility.Log("After", _gameObjectList, DebugUtility.DumpFeatures.None);
		}

		public static void SortByPrefabHierarchyAssetPath(List<string> _pathList)
		{
			List<GameObject> assets = new();
			foreach (var path in _pathList)
			{
				var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
				if (asset == null)
					continue;

				assets.Add(asset);
			}

			SortByPrefabHierarchy(assets);
			_pathList.Clear();
			foreach (var gameObject in assets)
				_pathList.Add(AssetDatabase.GetAssetPath(gameObject));
		}

		public static void SortByPrefabHierarchyGuids(List<string> _guidList)
		{
			List<string> pathList = new();
			foreach (var guid in _guidList)
			{
				var path = AssetDatabase.GUIDToAssetPath(guid);
				if (string.IsNullOrEmpty(path))
					continue;
				pathList.Add(path);
			}

			SortByPrefabHierarchyAssetPath(pathList);

			_guidList.Clear();
			foreach (var path in pathList)
				_guidList.Add(AssetDatabase.AssetPathToGUID(path));
		}

		private static void ReplaceInsertedPrefabs()
		{
			HashSet<GameObject> clonesToDo = s_clones.ToHashSet();
			List<GameObject> clonesDone = new();

			int currIdx = 0;

			// The approach is necessarily quite complicated.
			// The first clones to save are those which don't contain any other clones from the list (e.g. a button)
			// Then those which only contain these buttons (e.g. a button panel)
			// Then those which only contain these button panels and buttons...
			// Only by this way it can be ensured that overrides can be cloned properly.
			while (clonesToDo.Count > 0)
			{
				foreach (var clone in clonesToDo)
				{
					ExecuteInPrefab(clone, root =>
					{
						var transforms = root.GetComponentsInChildren<Transform>();
						Dictionary<GameObject, GameObject> clonesByOriginalsToReplace = new();
	
						foreach (var transform in transforms)
						{
							if (transform == root.transform)
								continue;
	
							var go = transform.gameObject;
							if (go.GetComponent<EditorMarker>())
								continue;

							var source = PrefabUtility.GetCorrespondingObjectFromOriginalSource(go);
							if (!s_clonesByOriginals.ContainsKey(source))
								continue;

							clonesByOriginalsToReplace.Add(go, s_clonesByOriginals[source]);
						}

						if (clonesByOriginalsToReplace.Count == 0)
						{
							clonesDone.Add(clone);
							return false;
						}

						foreach (var kv in clonesByOriginalsToReplace)
						{
							if (!clonesDone.Contains(kv.Value))
							{
								return false;
							}
						}

						foreach (var kv in clonesByOriginalsToReplace)
						{
							var embeddedOriginal = kv.Key;
							if (embeddedOriginal == null)
								continue;

							var embeddedClone = kv.Value;
							var parent = embeddedOriginal.transform.parent;
							var embeddedCloneInstance = (GameObject) PrefabUtility.InstantiatePrefab(embeddedClone, parent);
							embeddedCloneInstance.name = embeddedOriginal.name;
							embeddedCloneInstance.AddComponent<EditorMarkerIsClone>();

							embeddedCloneInstance.transform.SetSiblingIndex(embeddedOriginal.transform.GetSiblingIndex()+1);

							var children = embeddedOriginal.transform.GetChildrenList();
							foreach (var child in children)
							{
								if (child.GetComponent<EditorMarkerIsClone>())
									continue;

								DebugUtility.Log("Check for match", child.gameObject);
								// Possibly already exists in clone
								if (FindMatchingGameObject(embeddedCloneInstance, child.gameObject))
									continue;

								var clonedChild = child.gameObject.PrefabAwareClone(embeddedCloneInstance.transform);
								Debug.Assert(clonedChild.GetComponent<EditorMarkerIsClone>() == null, "There should be no EditorMarker on this go, because it should be tested already in an earlier stage");
								clonedChild.AddComponent<EditorMarkerIsClone>();
							}

							embeddedOriginal.AddComponent<EditorMarkerIsOriginal>();
						}

						clonesDone.Add(clone);
						return true;
					});
				}

				foreach (var go in clonesDone)
					clonesToDo.Remove(go);
			}

			// clonesDone is already properly sorted in terms of prefab chain dependencies, so we
			// can just conveniently walk through the list to clone stuff and remove the temporary EditorMarkers
			foreach (var clone in clonesDone)
			{
				ExecuteInPrefab(clone, root =>
				{
					var originalMarkers = clone.GetComponentsInChildren<EditorMarkerIsOriginal>();
					var clonedMarkers = clone.GetComponentsInChildren<EditorMarkerIsClone>();

					// If there are no original markers, there shouldn't be any clone markers as well
					if (originalMarkers == null || originalMarkers.Length == 0)
						return false;

					foreach (var clonedMarker in clonedMarkers)
					{
						if (clonedMarker == null)
							continue;

						clonedMarker.SafeDestroy();
					}

					foreach (var originalMarker in originalMarkers)
					{
						if (originalMarker == null)
							continue;

						originalMarker.gameObject.SafeDestroy();
					}

					return true;
				});
			}

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
				ReplaceInsertedPrefabs();
				TransferOverridesDir(_sourceDir, _targetDir);
			}
			finally
			{
				CleanUp();
			}
		}

		public static void TransferOverridesDir(string _sourceDir, string _targetDir)
		{
			// TODO: clone overrides/add/remove recursively
			List<(string sourcePath, string targetPath)> paths = new();
			var sourceGuids = AssetDatabase.FindAssets("t:prefab", new []{ _sourceDir }).ToList();
			SortByPrefabHierarchyGuids(sourceGuids);

			foreach (var sourceGuid in sourceGuids)
			{
				var sourcePath = AssetDatabase.GUIDToAssetPath( sourceGuid );
				var targetPath = sourcePath.Replace(_sourceDir, _targetDir);
				if (sourcePath == targetPath || !File.Exists(targetPath))
				{
					//TODO error msg?
					continue;
				}

Debug.Log($"!!! sourcePath:{sourcePath}\ntargetPath:{targetPath}");
				paths.Add((sourcePath, targetPath));
			}

			TransferOverrides(paths);
		}

		public static void TransferOverrides(List<(string sourcePath, string targetPath)> _paths)
		{
			foreach (var valueTuple in _paths)
				TransferOverrides(valueTuple.sourcePath, valueTuple.targetPath);
		}

		public static void TransferOverrides(string _sourcePath, string _targetPath)
		{
			GameObject targetInstance = null;
			try
			{
				var source = AssetDatabase.LoadAssetAtPath<GameObject>(_sourcePath);
				var target = AssetDatabase.LoadAssetAtPath<GameObject>(_targetPath);
				targetInstance = (GameObject)PrefabUtility.InstantiatePrefab(target);

				CloneOverrides(source, targetInstance);
				PrefabUtility.SaveAsPrefabAssetAndConnect(targetInstance, AssetDatabase.GetAssetPath(target), InteractionMode.AutomatedAction);
			}
			finally
			{
				targetInstance.SafeDestroy();
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
Debug.Log($"---::: Original: {prefab.name}:  {id}  :  {tguid}\n{DebugUtility.DumpOverridesString(prefab, prefab.name)}");

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

			for (int i=0; i<s_originals.Count; i++)
			{
				result += $"{s_originals[i].GetPath(1)} -> {s_clones[i].GetPath(1)}\n";
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
			clone.name = sourceGameObject.name;
			s_objectsToDelete.Add(clone);

//			if (!isRoot)
//			{
//				CloneRemovedAndAdded(_record.AssetEntry.Asset, clone);
//				CloneOverrides(_record.AssetEntry.Asset, clone);
//			}

			FixReferencesInClone(sourceGameObject, clone);
			var clonedVariant = PrefabUtility.SaveAsPrefabAssetAndConnect(clone, variantPath, InteractionMode.AutomatedAction);
			_record.CloneEntry = CreateAssetEntry(clonedVariant);
			s_originals.Add(baseSourceAsset);
			s_clones.Add(clonedVariant);
			s_clonesByOriginals.Add(baseSourceAsset, clonedVariant);
			s_originalsByClones.Add(clonedVariant, baseSourceAsset);

			foreach (var v in _record.VariantRecordsBasedOnThis)
				Clone(v, clonedVariant);
		}

		private static void CloneRemovedAndAdded(GameObject _originalAsset, GameObject _clonedAsset)
		{
			if (!PrefabUtility.IsPartOfVariantPrefab(_originalAsset))
				return;

			var addedGameObjects = PrefabUtility.GetAddedGameObjects(_originalAsset);
			var removedGameObjects = PrefabUtility.GetRemovedGameObjects(_originalAsset);
			var addedComponents = PrefabUtility.GetAddedComponents(_originalAsset);
			var removedComponents = PrefabUtility.GetRemovedComponents(_originalAsset);
			foreach (var addedGameObject in addedGameObjects)
			{
				var addedGo = addedGameObject.instanceGameObject;
				var originalParent = addedGo.transform.parent;
				if (originalParent == null)
				{
					// Error msg?
					continue;
				}

				var clonedParent = FindMatchingComponent(_clonedAsset, originalParent);
				if (clonedParent == null)
				{
					// Error msg?
					continue;
				}

				var clone = addedGo.PrefabAwareClone(clonedParent);
				if (clone == null)
					continue;
				
				clone.name = addedGo.name;
				clone.transform.SetSiblingIndex(addedGameObject.siblingIndex);
			}

			foreach (var removedGameObject in removedGameObjects)
			{
				var removedGo = removedGameObject.assetGameObject;

				var clonedRemovedGo = FindMatchingGameObject(_clonedAsset, removedGo);
				if (clonedRemovedGo == null)
				{
					// Error msg?
					continue;
				}

				clonedRemovedGo.SafeDestroy();
			}

			foreach (var addedComponent in addedComponents)
			{
				//TODO: Component index not yet supported!

				var sourceComponent = addedComponent.instanceComponent;
				var sourceGameObject = sourceComponent.gameObject;

				var clonedGameObject = FindMatchingGameObject(_clonedAsset, sourceGameObject);
				if (clonedGameObject == null)
				{
					// Error msg?
					continue;
				}

				var clonedComponent = clonedGameObject.AddComponent(sourceComponent.GetType());
				EditorUtility.CopySerializedManagedFieldsOnly(sourceComponent, clonedComponent);
			}

			foreach (var removedComponent in removedComponents)
			{
				var sourceComponent = removedComponent.assetComponent;
				var targetComponent = FindMatchingComponent(_clonedAsset, sourceComponent);
				if (targetComponent == null)
					continue;

				targetComponent.SafeDestroy();
			}
		}

		private static void CloneOverrides(GameObject _originalAsset, GameObject _clonedAsset) => CloneOverrides(_originalAsset, _clonedAsset, _clonedAsset);

		private static void CloneOverrides(GameObject _originalAsset, GameObject _clonedAsset, GameObject _clonedRoot)
		{
			if (PrefabUtility.IsPartOfVariantPrefab(_originalAsset) || PrefabUtility.IsAnyPrefabInstanceRoot(_originalAsset))
			{
				var sourcePropertyModifications = PrefabUtility.GetPropertyModifications(_originalAsset);
Debug.Log($"___!!! Overrides: {DebugUtility.DumpOverridesString(_originalAsset, _originalAsset.GetPath(1))}\n\n{_originalAsset.GetPath()}");
				if (sourcePropertyModifications != null)
				{
					List<PropertyModification> targetPropertyModifications = new();
	
					foreach (var propertyModification in sourcePropertyModifications)
					{
						Object originalTarget = propertyModification.target;
						var clonedTarget = FindMatchingObject(_clonedRoot, originalTarget);
						if (clonedTarget != null)
						{
							PropertyModification targetPropertyModification = propertyModification.ShallowClone();
							targetPropertyModification.target =
								PrefabUtility.GetCorrespondingObjectFromSource(clonedTarget);
							targetPropertyModifications.Add(targetPropertyModification);
						}
					}
	
					PrefabUtility.SetPropertyModifications(_clonedAsset, targetPropertyModifications.ToArray());
				}
			}

			foreach (Transform childTransform in _originalAsset.transform)
			{
				var child = childTransform.gameObject;
				var clonedChild = FindMatchingObject(_clonedRoot, child);
				if (clonedChild != null )
					CloneOverrides(child, clonedChild, _clonedRoot);
			}
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
			_variantName = $"{basename}{extension}";
			_variantPath = $"{_targetDir}/{_variantName}";
			return asset;
		}

		private static void CleanUp()
		{
			s_variantRecords.Clear();
			s_baseByPrefab.Clear();
			s_originals.Clear();
			s_clones.Clear();
			s_clonesByOriginals.Clear();
			s_originalsByClones.Clear();
			foreach (var gameObject in s_objectsToDelete)
				gameObject.SafeDestroy();
			s_objectsToDelete.Clear();
		}
	}
}