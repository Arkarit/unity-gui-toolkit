using System.Reflection;
using GuiToolkit.Editor.AiSupport;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Test
{
	/// <summary>
	/// Verifies the baker can stack several components on one node via the "components" field, so a UiView
	/// that is also a UiSimpleAnimation no longer needs a wrapper node — and that per-entry props are
	/// applied to the specific stacked component.
	/// </summary>
	[EditorAware]
	public class TestUiScreenBakerComponents
	{
		private string BakedPath => $"{TestData.Instance.TempFolderPath.ToString().TrimEnd('/')}/ComponentsTestNUnit.prefab";

		[SetUp]
		public void SetUp()
		{
			TestData.Initialize();
			AssetDatabase.DeleteAsset(BakedPath);
		}

		[TearDown]
		public void TearDown() => AssetDatabase.DeleteAsset(BakedPath);

		[Test]
		public void StacksExtraComponentWithPerEntryPropsOntoOneNode()
		{
			// A UiView root that also carries a UiSimpleAnimation, whose posXEnd is set via the stacked
			// entry's own props.
			string json =
				"{\"name\":\"ComponentsTestNUnit\",\"outputPath\":" + Quote(BakedPath) + "," +
				"\"root\":{\"type\":\"UiView\",\"id\":\"root\"," +
				"\"components\":[{\"type\":\"UiSimpleAnimation\",\"props\":{\"posXEnd\":42}}]}}";

			var result = UiScreenBaker.Bake(json);
			Assert.AreEqual(0, result.warnings.Count, "Unexpected warnings: " + string.Join(" | ", result.warnings));

			var go = AssetDatabase.LoadAssetAtPath<GameObject>(result.path);
			var view = go.GetComponent<UiView>();
			var anim = go.GetComponent<UiSimpleAnimation>();

			Assert.IsNotNull(view, "The primary UiView must be present.");
			Assert.IsNotNull(anim, "The stacked UiSimpleAnimation must be present on the same GameObject.");

			var posXEnd = GetField(anim, "m_posXEnd");
			Assert.IsNotNull(posXEnd, "UiSimpleAnimation should expose m_posXEnd.");
			Assert.AreEqual(42f, (float) posXEnd.GetValue(anim), 1e-4f, "Per-entry props should land on the stacked component.");
		}

		private static FieldInfo GetField( object _target, string _name )
		{
			for (var t = _target.GetType(); t != null; t = t.BaseType)
			{
				var f = t.GetField(_name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
				if (f != null)
					return f;
			}
			return null;
		}

		private static string Quote( string _s ) => "\"" + _s.Replace("\\", "/") + "\"";
	}
}
