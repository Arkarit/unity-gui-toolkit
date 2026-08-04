using GuiToolkit.Editor.AiSupport;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit.Test
{
	/// <summary>
	/// Verifies the raw UGUI/Unity base vocabulary bakes correctly: a native component (CanvasGroup), whose
	/// authorable data lives on C# properties rather than serialized fields, has its props set via the baker's
	/// property setter; and a managed UGUI type (Image) has its ordinary serialized fields set as usual.
	/// </summary>
	[EditorAware]
	public class TestUiScreenBakerUnityTypes
	{
		private string BakedPath => $"{TestData.Instance.TempFolderPath.ToString().TrimEnd('/')}/UnityTypesTestNUnit.prefab";

		[SetUp]
		public void SetUp()
		{
			TestData.Initialize();
			AssetDatabase.DeleteAsset(BakedPath);
		}

		[TearDown]
		public void TearDown() => AssetDatabase.DeleteAsset(BakedPath);

		[Test]
		public void SetsNativeComponentPropsViaPropertySetter()
		{
			// CanvasGroup is a native component: alpha/interactable/... are C# properties, not serialized
			// fields, so this exercises the baker's property-setter (MemberRef) path.
			string json =
				"{\"name\":\"UnityTypesTestNUnit\",\"outputPath\":" + Quote(BakedPath) + "," +
				"\"root\":{\"type\":\"UiView\",\"id\":\"root\",\"children\":[" +
				"{\"type\":\"CanvasGroup\",\"id\":\"fader\",\"props\":{" +
				"\"alpha\":0.5,\"interactable\":false,\"blocksRaycasts\":false,\"ignoreParentGroups\":true}}" +
				"]}}";

			var result = UiScreenBaker.Bake(json);
			Assert.AreEqual(0, result.warnings.Count, "Unexpected warnings: " + string.Join(" | ", result.warnings));

			var go = AssetDatabase.LoadAssetAtPath<GameObject>(result.path);
			var cg = go.transform.GetChild(0).GetComponent<CanvasGroup>();
			Assert.IsNotNull(cg, "The CanvasGroup child must be present.");

			Assert.AreEqual(0.5f, cg.alpha, 1e-4f, "alpha should be set via the property setter.");
			Assert.IsFalse(cg.interactable, "interactable should be set via the property setter.");
			Assert.IsFalse(cg.blocksRaycasts, "blocksRaycasts should be set via the property setter.");
			Assert.IsTrue(cg.ignoreParentGroups, "ignoreParentGroups should be set via the property setter.");
		}

		[Test]
		public void SetsFieldsOnManagedRawUguiType()
		{
			// Image is a managed UGUI type with real serialized fields — the ordinary field path. Its
			// authoring names come straight from the catalog (m_* stripped): RaycastTarget/FillAmount/Color.
			string json =
				"{\"name\":\"UnityTypesTestNUnit\",\"outputPath\":" + Quote(BakedPath) + "," +
				"\"root\":{\"type\":\"UiView\",\"id\":\"root\",\"children\":[" +
				"{\"type\":\"Image\",\"id\":\"img\",\"props\":{" +
				"\"RaycastTarget\":false,\"FillAmount\":0.25,\"Color\":\"#FF0000\"}}" +
				"]}}";

			var result = UiScreenBaker.Bake(json);
			Assert.AreEqual(0, result.warnings.Count, "Unexpected warnings: " + string.Join(" | ", result.warnings));

			var go = AssetDatabase.LoadAssetAtPath<GameObject>(result.path);
			var img = go.transform.GetChild(0).GetComponent<Image>();
			Assert.IsNotNull(img, "The Image child must be present.");

			Assert.IsFalse(img.raycastTarget, "RaycastTarget (serialized field) should be set.");
			Assert.AreEqual(0.25f, img.fillAmount, 1e-4f, "FillAmount (serialized field) should be set.");
			Assert.AreEqual(1f, img.color.r, 1e-3f, "Color.r should be set from the html string.");
			Assert.AreEqual(0f, img.color.g, 1e-3f, "Color.g should be set from the html string.");
		}

		private static string Quote( string _s ) => "\"" + _s.Replace("\\", "/") + "\"";
	}
}
