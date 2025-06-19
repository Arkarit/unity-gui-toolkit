using System.Linq;
using GuiToolkit.Editor;
using NUnit.Framework;
using UnityEditor;

namespace GuiToolkit.Test
{
	public class TestEditorAssetUtility
	{

		[SetUp]
		public void SetUpSortByPrefabHierarchy()
		{
		}

		[Test]
		public void TestSortByPrefabHierarchy()
		{
			var paths = TestData.Instance.SortByPrefabHierarchyPaths;

			foreach (var pathField in paths)
			{
				string path = pathField;
				var guids = AssetDatabase.FindAssets("t:prefab", new[] { path });
				EditorAssetUtility.SortByPrefabHierarchyGuids(guids.ToList());

				//TODO test for sorting:
				// Sorting order should be (only as example:)
				// 1 A: Regular Prefab
				// 2 B: Regular Prefab
				// 3 A Variant: Variant of A
				// 4 B Variant: Variant of B
				// 5 B Variant Variant: Variant of B Variant
				//
				// The only sorting criteria is the prefab chain; names are unimportant;
				// thus 3 B Variant, 4 A Variant would also count as succeeded
			}
		}

		[TearDown]
		public void TearDownSortByPrefabHierarchy()
		{
		}
	}
}