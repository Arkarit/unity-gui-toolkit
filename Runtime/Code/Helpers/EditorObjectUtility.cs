using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEditor;
using UnityEngine;
using System;
using UnityEngine.SceneManagement;

namespace GuiToolkit
{
	/// \brief General Object helper Editor Utility
	/// 
	/// This is a collection of common editor object helper functions.
	/// 
	/// Note: This file must reside outside of an "Editor" folder, since it must be accessible
	/// from mixed game/editor classes (even though all accesses are in #if UNITY_EDITOR clauses)
	/// See https://answers.unity.com/questions/426184/acces-script-in-the-editor-folder.html for reasons.
	public static class EditorObjectUtility
	{
		public static bool IsEditingPrefab( GameObject _go )
		{
			return PrefabStageUtility.GetPrefabStage(_go) != null || _go.scene.name == null;
		}

		public static GameObject GetEditedPrefab( GameObject _go )
		{
			PrefabStage stage = PrefabStageUtility.GetPrefabStage(_go);
			if (stage == null)
				return null;
			string path = stage.prefabAssetPath;
			return AssetDatabase.LoadAssetAtPath<GameObject>(path);
		}

		public static bool IsPrefab( GameObject _go )
		{
			if (_go == null)
				return false;
			return _go.scene.name == null;
		}

		public static bool IsSceneObject( GameObject _go )
		{
			if (_go == null)
				return false;
			return _go.scene.name != null;
		}

		public static bool IsRoot( GameObject _go )
		{
			if (IsEditingPrefab(_go))
				return _go.transform.parent != null && _go.transform.parent.parent == null;
			else
				return _go.transform.parent == null;
		}

		public static bool HasAnyLabel<T>( GameObject _go, T _labelsToFind ) where T : ICollection<string>
		{
			string[] currentLabels = AssetDatabase.GetLabels(_go);
			List<string> newLabels = new List<string>();
			foreach (string label in currentLabels)
				if (_labelsToFind.Contains(label))
					return true;
			return false;
		}

		public static bool HasLabel( GameObject _go, string _labelToFind )
		{
			string[] currentLabels = AssetDatabase.GetLabels(_go);
			foreach (string label in currentLabels)
				if (label == _labelToFind)
					return true;
			return false;
		}

		public static void RemoveLabels<T>( GameObject _go, T _labelsToRemove ) where T : ICollection<string>
		{
			string[] currentLabels = AssetDatabase.GetLabels(_go);
			List<string> newLabels = new List<string>();
			foreach (string label in currentLabels)
				if (!_labelsToRemove.Contains(label))
					newLabels.Add(label);
			AssetDatabase.SetLabels(_go, newLabels.ToArray());
		}

		public static void RemoveLabel( GameObject _go, string _label )
		{
			RemoveLabels(_go, new HashSet<string>() { _label });
		}

		public static void AddLabels<T>( GameObject _go, T _labelsToAdd ) where T : ICollection<string>
		{
			string[] labelsArr = AssetDatabase.GetLabels(_go);
			List<string> labels = new List<string>(labelsArr);
			foreach (string labelToAdd in _labelsToAdd)
			{
				if (!labels.Contains(labelToAdd))
					labels.Add(labelToAdd);
			}
			AssetDatabase.SetLabels(_go, labels.ToArray());
		}

		public static void AddLabel( GameObject _go, string _label )
		{
			AddLabels(_go, new HashSet<string>() { _label });
		}

		// Workaround for the completely idiotic (and insanely named) PrefabUtility functions, which returns funny and completely differing values
		// depending if you are currently editing a prefab or have it selected in the Project or Hierarchy window.
		// This function sorts out all of these circumstances, and returns either when:
		// _rootPrefab == false : the topmost variant if variants exist, the root prefab if no variant exists, or null if _go is not a prefab
		// _rootPrefab == true : the root prefab or null if _go is not a prefab
		public static GameObject GetPrefab( GameObject _go, bool _rootPrefab = false )
		{
			// _go is a prefab root being edited.
			if (IsEditingPrefab(_go) && IsRoot(_go))
			{
				GameObject go = GetEditedPrefab(_go);
				return _rootPrefab ? PrefabUtility.GetCorrespondingObjectFromOriginalSource(_go) : go;
			}

			// _go is a prefab asset selected in the project window
			if (_go.scene.name == null)
				return _rootPrefab ? PrefabUtility.GetCorrespondingObjectFromOriginalSource(_go) : _go;

			// _go is a prefab instance either in the prefab editor or hierarchy window
			if (PrefabUtility.IsAnyPrefabInstanceRoot(_go))
			{
				if (_rootPrefab)
					return PrefabUtility.GetCorrespondingObjectFromOriginalSource(_go);
				string path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(_go);
				if (!string.IsNullOrEmpty(path))
					return AssetDatabase.LoadAssetAtPath<GameObject>(path);
			}

			// _go is not a prefab
			return null;
		}

				public delegate void AssetFoundDelegate<T>(T _component);

