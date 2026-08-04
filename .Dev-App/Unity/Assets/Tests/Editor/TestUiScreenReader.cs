using System.Linq;
using GuiToolkit.Editor.AiSupport;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Test
{
	/// <summary>
	/// Verifies the Prefab→JSON read-back: the sidecar (auto) path returns the exact authoring JSON, and the
	/// structural path returns a re-bakeable tree with correct template/type identity and no text bleeding up
	/// from child nodes.
	/// </summary>
	[EditorAware]
	public class TestUiScreenReader
	{
		private string Dir => TestData.Instance.TempFolderPath.ToString().TrimEnd('/');
		private string BakedPath => $"{Dir}/ReadBackNUnit.prefab";
		private string RoundtripPath => $"{Dir}/ReadBackNUnitRT.prefab";

		private const string Json =
			"{\"name\":\"ReadBackNUnit\",\"root\":{\"type\":\"UiView\",\"id\":\"root\",\"props\":{\"layer\":\"Dialog\"}," +
			"\"children\":[{\"template\":\"StandardButtonBar\",\"id\":\"bar\",\"children\":[" +
			"{\"template\":\"OkButton\",\"id\":\"okBtn\",\"text\":\"@text:OK\"}]}]}}";

		[SetUp]
		public void SetUp()
		{
			TestData.Initialize();
			AssetDatabase.DeleteAsset(BakedPath);
			AssetDatabase.DeleteAsset(RoundtripPath);
		}

		[TearDown]
		public void TearDown()
		{
			AssetDatabase.DeleteAsset(BakedPath);
			AssetDatabase.DeleteAsset(RoundtripPath);
			var sidecar = UiScreenBaker.SidecarPathFor(BakedPath);
			if (System.IO.File.Exists(System.IO.Path.GetFullPath(sidecar)))
				AssetDatabase.DeleteAsset(sidecar);
		}

		[Test]
		public void AutoReadReturnsExactSidecarJson()
		{
			var baked = UiScreenBaker.Bake(AddOutput(Json, BakedPath));
			Assert.AreEqual(0, baked.warnings.Count);

			var read = UiScreenReader.Read(BakedPath, "auto");
			var root = (Newtonsoft.Json.Linq.JObject) read.screen["root"];

			Assert.AreEqual("UiView", (string) root["type"], "Root type should round-trip via the sidecar.");
			Assert.AreEqual("Dialog", (string) root["props"]?["layer"], "The authored 'layer' prop must survive.");
			var bar = (Newtonsoft.Json.Linq.JObject) root["children"][0];
			Assert.AreEqual("StandardButtonBar", (string) bar["template"]);
			var ok = (Newtonsoft.Json.Linq.JObject) bar["children"][0];
			Assert.AreEqual("OkButton", (string) ok["template"]);
			Assert.AreEqual("@text:OK", (string) ok["text"]);
		}

		[Test]
		public void StructuralReadHasIdentityAndNoTextBleedAndRebakes()
		{
			UiScreenBaker.Bake(AddOutput(Json, BakedPath));

			var read = UiScreenReader.Read(BakedPath, "structural");
			var root = (Newtonsoft.Json.Linq.JObject) read.screen["root"];

			Assert.AreEqual("UiView", (string) root["type"], "Structural read should identify the element root.");
			Assert.IsNull((string) root["text"], "Text must NOT bleed up from a descendant button into the root.");

			var templates = root.Descendants()
				.OfType<Newtonsoft.Json.Linq.JProperty>()
				.Where(p => p.Name == "template")
				.Select(p => (string) p.Value)
				.ToList();
			CollectionAssert.Contains(templates, "StandardButtonBar");
			CollectionAssert.Contains(templates, "OkButton");

			// The structural read-back must itself re-bake into a valid prefab.
			var screen = (Newtonsoft.Json.Linq.JObject) read.screen.DeepClone();
			screen["name"] = "ReadBackNUnitRT";
			screen["outputPath"] = RoundtripPath;
			var rebaked = UiScreenBaker.Bake(screen.ToString());
			Assert.IsFalse(string.IsNullOrEmpty(rebaked.path), "The structural read-back should re-bake.");
			Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<GameObject>(rebaked.path));
		}

		private static string AddOutput( string _json, string _path )
		{
			var o = Newtonsoft.Json.Linq.JObject.Parse(_json);
			o["outputPath"] = _path;
			return o.ToString();
		}
	}
}
