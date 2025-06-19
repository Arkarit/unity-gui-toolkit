using System.Collections.Generic;
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

		public static void SortByPrefabHierarchy(List<GameObject> _gameObjectList)
		{
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
							containsUnhandledPrefabs = true;
							break;
						}
					}

					if (containsUnhandledPrefabs)
						continue;

					done.Add(current);
					toDo.RemoveAt(i);
					transforms.RemoveAt(i);
					break;
				}
			}

			_gameObjectList.Clear();
			_gameObjectList.AddRange(done);
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
	}
}