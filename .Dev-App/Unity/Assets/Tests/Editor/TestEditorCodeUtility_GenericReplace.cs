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

			[SerializeField] public long[] LongArray;
			[SerializeField] public double[] DoubleArray;
			[SerializeField] public float[] FloatArray;
			[SerializeField] public bool[] BoolArray;

			[SerializeField] public Vector3[] Vector3Array;
			[SerializeField] public Color[] ColorArray;

			[SerializeField] public UnityEngine.Object[] ObjectArray;
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

			[SerializeField] public long[] LongArray;
			[SerializeField] public double[] DoubleArray;
			[SerializeField] public float[] FloatArray;
			[SerializeField] public bool[] BoolArray;

			[SerializeField] public Vector3[] Vector3Array;
			[SerializeField] public Color[] ColorArray;

			[SerializeField] public UnityEngine.Object[] ObjectArray;
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

			// primitive arrays
			src.IntArray = new[] { 1, 2, 3 };
			src.StringArray = new[] { "a", "b" };
			src.LongArray = new[] { 10000000000L, -7L };
			src.DoubleArray = new[] { 0.25, -3.5 };
			src.FloatArray = new[] { 1.5f, 2.5f };
			src.BoolArray = new[] { true, false, true };

			// struct arrays
			src.Vector3Array = new[]
			{
				new Vector3(1, 2, 3),
				new Vector3(4, 5, 6)
			};

			src.ColorArray = new[]
			{
				new Color(0.1f, 0.2f, 0.3f, 0.4f),
				new Color(0.9f, 0.8f, 0.7f, 0.6f)
			};

			// object reference array
			var refGo1 = new GameObject("Ref1");
			var refGo2 = new GameObject("Ref2");
			src.ObjectArray = new UnityEngine.Object[] { refGo1, refGo2 };

			var results = GuiToolkit.Editor.EditorCodeUtility
				.ReplaceMonoBehavioursInActiveSceneGeneric<SourceComp, TargetComp>();

			Assert.That(results.Count, Is.EqualTo(1));

			var dst = go.GetComponent<TargetComp>();
			Assert.That(dst, Is.Not.Null);

			// --- int ---
			Assert.That(dst.IntArray, Is.Not.Null);
			Assert.That(dst.IntArray.Length, Is.EqualTo(3));
			Assert.That(dst.IntArray[0], Is.EqualTo(1));
			Assert.That(dst.IntArray[1], Is.EqualTo(2));
			Assert.That(dst.IntArray[2], Is.EqualTo(3));

			// --- string ---
			Assert.That(dst.StringArray.Length, Is.EqualTo(2));
			Assert.That(dst.StringArray[0], Is.EqualTo("a"));
			Assert.That(dst.StringArray[1], Is.EqualTo("b"));

			// --- long ---
			Assert.That(dst.LongArray.Length, Is.EqualTo(2));
			Assert.That(dst.LongArray[0], Is.EqualTo(10000000000L));
			Assert.That(dst.LongArray[1], Is.EqualTo(-7L));

			// --- double ---
			Assert.That(dst.DoubleArray.Length, Is.EqualTo(2));
			Assert.That(dst.DoubleArray[0], Is.EqualTo(0.25).Within(1e-9));
			Assert.That(dst.DoubleArray[1], Is.EqualTo(-3.5).Within(1e-9));

			// --- float ---
			Assert.That(dst.FloatArray.Length, Is.EqualTo(2));
			Assert.That(dst.FloatArray[0], Is.EqualTo(1.5f));
			Assert.That(dst.FloatArray[1], Is.EqualTo(2.5f));

			// --- bool ---
			Assert.That(dst.BoolArray.Length, Is.EqualTo(3));
			Assert.That(dst.BoolArray[0], Is.True);
			Assert.That(dst.BoolArray[1], Is.False);
			Assert.That(dst.BoolArray[2], Is.True);

			// --- Vector3 ---
			Assert.That(dst.Vector3Array.Length, Is.EqualTo(2));
			Assert.That(dst.Vector3Array[0], Is.EqualTo(new Vector3(1, 2, 3)));
			Assert.That(dst.Vector3Array[1], Is.EqualTo(new Vector3(4, 5, 6)));

			// --- Color ---
			Assert.That(dst.ColorArray.Length, Is.EqualTo(2));
			Assert.That(dst.ColorArray[0], Is.EqualTo(new Color(0.1f, 0.2f, 0.3f, 0.4f)));
			Assert.That(dst.ColorArray[1], Is.EqualTo(new Color(0.9f, 0.8f, 0.7f, 0.6f)));

			// --- ObjectReference ---
			Assert.That(dst.ObjectArray.Length, Is.EqualTo(2));
			Assert.That(dst.ObjectArray[0], Is.SameAs(refGo1));
			Assert.That(dst.ObjectArray[1], Is.SameAs(refGo2));
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
