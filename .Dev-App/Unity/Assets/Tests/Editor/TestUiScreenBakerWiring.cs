using System.Reflection;
using GuiToolkit.Editor.AiSupport;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit.Test
{
	/// <summary>
	/// Verifies the baker's two-pass reference wiring: a "#id" prop (here a list on UiView.m_closeButtons)
	/// is resolved after the whole tree is built, so it can point FORWARD to nodes created later, and the
	/// element type (Button) is pulled off the referenced node.
	/// </summary>
	[EditorAware]
	public class TestUiScreenBakerWiring
	{
		private string BakedPath => $"{TestData.Instance.TempFolderPath.ToString().TrimEnd('/')}/WiringTestNUnit.prefab";

		[SetUp]
		public void SetUp()
		{
			TestData.Initialize();
			AssetDatabase.DeleteAsset(BakedPath);
		}

		[TearDown]
		public void TearDown() => AssetDatabase.DeleteAsset(BakedPath);

		[Test]
		public void CloseButtonRefListResolvesForwardById()
		{
			// closeButtons references the button by id — and the button node is created AFTER the root's
			// props are read, so this only works if the reference is deferred to the second pass.
			string json =
				"{\"name\":\"WiringTestNUnit\",\"outputPath\":" + Quote(BakedPath) + "," +
				"\"root\":{\"type\":\"UiView\",\"id\":\"root\",\"props\":{\"closeButtons\":[\"#b1\"]}," +
				"\"children\":[{\"template\":\"OkButton\",\"id\":\"b1\",\"text\":\"@text:OK\"}]}}";

			var result = UiScreenBaker.Bake(json);
			Assert.AreEqual(0, result.warnings.Count, "Unexpected warnings: " + string.Join(" | ", result.warnings));

			var view = AssetDatabase.LoadAssetAtPath<GameObject>(result.path).GetComponent<UiView>();
			Assert.IsNotNull(view, "Baked root should carry a UiView.");

			var field = typeof(UiView).GetField("m_closeButtons", BindingFlags.Instance | BindingFlags.NonPublic);
			var closeButtons = (Button[]) field.GetValue(view);

			Assert.IsNotNull(closeButtons, "m_closeButtons should be assigned.");
			Assert.AreEqual(1, closeButtons.Length, "The one referenced button should be wired.");
			Assert.IsNotNull(closeButtons[0], "The wired reference must resolve to a real Button, not null.");
		}

		private static string Quote( string _s ) => "\"" + _s.Replace("\\", "/") + "\"";
	}
}
