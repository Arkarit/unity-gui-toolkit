using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace GuiToolkit.Editor
{
	public static class EditorExtensions
	{
		public static IEnumerable<SerializedProperty> GetVisibleChildren( this SerializedProperty _serializedProperty, bool _hideScript = true )
		{
			SerializedProperty currentProperty = _serializedProperty.Copy();

			if (currentProperty.NextVisible(true))
			{
				do
				{
					if (_hideScript && currentProperty.name == "m_Script")
						continue;

					yield return currentProperty;
				}
				while (currentProperty.NextVisible(false));
			}
		}

		public static void DisplayProperties( this SerializedObject _this )
		{
			var props = _this.GetIterator().GetVisibleChildren();
			foreach (var prop in props)
				EditorGUILayout.PropertyField(prop, true);
		}

		/// <summary>
		/// This instantiates a game object similar to Object.Instantiate()
		/// The crucial difference between this and Object.Instantiate() is, that Object.Instantiate() unpacks all prefabs.
		/// InstantiatePrefabAware() rebuilds the whole hierarchy instead and keeps all prefabs.
		/// </summary>
		/// <param name="_gameObject"></param>
		/// <param name="_newParent"></param>
		/// <returns></returns>
		public static GameObject InstantiatePrefabAware(this GameObject _gameObject, Transform _newParent = null, bool _keepName = true)
		{
			return InstantiatePrefabAwareInternal(_gameObject, null, _newParent, _keepName);
		}

		private static GameObject InstantiatePrefabAwareInternal(GameObject _gameObject, GameObject _clonedRoot, Transform _newParent, bool _keepName)
		{
			if (_gameObject == null)
				return null;

			bool isRoot = _clonedRoot == null;
			GameObject result;
			GameObject alreadyExistingInClone = !isRoot ? EditorAssetUtility.FindMatchingGameObject(_clonedRoot, _gameObject) : null;

			if (alreadyExistingInClone != null)
			{
				result = alreadyExistingInClone;
			}
			else if (PrefabUtility.IsAnyPrefabInstanceRoot(_gameObject))
			{
				var prefabToClone = PrefabUtility.GetCorrespondingObjectFromOriginalSource(_gameObject);
				result = (GameObject) PrefabUtility.InstantiatePrefab(prefabToClone, _newParent);
				var targetTransform = result.transform;
				var sourceTransform = _gameObject.transform;
				targetTransform.position = sourceTransform.position;
				targetTransform.rotation = sourceTransform.rotation;
				targetTransform.localScale = sourceTransform.localScale;
				
				TransferOverridesAddedAndRemoved(_gameObject, result);
			}
			else
			{
				result = _gameObject.CloneWithoutChildren(_newParent);
			}

			if (isRoot)
				_clonedRoot = result;

			if (_keepName)
				result.name = _gameObject.name;

			foreach (Transform child in _gameObject.transform)
				InstantiatePrefabAwareInternal(child.gameObject, _clonedRoot, result != null ? result.transform : null, _keepName);

			if (isRoot)
				FixOverrides(_gameObject, _clonedRoot, _clonedRoot);

			return result;
		}

		private static void FixOverrides(GameObject _originalRoot, GameObject _clonedRoot, GameObject _currentClonedGameObject)
		{
			bool isRoot = _clonedRoot == _currentClonedGameObject;

			if (PrefabUtility.IsAnyPrefabInstanceRoot(_currentClonedGameObject))
			{
				var overrideInfo = GetOverrideInfo(_currentClonedGameObject);

				foreach (var propertyModification in overrideInfo.PropertyModifications)
				{
					var foundObj = EditorAssetUtility.FindMatchingObject(_clonedRoot, propertyModification.objectReference);
					if (foundObj != null)
						propertyModification.objectReference = foundObj;
				}

				SetOverrideInfo(_currentClonedGameObject, overrideInfo);
			}

			foreach (Transform child in _currentClonedGameObject.transform)
				FixOverrides(_originalRoot, _clonedRoot, child.gameObject);

			if (isRoot)
			{
				//?
			}
		}

		struct OverrideInfo
		{
			public bool IsPrefab;
			public List<PropertyModification> PropertyModifications;
			public List<AddedGameObject> AddedGameObjects;
			public List<RemovedGameObject> RemovedGameObjects;
			public List<AddedComponent> AddedComponents;
			public List<RemovedComponent> RemovedComponents;
		}
		
		/// <summary>
		/// Transfers overrides from one game object to another.
		/// Note that references are NOT corrected in this step; this is
		/// due to the hierarchical cloning; the objects the references point to might not yet exist.
		/// </summary>
		/// <param name="_gameObject"></param>
		/// <param name="_clonedGameObject"></param>
		private static void TransferOverridesAddedAndRemoved(GameObject _gameObject, GameObject _clonedGameObject)
		{
			var overrideInfo = GetOverrideInfo(_gameObject);
			if (!overrideInfo.IsPrefab)
				return;

			SetOverrideInfo(_clonedGameObject, overrideInfo);
		}
		
		private static OverrideInfo GetOverrideInfo(GameObject _gameObject)
		{
			if (_gameObject == null)
				throw new ArgumentNullException("GameObject mustn't be null");

			bool isPrefab = PrefabUtility.IsPartOfAnyPrefab(_gameObject);
			var outermostRoot = isPrefab ? PrefabUtility.GetOutermostPrefabInstanceRoot(_gameObject) : null;
			
			return new OverrideInfo()
			{
				IsPrefab = isPrefab,

				PropertyModifications = isPrefab ? PrefabUtility.GetPropertyModifications(outermostRoot).ToList() : new List<PropertyModification>(),
				AddedGameObjects = isPrefab ? PrefabUtility.GetAddedGameObjects(outermostRoot) : new List<AddedGameObject>(),
				RemovedGameObjects = isPrefab ? PrefabUtility.GetRemovedGameObjects(outermostRoot) : new List<RemovedGameObject>(),
				AddedComponents = isPrefab ? PrefabUtility.GetAddedComponents(outermostRoot) : new List<AddedComponent>(),
				RemovedComponents = isPrefab ? PrefabUtility.GetRemovedComponents(outermostRoot) : new List<RemovedComponent>()
			};
		}
		
		private static bool SetOverrideInfo(GameObject _gameObject, OverrideInfo _overrideInfo)
		{
			if (!_overrideInfo.IsPrefab)
				return false;

			if (_gameObject == null)
				throw new ArgumentNullException("GameObject mustn't be null");

			bool isPrefab = PrefabUtility.IsPartOfAnyPrefab(_gameObject);
			if (!isPrefab)
				return false;

			var outermostRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(_gameObject);
			if (outermostRoot == null)
				return false;

			bool hasOverrides =
				_overrideInfo.PropertyModifications.Count > 0
				|| _overrideInfo.RemovedGameObjects.Count > 0
				|| _overrideInfo.AddedGameObjects.Count > 0
				|| _overrideInfo.RemovedComponents.Count > 0
				|| _overrideInfo.AddedComponents.Count > 0;

			if (!hasOverrides)
				return false;

			foreach (var removedGameObject in _overrideInfo.RemovedGameObjects)
				removedGameObject.assetGameObject.SafeDestroy();

			foreach (var addedGameObject in _overrideInfo.AddedGameObjects)
			{
				var go = addedGameObject.instanceGameObject.InstantiatePrefabAware(_gameObject.transform);
				go.transform.SetSiblingIndex(addedGameObject.siblingIndex);
			}

			foreach (var removedComponent in _overrideInfo.RemovedComponents)
				removedComponent.assetComponent.SafeDestroy();

			foreach (var addedComponent in _overrideInfo.AddedComponents)
			{
				var clonedComponent = _gameObject.AddComponent(addedComponent.instanceComponent.GetType());
				EditorUtility.CopySerializedManagedFieldsOnly(addedComponent.instanceComponent, clonedComponent);
			}

			PrefabUtility.SetPropertyModifications(outermostRoot, _overrideInfo.PropertyModifications.ToArray());

			return true;
		}
	}
}
