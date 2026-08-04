using GuiToolkit.Editor.AiSupport;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit.Test
{
	/// <summary>
	/// Verifies the baker makes a ScrollRect's Content actually scroll: a scaffolded (bare) UiScrollRect
	/// gets a Content with a layout group + ContentSizeFitter along the scroll axis, and an explicit
	/// "scroll" field drives direction / layout / fit. Without this the Content stays 0-sized (dead scroll).
	/// </summary>
	[EditorAware]
	public class TestUiScreenBakerScroll
	{
		private string BakedPath => $"{TestData.Instance.TempFolderPath.ToString().TrimEnd('/')}/ScrollTestNUnit.prefab";

		[SetUp]
		public void SetUp()
		{
			TestData.Initialize();
			AssetDatabase.DeleteAsset(BakedPath);
		}

		[TearDown]
		public void TearDown() => AssetDatabase.DeleteAsset(BakedPath);

		[Test]
		public void BareScrollRectDefaultsToVerticalListThatSizesItsContent()
		{
			string json =
				"{\"name\":\"ScrollTestNUnit\",\"outputPath\":" + Quote(BakedPath) + "," +
				"\"root\":{\"type\":\"UiScrollRect\",\"id\":\"scroll\"," +
				"\"children\":[{\"type\":\"UiScrollRect\",\"id\":\"dummy1\"},{\"type\":\"UiScrollRect\",\"id\":\"dummy2\"}]}}";

			var result = UiScreenBaker.Bake(json);
			Assert.AreEqual(0, result.warnings.Count, "Unexpected warnings: " + string.Join(" | ", result.warnings));

			var scrollRect = AssetDatabase.LoadAssetAtPath<GameObject>(result.path).GetComponent<ScrollRect>();
			Assert.IsNotNull(scrollRect, "Baked root should carry a ScrollRect.");
			Assert.IsNotNull(scrollRect.content, "A Content should have been scaffolded.");
			Assert.IsTrue(scrollRect.vertical, "Default direction is vertical.");

			var content = scrollRect.content.gameObject;
			Assert.IsNotNull(content.GetComponent<VerticalLayoutGroup>(), "Default layout is a vertical list.");

			var fitter = content.GetComponent<ContentSizeFitter>();
			Assert.IsNotNull(fitter, "Content should size itself so it can scroll.");
			Assert.AreEqual(ContentSizeFitter.FitMode.PreferredSize, fitter.verticalFit, "Vertical scroll grows Content height.");
			Assert.AreEqual(ContentSizeFitter.FitMode.Unconstrained, fitter.horizontalFit, "Width follows the viewport.");
		}

		[Test]
		public void ExplicitScrollFieldDrivesHorizontalGridAndFlags()
		{
			string json =
				"{\"name\":\"ScrollTestNUnit\",\"outputPath\":" + Quote(BakedPath) + "," +
				"\"root\":{\"type\":\"UiScrollRect\",\"id\":\"scroll\"," +
				"\"scroll\":{\"direction\":\"horizontal\",\"layout\":\"grid\",\"cellSize\":[120,80],\"spacing\":6,\"padding\":[4,4,4,4]}}}";

			var result = UiScreenBaker.Bake(json);
			Assert.AreEqual(0, result.warnings.Count, "Unexpected warnings: " + string.Join(" | ", result.warnings));

			var scrollRect = AssetDatabase.LoadAssetAtPath<GameObject>(result.path).GetComponent<ScrollRect>();
			Assert.IsTrue(scrollRect.horizontal, "Direction horizontal enables horizontal scrolling.");
			Assert.IsFalse(scrollRect.vertical, "Horizontal-only scroll disables vertical.");

			var content = scrollRect.content.gameObject;
			var grid = content.GetComponent<GridLayoutGroup>();
			Assert.IsNotNull(grid, "layout:grid should add a GridLayoutGroup.");
			Assert.AreEqual(new Vector2(120, 80), grid.cellSize);
			Assert.AreEqual(new Vector2(6, 6), grid.spacing, "A scalar spacing applies to both grid axes.");
			Assert.AreEqual(4, grid.padding.left);

			var fitter = content.GetComponent<ContentSizeFitter>();
			Assert.AreEqual(ContentSizeFitter.FitMode.PreferredSize, fitter.horizontalFit, "Horizontal scroll grows Content width.");
			Assert.AreEqual(ContentSizeFitter.FitMode.Unconstrained, fitter.verticalFit);
		}

		private static string Quote( string _s ) => "\"" + _s.Replace("\\", "/") + "\"";
	}
}