		public static void FindAllComponentsInAllPrefabs<T>(AssetFoundDelegate<T> _foundFn, bool _includeInactive = true)
		{
			string[] allAssetPathGuids = AssetDatabase.FindAssets("t:GameObject");

			foreach (string guid in allAssetPathGuids)
			{
				string assetPath = AssetDatabase.GUIDToAssetPath(guid);
				GameObject go = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
				T[] components = go.GetComponentsInChildren<T>(_includeInactive);
				if (components == null || components.Length == 0)
					continue;

				foreach (T component in components)
					_foundFn(component);
			}
		}

		public static void FindAllComponentsInAllScriptableObjects<T>(AssetFoundDelegate<T> _foundFn)
		{
			string[] allAssetPathGuids = AssetDatabase.FindAssets("t:ScriptableObject");

			foreach (string guid in allAssetPathGuids)
			{
				string assetPath = AssetDatabase.GUIDToAssetPath(guid);
				ScriptableObject scriptableObject = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);
				if (scriptableObject == null || !(scriptableObject is T))
					continue;

				_foundFn( (T)(object) scriptableObject);
			}
		}

		public static void FindAllComponentsInAllScenes<T>(AssetFoundDelegate<T> _foundFn, bool _includeInactive = true)
		{
			string[] allAssetPathGuids = AssetDatabase.FindAssets("t:Scene");

			foreach (string guid in allAssetPathGuids)
			{
				string assetPath = AssetDatabase.GUIDToAssetPath(guid);
				Scene scene = EditorSceneManager.GetSceneByPath(assetPath);
				bool wasLoaded = scene.isLoaded;
				if (!wasLoaded)
					scene = EditorSceneManager.OpenScene(assetPath, OpenSceneMode.Additive);

				GameObject[] roots = scene.GetRootGameObjects();
				foreach(GameObject root in roots)
				{
					T[] components = root.GetComponentsInChildren<T>(_includeInactive);
					if (components == null || components.Length == 0)
						continue;

					foreach (T component in components)
						_foundFn(component);
				}

				if (!wasLoaded)
					EditorSceneManager.CloseScene(scene, true);
			}
		}

		public static void FindAllComponentsInAllAssets<T>(AssetFoundDelegate<T> _foundFn, bool _includeInactive = true)
		{
			FindAllComponentsInAllScenes(_foundFn, _includeInactive);
			FindAllComponentsInAllPrefabs(_foundFn, _includeInactive);
			FindAllComponentsInAllScriptableObjects(_foundFn);
		}

		public delegate void ScriptFoundDelegate(string path, string _content);

		public static void FindAllScripts(ScriptFoundDelegate _foundFn, bool _excludePackages = true)
		{
			string[] allAssetPathGuids = AssetDatabase.FindAssets("t:Script");

			foreach (string guid in allAssetPathGuids)
			{
				string assetPath = AssetDatabase.GUIDToAssetPath(guid);

				if (_excludePackages && assetPath.StartsWith("Packages/", StringComparison.OrdinalIgnoreCase))
					continue;

				TextAsset textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(assetPath);
				if (textAsset == null)
					continue;

				_foundFn( assetPath, textAsset.text );
			}
		}

		public static int FindAllScriptsCount(bool _excludePackages = true)
		{
			string[] allAssetPathGuids = AssetDatabase.FindAssets("t:Script");

			int result = 0;
			foreach (string guid in allAssetPathGuids)
			{
				string assetPath = AssetDatabase.GUIDToAssetPath(guid);

				if (_excludePackages && assetPath.StartsWith("Packages/", StringComparison.OrdinalIgnoreCase))
					continue;

				result++;
			}
			return result;
		}

		// Supports interfaces
		// Caution! Clear _result before usage!
		// (It is not cleared here on purpose, to be able to do multiple FindObjectsOfType() after another)
		public static void FindObjectsOfType<T>( List<T> _result, Scene _scene, bool _includeInactive = true)
		{
			GameObject[] roots = _scene.GetRootGameObjects();
			foreach(GameObject root in roots)
			{
				T[] components = root.GetComponentsInChildren<T>(_includeInactive);
				if (components == null || components.Length == 0)
					continue;

				foreach (T component in components)
					_result.Add(component);
			}
		}

		public static List<T> FindObjectsOfType<T>(Scene _scene, bool _includeInactive = true)
		{
			List<T> result = new List<T>();
			FindObjectsOfType<T>(result, _scene, _includeInactive);
			return result;
		}

		public static void FindObjectsOfType<T>( List<T> _result, bool _includeInactive = true)
		{
			_result.Clear();
			for (int i=0; i<SceneManager.loadedSceneCount; i++)
			{
				Scene scene = EditorSceneManager.GetSceneAt(i);
				FindObjectsOfType<T>(_result, scene, _includeInactive);
			}
		}

		public static List<T> FindObjectsOfType<T>(bool _includeInactive = true)
		{
			List<T> result = new List<T>();
			FindObjectsOfType<T>(result, _includeInactive);
			return result;
		}
	}
}
