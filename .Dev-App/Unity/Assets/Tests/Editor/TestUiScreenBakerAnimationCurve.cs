using System.Reflection;
using GuiToolkit.Editor.AiSupport;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Test
{
	/// <summary>
	/// Verifies the baker converts AnimationCurve props from all three author-friendly shapes (a preset
	/// name, a keyframe list, and a { preset, from, to } object) onto a real serialized AnimationCurve
	/// field — here <see cref="UiSimpleAnimation"/>'s alpha / scale curves.
	/// </summary>
	[EditorAware]
	public class TestUiScreenBakerAnimationCurve
	{
		private string BakedPath => $"{TestData.Instance.TempFolderPath.ToString().TrimEnd('/')}/AnimationCurveTestNUnit.prefab";

		[SetUp]
		public void SetUp()
		{
			TestData.Initialize();
			AssetDatabase.DeleteAsset(BakedPath);
		}

		[TearDown]
		public void TearDown() => AssetDatabase.DeleteAsset(BakedPath);

		[Test]
		public void AnimationCurvePropsConvertFromPresetKeysAndRange()
		{
			// alphaCurve: bare preset string. scaleXCurve: explicit keyframe list. scaleYCurve: preset with
			// an explicit from/to range (2s → value 2).
			//
			// A minimally complete animation, because an incomplete one puts its own gaps in the way of the
			// assertions: "support" has to name every channel a curve is authored for (an unsupported curve
			// is baked but never runs), the animation needs a "target" to drive, and an Alpha channel needs
			// a graphic to fade. All three are things the baker warns about, and all three are fixture
			// concerns rather than curve-conversion concerns - hence the single Image child that serves as
			// target and alpha carrier at once.
			string json =
				"{\"name\":\"AnimationCurveTestNUnit\",\"outputPath\":" + Quote(BakedPath) + "," +
				"\"root\":{\"type\":\"UiSimpleAnimation\",\"id\":\"anim\",\"props\":{" +
				"\"support\":\"ScaleX, ScaleY, Alpha\"," +
				"\"target\":\"#animated\"," +
				"\"alphaGraphic\":\"#animated\"," +
				"\"alphaCurve\":\"easeInOut\"," +
				"\"scaleXCurve\":[{\"time\":0,\"value\":0},{\"time\":1,\"value\":1}]," +
				"\"scaleYCurve\":{\"preset\":\"linear\",\"from\":[0,0],\"to\":[2,2]}" +
				"}," +
				"\"children\":[{\"type\":\"Image\",\"id\":\"animated\"}]}}";

			var result = UiScreenBaker.Bake(json);
			Assert.AreEqual(0, result.warnings.Count, "Unexpected warnings: " + string.Join(" | ", result.warnings));

			var anim = AssetDatabase.LoadAssetAtPath<GameObject>(result.path).GetComponent<UiSimpleAnimation>();
			Assert.IsNotNull(anim, "Baked root should carry a UiSimpleAnimation.");

			var alpha = GetCurve(anim, "m_alphaCurve");
			Assert.IsNotNull(alpha, "m_alphaCurve should be assigned.");
			Assert.AreEqual(2, alpha.length, "easeInOut preset produces a 2-key curve.");
			Assert.AreEqual(1f, alpha.keys[1].value, 1e-4f, "Default preset range ends at value 1.");

			var scaleX = GetCurve(anim, "m_scaleXCurve");
			Assert.IsNotNull(scaleX, "m_scaleXCurve should be assigned.");
			Assert.AreEqual(2, scaleX.length, "The keyframe list has two keys.");
			Assert.AreEqual(0f, scaleX.keys[0].time, 1e-4f);
			Assert.AreEqual(1f, scaleX.keys[1].value, 1e-4f);

			var scaleY = GetCurve(anim, "m_scaleYCurve");
			Assert.IsNotNull(scaleY, "m_scaleYCurve should be assigned.");
			Assert.AreEqual(2f, scaleY.keys[scaleY.length - 1].time, 1e-4f, "Explicit 'to' time should be honoured.");
			Assert.AreEqual(2f, scaleY.keys[scaleY.length - 1].value, 1e-4f, "Explicit 'to' value should be honoured.");
		}

		private static AnimationCurve GetCurve( object _target, string _field )
		{
			for (var t = _target.GetType(); t != null; t = t.BaseType)
			{
				var f = t.GetField(_field, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
				if (f != null)
					return (AnimationCurve) f.GetValue(_target);
			}
			return null;
		}

		private static string Quote( string _s ) => "\"" + _s.Replace("\\", "/") + "\"";
	}
}
