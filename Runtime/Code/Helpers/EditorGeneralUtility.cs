#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;


namespace GuiToolkit
{
	public static class EditorGeneralUtility
	{
		public static void RemoveArrayElementAtIndex( SerializedProperty _list, int _idx )
		{
			if (!ValidateListAndIndex(_list, _idx))
				return;

			for (int i = _idx; i < _list.arraySize - 1; i++)
				_list.MoveArrayElement(i + 1, i);
			_list.arraySize--;
		}

		public static void SwapArrayElement( SerializedProperty _list, int _idx1, int _idx2 )
		{
			if (_idx1 == _idx2 || !ValidateListAndIndex(_list, _idx1) || !ValidateListAndIndex(_list, _idx2))
				return;
			SerializedProperty pt = _list.GetArrayElementAtIndex(_idx1);
			_list.MoveArrayElement(_idx2, _idx1);
			SerializedProperty p = _list.GetArrayElementAtIndex(_idx2);
			p = pt;
		}

		public static bool AreMultipleBitsSet( int _n )
		{
			return (_n & (_n - 1)) != 0;
		}

		// Supports interfaces
		// Caution! Clear result before usage!
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
			List<T> result = new ();
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


		private static bool ValidateListAndIndex( SerializedProperty _list, int _idx )
		{
			if (!ValidateList(_list))
				return false;

			return ValidateListIndex(_idx, _list);
		}

		private static bool ValidateList( SerializedProperty _list )
		{
			if (!_list.isArray)
			{
				Debug.LogError("Attempt to access array element from a SerializedProperty which isn't an array");
				return false;
			}
			return true;
		}

		private static bool ValidateListIndex( int _idx, SerializedProperty _list )
		{
			if (_idx < 0 || _idx >= _list.arraySize)
			{
				Debug.LogError("Out of Bounds when accessing an array element");
				return false;
			}
			return true;
		}
	}
}

#endif
