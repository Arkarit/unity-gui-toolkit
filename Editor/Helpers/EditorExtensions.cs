using System.Collections.Generic;
using UnityEditor;
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
		/// This clones a whole game object hierarchy, while keeping all prefab structures
		/// </summary>
		/// <param name="_gameObject"></param>
		/// <param name="_newParent"></param>
		/// <returns></returns>
		public static GameObject PrefabAwareClone(this GameObject _gameObject, Transform _newParent = null)
		{
			return PrefabAwareCloneInternal(_gameObject, null, _newParent);
		}

		private static GameObject PrefabAwareCloneInternal(GameObject _gameObject, GameObject _clonedRoot, Transform _newParent = null)
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
			}
			else
			{
				result = _gameObject.CloneWithoutChildren(_newParent);
			}

			if (_clonedRoot == null)
				_clonedRoot = result;

			foreach (Transform child in _gameObject.transform)
				PrefabAwareCloneInternal(child.gameObject, _clonedRoot, result != null ? result.transform : null);

			return result;
		}
	}
}
