using NUnit.Framework;
using UnityEngine;
using UnityEditor;
using System.Linq;
using GuiToolkit.Editor;
using System.Collections.Generic;
using GuiToolkit.Debugging;
using System;

namespace GuiToolkit.Test
{
	public class TestEditorAssetUtility
	{
		[Test]
		public void TestSortByPrefabHierarchy()
		{
			Assert.DoesNotThrow(() => EditorAssetUtility.SortByPrefabHierarchyGuids(new List<string>()));
			Assert.DoesNotThrow(() => EditorAssetUtility.SortByPrefabHierarchyAssetPaths(new List<string>()));
			Assert.DoesNotThrow(() => EditorAssetUtility.SortByPrefabHierarchy(new List<GameObject>()));
			Assert.Throws<ArgumentNullException>(() => EditorAssetUtility.SortByPrefabHierarchyGuids(null));
			Assert.Throws<ArgumentNullException>(() => EditorAssetUtility.SortByPrefabHierarchyAssetPaths(null));
			Assert.Throws<ArgumentNullException>(() => EditorAssetUtility.SortByPrefabHierarchy(null));
			Assert.Throws<ArgumentNullException>(() => EditorAssetUtility.SortByPrefabHierarchyGuids(new List<string>() {null}));
			Assert.Throws<ArgumentNullException>(() => EditorAssetUtility.SortByPrefabHierarchyAssetPaths(new List<string>() {null}));
			Assert.Throws<ArgumentNullException>(() => EditorAssetUtility.SortByPrefabHierarchyGuids(new List<string>() {"not a valid guid"}));
			Assert.Throws<ArgumentNullException>(() => EditorAssetUtility.SortByPrefabHierarchyAssetPaths(new List<string>() {"not a valid path"}));
			Assert.Throws<ArgumentNullException>(() => EditorAssetUtility.SortByPrefabHierarchy(new List<GameObject>() {null}));

			GameObject notAPrefab = new GameObject();
			Assert.Throws<ArgumentException>(() => EditorAssetUtility.SortByPrefabHierarchy(new List<GameObject>() {notAPrefab} ));
			UnityEngine.Object.DestroyImmediate(notAPrefab );

			var paths = TestData.Instance.SortByPrefabHierarchyPaths;


			foreach (var pathField in paths)
			{
				string path = pathField;
				Debug.Log($"Testing path '{path}'");
				var guids = AssetDatabase.FindAssets("t:prefab", new[] { path }).ToList();
				Debug.Log(DebugUtility.GetAllGuidsString(guids, "Before sorting:"));
				EditorAssetUtility.SortByPrefabHierarchyGuids(guids);
				Debug.Log(DebugUtility.GetAllGuidsString(guids, "After sorting:"));
				AssertSortingOrder(guids);
			}
		}

		private void AssertSortingOrder( List<string> _sortedGuids )
		{
			var currentPaths = _sortedGuids.Select(AssetDatabase.GUIDToAssetPath);
			var toDo = currentPaths.Select(AssetDatabase.LoadAssetAtPath<GameObject>).ToHashSet();
			var all = new HashSet<GameObject>(toDo);
			var done = new HashSet<GameObject>();

			for (int i = 0; i < _sortedGuids.Count; i++)
			{
				var currentPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(_sortedGuids[i]));

				// Ensure that the prefab does not contain any children which haven't been handled yet
				// (if they are in the list at all)
				AssertDoesNotContainUnhandledChildren(currentPrefab);

				// Ensure that the prefab is based on a prefab which already was handled (lower index in list)
				// or is based on a prefab outside of the list
				if (PrefabUtility.IsPartOfVariantPrefab(currentPrefab))
					AssertPrefabValidity(currentPrefab, $"(list index {i}) is based on");

				done.Add(currentPrefab);
				toDo.Remove(currentPrefab);
			}

			void AssertDoesNotContainUnhandledChildren(GameObject _go)
			{
				var transforms = _go.GetComponentsInChildren<Transform>();
				foreach (Transform t in transforms)
				{
					if (t == _go.transform)
						continue;

					GameObject currentPrefab = t.gameObject;
					if (!PrefabUtility.IsAnyPrefabInstanceRoot(currentPrefab))
						continue;

					AssertPrefabValidity(currentPrefab, "has child based on");
				}
			}

			void AssertPrefabValidity(GameObject _currentPrefab, string _text)
			{
				var basePrefab = PrefabUtility.GetCorrespondingObjectFromSource(_currentPrefab);
				var currentPath = AssetDatabase.GetAssetPath(_currentPrefab);
				var basePath = AssetDatabase.GetAssetPath(basePrefab);

				Assert.IsTrue(!all.Contains(basePrefab) || done.Contains(basePrefab), 
					$"Asset\n\t'{currentPath}'\n{_text}\n\t'{basePath}'\n, which hasn't been handled yet");
			}
		}
	}
}
