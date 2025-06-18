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

		struct OverrideInfo
		{
			public GameObject OutermostRoot;
			public List<PropertyModification> PropertyModifications;
			public List<AddedGameObject> AddedGameObjects;
			public List<RemovedGameObject> RemovedGameObjects;
			public List<AddedComponent> AddedComponents;
			public List<RemovedComponent> RemovedComponents;
		}
		
		private static GameObject InstantiatePrefabAwareInternal(GameObject _gameObject, GameObject _clonedRoot, Transform _newParent, bool _keepName)
		{
			if (_gameObject == null)
				return null;

			GameObject result;
			GameObject alreadyExistingInClone = _clonedRoot != null ? EditorAssetUtility.FindMatchingGameObject(_clonedRoot, _gameObject) : null;

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

			if (_clonedRoot == null)
				_clonedRoot = result;

			if (_keepName)
				result.name = _gameObject.name;

			foreach (Transform child in _gameObject.transform)
				InstantiatePrefabAwareInternal(child.gameObject, _clonedRoot, result != null ? result.transform : null, _keepName);

			return result;
		}
		
		private static void TransferOverridesAddedAndRemoved(GameObject _gameObject, GameObject _clonedGameObject)
		{
			
		}
		
		private static OverrideInfo GetOverrideInfo(GameObject _gameObject)
		{
			bool isPrefab = PrefabUtility.IsPartOfAnyPrefab(_gameObject);
			var outermostRoot = isPrefab ? PrefabUtility.GetOutermostPrefabInstanceRoot(_gameObject) : null;
			
			return new OverrideInfo()
			{
				OutermostRoot = outermostRoot,
				PropertyModifications = isPrefab ? PrefabUtility.GetPropertyModifications(outermostRoot).ToList() : new List<PropertyModification>(),
				AddedGameObjects = isPrefab ? PrefabUtility.GetAddedGameObjects(outermostRoot) : new List<AddedGameObject>(),
				RemovedGameObjects = isPrefab ? PrefabUtility.GetRemovedGameObjects(outermostRoot) : new List<RemovedGameObject>(),
				AddedComponents = isPrefab ? PrefabUtility.GetAddedComponents(outermostRoot) : new List<AddedComponent>(),
				RemovedComponents = isPrefab ? PrefabUtility.GetRemovedComponents(outermostRoot) : new List<RemovedComponent>()
			};
		}
		
		private static bool SetOverrideInfo(GameObject _gameObject, OverrideInfo _overrideInfo)
		{
			
			return true;
		}
	}
}
