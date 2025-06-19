using System.Linq;
using GuiToolkit.Debugging;
using GuiToolkit.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Test
{
	public class TestEditorAssetUtility
	{

		[SetUp]
		public void SetUpSortByPrefabHierarchy()
		{
		}

		/// <summary>
		/// Test the sort order of prefabs by their variant hierarchy
		/// Sorting order should be (only as example:)
		/// 1 A: Regular Prefab
		/// 2 B: Regular Prefab
		/// 3 A Variant: Variant of A
		/// 4 B Variant: Variant of B
		/// 5 B Variant Variant: Variant of B Variant
		///
		/// The only sorting criteria is the prefab chain; names are unimportant;
		/// thus 3 B Variant, 4 A Variant would also count as succeeded
		/// </summary>
		[Test]
		public void TestSortByPrefabHierarchy()
		{
			Debug.Log($"Test '{nameof(TestSortByPrefabHierarchy)}'\n{new string('-', 80)}");
			var paths = TestData.Instance.SortByPrefabHierarchyPaths;

			foreach (var pathField in paths)
			{
				string path = pathField;
				var guids = AssetDatabase.FindAssets("t:prefab", new[] { path }).ToList();

				// Sort using the tested utility
				Debug.Log(DebugUtility.GetAllGuidsString(guids, "Before sorting:"));
				EditorAssetUtility.SortByPrefabHierarchyGuids(guids);
				Debug.Log(DebugUtility.GetAllGuidsString(guids, "After sorting:"));

				// Determine the prefab hierarchy depth for each prefab
				var levels = guids
					.Select(guid =>
					{
						var assetPath = AssetDatabase.GUIDToAssetPath(guid);
						var go = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
						int level = 0;
						var current = go;

						// Count how many times we can walk up the variant chain
						while (PrefabUtility.IsPartOfVariantPrefab(current))
						{
							var basePrefab = PrefabUtility.GetCorrespondingObjectFromSource(current);
							if (basePrefab == null)
								break;

							level++;
							current = basePrefab;
						}
						return level;
					})
					.ToList();

				// Assert that the sorted prefabs are in correct hierarchy order
				for (int i = 1; i < levels.Count; i++)
				{
					Assert.That(levels[i], Is.GreaterThanOrEqualTo(levels[i - 1]),
						$"Sorting error: Prefab at index {i} (level {levels[i]}) comes after index {i - 1} (level {levels[i - 1]}), which violates ascending hierarchy order.\n" +
						$"GUID[{i - 1}] = {guids[i - 1]}\nGUID[{i}] = {guids[i]}");
				}
			}
		}

		[TearDown]
		public void TearDownSortByPrefabHierarchy()
		{
		}
	}
}