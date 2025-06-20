using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Editor
{
	public static partial class EditorAssetUtility
	{
		public const string PrefabFolder = "Prefabs/";
		
		public static string BuiltinPrefabDir
		{
			get
			{
				string rootProjectDir = UiToolkitConfiguration.Instance.GetUiToolkitRootProjectDir();
				return rootProjectDir + PrefabFolder;
			}
		}

		/// <summary>
		/// Sorts a list of GameObjects based on their prefab hierarchy.
		/// This method ensures that GameObjects are ordered according to the hierarchy
		/// of their corresponding prefab variants, from base prefab to any variants.
		/// It checks that each GameObject is part of a prefab and that prefabs in
		/// the hierarchy are handled before sorting the GameObject itself.
		/// </summary>
		/// <param name="_gameObjectList">The list of GameObjects to be sorted.</param>
		/// <exception cref="ArgumentNullException">
		/// Thrown when the provided list of GameObjects is null or any GameObject in the list is null.
		/// </exception>
		/// <exception cref="ArgumentException">
		/// Thrown when any GameObject in the list is not part of a prefab.
		/// </exception>
		public static void SortByPrefabHierarchy(List<GameObject> _gameObjectList)
		{
			ValidateList(_gameObjectList);
			ValidateArguments(_gameObjectList);

			List<GameObject> toDo = new List<GameObject>(_gameObjectList);
			List<GameObject> done = new List<GameObject>(); // List to hold sorted GameObjects

			List<Transform[]> transformsCache = new();
			foreach (var gameObject in toDo)
				transformsCache.Add(gameObject.GetComponentsInChildren<Transform>());

			while (toDo.Count > 0)
			{
				// Iterate through the list in reverse order to avoid issues when removing items
				for (int i = toDo.Count - 1; i >= 0; i--)
				{
					GameObject current = toDo[i];
					var transforms = transformsCache[i];

					// Skip the current GameObject if it has unprocessed prefabs in its hierarchy
					if (ContainsUnhandledPrefabs(transforms, current))
						continue;

					if (PrefabUtility.IsPartOfVariantPrefab(current))
					{
						var basePrefab = PrefabUtility.GetCorrespondingObjectFromSource(current);

						// Skip if the base prefab is still in the "to-do" list
						if (toDo.Contains(basePrefab))
							continue;
					}

					// Mark this GameObject as done, and remove it from the "to-do" list and also remove transform from transform cache
					done.Add(current);
					toDo.RemoveAt(i);
					transformsCache.RemoveAt(i);
				}
			}

			_gameObjectList.Clear();
			_gameObjectList.AddRange(done);

			// Helper function to check if a GameObject has unprocessed prefabs in its hierarchy
			bool ContainsUnhandledPrefabs(Transform[] _transformArray, GameObject _gameObject)
			{
				foreach (var t in _transformArray)
				{
					GameObject currentDescendant = t.gameObject;

					if (currentDescendant == _gameObject)
						continue;

					if (!PrefabUtility.IsAnyPrefabInstanceRoot(currentDescendant))
						continue;

					// Get the original prefab and check if it is still in the "to-do" list
					var prefab = PrefabUtility.GetCorrespondingObjectFromOriginalSource(currentDescendant);
					if (toDo.Contains(prefab))
						return true;
				}

				return false;
			}

			void ValidateArguments(List<GameObject> _gameObjectList)
			{
				for (var i = 0; i < _gameObjectList.Count; i++)
				{
					var gameObject = _gameObjectList[i];
					if (!PrefabUtility.IsPartOfAnyPrefab(gameObject))
						throw new ArgumentException(
							$"game object at index {i} of {nameof(_gameObjectList)} ('{gameObject.name}') is not a prefab");
				}
			}
		}

		public static void SortByPrefabHierarchyAssetPath(List<string> _pathList)
		{
			ValidateList(_pathList);
			List<GameObject> assets = _pathList.Select(AssetDatabase.LoadAssetAtPath<GameObject>).ToList();
			ValidateList(assets, "; game object could not be loaded");
			SortByPrefabHierarchy(assets);
			_pathList.Clear();
			foreach (var gameObject in assets)
				_pathList.Add(AssetDatabase.GetAssetPath(gameObject));
		}

		public static void SortByPrefabHierarchyGuids(List<string> _guidList)
		{
			ValidateList(_guidList);
			var pathList = _guidList.Select(AssetDatabase.GUIDToAssetPath).ToList();
			ValidateList(pathList, "; seems not to be a valid GUID");
			SortByPrefabHierarchyAssetPath(pathList);

			_guidList.Clear();
			foreach (var path in pathList)
				_guidList.Add(AssetDatabase.AssetPathToGUID(path));
		}

		private static void ValidateList<T>(List<T> _list, string _postfix = null)
		{
			if (_list == null)
				throw new ArgumentNullException($"List argument mustn't be null");

			for (var i = 0; i < _list.Count; i++)
			{
				var gameObject = _list[i];

				if (gameObject == null)
					throw new ArgumentNullException($"List element at index {i} is null{_postfix}");
			}
		}

	}
}