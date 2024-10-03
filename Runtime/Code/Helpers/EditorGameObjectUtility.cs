#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;


namespace GuiToolkit
{
	public static class EditorGameObjectUtility
	{
		public static bool IsInPrefabEditingMode => PrefabStageUtility.GetCurrentPrefabStage() != null;

		// Workaround for the completely idiotic (and insanely named) PrefabUtility functions, which returns funny and completely differing values
		// depending if you are currently editing a prefab or have it selected in the Project or Hierarchy window.
		// This function sorts out all of these circumstances, and returns either when:
		// rootPrefab == false : the topmost variant if variants exist, the root prefab if no variant exists, or null if _gameObject is not a prefab
		// rootPrefab == true : the root prefab or null if _gameObject is not a prefab
		public static GameObject GetPrefab( GameObject _gameObject, bool _rootPrefab = false )
		{
			// _gameObject is a prefab root being edited.
			if (IsEditingPrefab(_gameObject) && IsRoot(_gameObject))
			{
				GameObject editedPrefab = GetEditedPrefab(_gameObject);
				return _rootPrefab ? PrefabUtility.GetCorrespondingObjectFromOriginalSource(_gameObject) : editedPrefab;
			}

			// _gameObject is a prefab asset selected in the project window
			if (_gameObject.scene.name == null)
				return _rootPrefab ? PrefabUtility.GetCorrespondingObjectFromOriginalSource(_gameObject) : _gameObject;

			// _gameObject is a prefab instance either in the prefab editor or hierarchy window
			if (PrefabUtility.IsAnyPrefabInstanceRoot(_gameObject))
			{
				if (_rootPrefab)
					return PrefabUtility.GetCorrespondingObjectFromOriginalSource(_gameObject);
				string path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(_gameObject);
				if (!string.IsNullOrEmpty(path))
					return AssetDatabase.LoadAssetAtPath<GameObject>(path);
			}

			// _gameObject is not a prefab
			return null;
		}

		public static bool IsNotAttachedPrefab(GameObject _gameObject) => PrefabUtility.GetNearestPrefabInstanceRoot(_gameObject) == null;

		public static bool IsEditingPrefab( GameObject _gameObject, bool _onlyDirectPrefab = false )
		{
			var prefabStage = PrefabStageUtility.GetPrefabStage(_gameObject);
			if (prefabStage == null)
				return false;

			if (!_onlyDirectPrefab)
				return true;

			// Only if we are actually editing the direct prefab our _gameObject resides in, the nearest prefab instance root is null
			return IsNotAttachedPrefab(_gameObject);
		}

		public static GameObject GetEditedPrefab( GameObject _gameObject )
		{
			PrefabStage stage = PrefabStageUtility.GetPrefabStage(_gameObject);
			if (stage == null)
				return null;
			string path = stage.assetPath;
			return AssetDatabase.LoadAssetAtPath<GameObject>(path);
		}

		public static bool IsPrefab( GameObject _gameObject )
		{
			if (_gameObject == null)
				return false;
			return _gameObject.scene.name == null;
		}

		public static bool InfoBoxIfPrefab( GameObject _gameObject )
		{
			if (!IsPrefab(_gameObject))
				return false;
			EditorGUILayout.HelpBox("Please open Prefab Asset to edit", MessageType.Info);
			return true;
		}

		public static bool IsSceneObject( GameObject _gameObject )
		{
			if (_gameObject == null)
				return false;
			return _gameObject.scene.name != null;
		}

		public static bool IsRoot( GameObject _gameObject )
		{
			if (IsEditingPrefab(_gameObject))
				return _gameObject.transform.parent != null && _gameObject.transform.parent.parent == null;
			else
				return _gameObject.transform.parent == null;
		}

		public static bool HasAnyLabel<T>( GameObject _gameObject, T _labelsToFind ) where T : ICollection<string>
		{
			string[] currentLabels = AssetDatabase.GetLabels(_gameObject);
			List<string> newLabels = new List<string>();
			foreach (string label in currentLabels)
				if (_labelsToFind.Contains(label))
					return true;
			return false;
		}

		public static bool HasLabel( GameObject _gameObject, string _labelToFind )
		{
			string[] currentLabels = AssetDatabase.GetLabels(_gameObject);
			foreach (string label in currentLabels)
				if (label == _labelToFind)
					return true;
			return false;
		}

		public static void RemoveLabels<T>( GameObject _gameObject, T _labelsToRemove ) where T : ICollection<string>
		{
			string[] currentLabels = AssetDatabase.GetLabels(_gameObject);
			List<string> newLabels = new List<string>();
			foreach (string label in currentLabels)
				if (!_labelsToRemove.Contains(label))
					newLabels.Add(label);
			AssetDatabase.SetLabels(_gameObject, newLabels.ToArray());
		}

		public static void RemoveLabel( GameObject _gameObject, string _label )
		{
			RemoveLabels(_gameObject, new HashSet<string>() { _label });
		}

		public static void AddLabels<T>( GameObject _gameObject, T _labelsToAdd ) where T : ICollection<string>
		{
			string[] labelsArr = AssetDatabase.GetLabels(_gameObject);
			List<string> labels = new List<string>(labelsArr);
			foreach (string labelToAdd in _labelsToAdd)
			{
				if (!labels.Contains(labelToAdd))
					labels.Add(labelToAdd);
			}
			AssetDatabase.SetLabels(_gameObject, labels.ToArray());
		}

		public static void AddLabel( GameObject _gameObject, string _label )
		{
			AddLabels(_gameObject, new HashSet<string>() { _label });
		}

		public static string GetPathInScene(GameObject _gameObject)
		{
			string result = _gameObject.name;

			while (_gameObject.transform.parent)
			{
				_gameObject = _gameObject.transform.parent.gameObject;
				result = $"{_gameObject.name}/{result}";
			}

			return result;
		}

	}
}

#endif
