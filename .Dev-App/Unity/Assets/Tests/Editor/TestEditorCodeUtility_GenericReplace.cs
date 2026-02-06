#if UNITY_EDITOR
using System;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GuiToolkit.Test
{
	public sealed class TestEditorCodeUtility_GenericReplace
	{
		[Serializable]
		private sealed class SourceComp : MonoBehaviour
		{
			[SerializeField] public int IntValue;
			[SerializeField] public float FloatValue;
			[SerializeField] public bool BoolValue;
			[SerializeField] public string StringValue;
			[SerializeField] public Color ColorValue;
			[SerializeField] public Vector3 Vector3Value;

			[SerializeField] public int[] IntArray = new int[0];
			[SerializeField] public string[] StringArray = new string[0];
		}

		[Serializable]
		private sealed class TargetComp : MonoBehaviour
		{
			[SerializeField] public int IntValue;
			[SerializeField] public float FloatValue;
			[SerializeField] public bool BoolValue;
			[SerializeField] public string StringValue;
			[SerializeField] public Color ColorValue;
			[SerializeField] public Vector3 Vector3Value;

			[SerializeField] public int[] IntArray = new int[0];
			[SerializeField] public string[] StringArray = new string[0];
		}

		[Serializable]
		private sealed class RefHolder : MonoBehaviour
		{
			[SerializeField] public MonoBehaviour Ref;
		}

		[SetUp]
		public void SetUp()
		{
			EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
		}

		[Test]
		public void ReplaceMonoBehavioursInActiveSceneGeneric_ReplacesComponent()
		{
			var go = new GameObject("A");
			var src = go.AddComponent<SourceComp>();

			src.IntValue = 42;

			var results = GuiToolkit.Editor.EditorCodeUtility
				.ReplaceMonoBehavioursInActiveSceneGeneric<SourceComp, TargetComp>();

			Assert.That(results, Is.Not.Null);
			Assert.That(results.Count, Is.EqualTo(1));

			var srcAfter = go.GetComponent<SourceComp>();
			var dstAfter = go.GetComponent<TargetComp>();

			Assert.That(srcAfter, Is.Null);
			Assert.That(dstAfter, Is.Not.Null);

			Assert.That(dstAfter.IntValue, Is.EqualTo(42));
		}

		[Test]
		public void ReplaceMonoBehavioursInActiveSceneGeneric_CopiesMatchingSerializedFields()
		{
			var go = new GameObject("A");
			var src = go.AddComponent<SourceComp>();

			src.IntValue = 123;
			src.FloatValue = 1.25f;
			src.BoolValue = true;
			src.StringValue = "hello";
			src.ColorValue = new Color(0.1f, 0.2f, 0.3f, 0.4f);
			src.Vector3Value = new Vector3(7f, 8f, 9f);

			var results = GuiToolkit.Editor.EditorCodeUtility
				.ReplaceMonoBehavioursInActiveSceneGeneric<SourceComp, TargetComp>();

			Assert.That(results.Count, Is.EqualTo(1));

			var dst = go.GetComponent<TargetComp>();
			Assert.That(dst, Is.Not.Null);

			Assert.That(dst.IntValue, Is.EqualTo(123));
			Assert.That(dst.FloatValue, Is.EqualTo(1.25f));
			Assert.That(dst.BoolValue, Is.True);
			Assert.That(dst.StringValue, Is.EqualTo("hello"));
			Assert.That(dst.ColorValue, Is.EqualTo(new Color(0.1f, 0.2f, 0.3f, 0.4f)));
			Assert.That(dst.Vector3Value, Is.EqualTo(new Vector3(7f, 8f, 9f)));
		}

		[Test]
		public void ReplaceMonoBehavioursInActiveSceneGeneric_CopiesArraysBySizeAndValues()
		{
			var go = new GameObject("A");
			var src = go.AddComponent<SourceComp>();

			src.IntArray = new[] { 5, 6, 7 };
			src.StringArray = new[] { "a", "b" };

			var results = GuiToolkit.Editor.EditorCodeUtility
				.ReplaceMonoBehavioursInActiveSceneGeneric<SourceComp, TargetComp>();

			Assert.That(results.Count, Is.EqualTo(1));

			var dst = go.GetComponent<TargetComp>();
			Assert.That(dst, Is.Not.Null);

			Assert.That(dst.IntArray, Is.Not.Null);
			Assert.That(dst.IntArray.Length, Is.EqualTo(3));
			Assert.That(dst.IntArray[0], Is.EqualTo(5));
			Assert.That(dst.IntArray[1], Is.EqualTo(6));
			Assert.That(dst.IntArray[2], Is.EqualTo(7));

			Assert.That(dst.StringArray, Is.Not.Null);
			Assert.That(dst.StringArray.Length, Is.EqualTo(2));
			Assert.That(dst.StringArray[0], Is.EqualTo("a"));
			Assert.That(dst.StringArray[1], Is.EqualTo("b"));
		}

		[Test]
		public void ReplaceMonoBehavioursInActiveSceneGeneric_RewiresObjectReferencesToNewComponent()
		{
			var a = new GameObject("A");
			var b = new GameObject("B");

			var src = a.AddComponent<SourceComp>();

			var holder = b.AddComponent<RefHolder>();
			holder.Ref = src;

			var results = GuiToolkit.Editor.EditorCodeUtility
				.ReplaceMonoBehavioursInActiveSceneGeneric<SourceComp, TargetComp>();

			Assert.That(results.Count, Is.EqualTo(1));

			var dst = a.GetComponent<TargetComp>();
			Assert.That(dst, Is.Not.Null);

			Assert.That(holder.Ref, Is.Not.Null);
			Assert.That(holder.Ref, Is.SameAs(dst));
		}
	}
}
#endif
