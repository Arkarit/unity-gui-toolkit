using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit.Test
{
	/// <summary>
	/// UiShapeImage.FadeOnly: a filled shape draws its fade ring and leaves out the body inside it.
	///
	/// Told apart by colour. Every triangle of the fade ring touches the shape's outer edge, which has the
	/// fade colour (transparent here); a triangle of the body has the shape's colour at all three corners.
	/// So "no fully opaque triangle" is "no body", for every shape and corner setting alike.
	/// </summary>
	[EditorAware]
	public class TestUiShapeImageFadeOnly
	{
		private const float Size = 100f;
		private const float Fade = 10f;

		private readonly List<Object> m_created = new();
		private MethodInfo m_populate;

		[SetUp]
		public void SetUp()
		{
			m_populate = typeof(UiShapeImage).GetMethod("OnPopulateMesh", BindingFlags.Instance | BindingFlags.NonPublic,
				null, new[] { typeof(VertexHelper) }, null);
			Assert.That(m_populate, Is.Not.Null);
		}

		[TearDown]
		public void TearDown()
		{
			foreach (var obj in m_created)
			{
				if (obj != null)
					Object.DestroyImmediate(obj);
			}

			m_created.Clear();
		}

		private static IEnumerable<System.Type> ShapeTypes()
		{
			yield return typeof(UiRoundedImage);
			yield return typeof(UiCircle);
			yield return typeof(UiStar);
		}

		[TestCaseSource(nameof(ShapeTypes))]
		public void WithoutFadeOnlyTheBodyIsDrawn( System.Type _type )
		{
			var shape = Build(_type);
			Assert.That(OpaqueTriangles(shape), Is.GreaterThan(0));
		}

		[TestCaseSource(nameof(ShapeTypes))]
		public void FadeOnlyLeavesOutTheBodyAndKeepsTheRing( System.Type _type )
		{
			var shape = Build(_type);
			int fullCount = Stream(shape).Count;

			shape.FadeOnly = true;
			var stream = Stream(shape);

			Assert.That(OpaqueTriangles(shape), Is.EqualTo(0));
			Assert.That(stream.Count, Is.GreaterThan(0));
			Assert.That(stream.Count, Is.LessThan(fullCount));
		}

		[Test]
		public void FadeOnlyLeavesOutTheBodyOfASquareRoundedImage()
		{
			var shape = (UiRoundedImage)Build(typeof(UiRoundedImage));
			shape.Corners = 0;
			shape.FadeOnly = true;

			Assert.That(OpaqueTriangles(shape), Is.EqualTo(0));
			Assert.That(Area(shape), Is.EqualTo(Size * Size - (Size - 2 * Fade) * (Size - 2 * Fade)).Within(0.05f));
		}

		[TestCaseSource(nameof(ShapeTypes))]
		public void FadeOnlyWithoutAFadeDrawsNothing( System.Type _type )
		{
			var shape = Build(_type);
			shape.FadeOnly = true;
			shape.FadeSize = 0f;

			Assert.That(Stream(shape), Is.Empty);
		}

		[TestCaseSource(nameof(ShapeTypes))]
		public void AFrameIgnoresFadeOnly( System.Type _type )
		{
			var shape = Build(_type);
			shape.FrameSize = 30f;
			float area = Area(shape);

			shape.FadeOnly = true;
			Assert.That(Area(shape), Is.EqualTo(area).Within(0.05f));
		}

		#region Helpers

		private UiShapeImage Build( System.Type _type )
		{
			var canvas = new GameObject("FadeOnlyTestCanvas", typeof(RectTransform), typeof(Canvas));
			m_created.Add(canvas);

			var go = new GameObject("Shape", typeof(RectTransform));
			go.transform.SetParent(canvas.transform, false);
			((RectTransform)go.transform).sizeDelta = new Vector2(Size, Size);

			var shape = (UiShapeImage)go.AddComponent(_type);
			shape.color = Color.white;
			shape.FadeColor = Color.clear;
			shape.FrameSize = 0f;
			shape.FadeSize = Fade;
			return shape;
		}

		private List<UIVertex> Stream( UiShapeImage _shape )
		{
			using var vertexHelper = new VertexHelper();
			m_populate.Invoke(_shape, new object[] { vertexHelper });

			var stream = new List<UIVertex>();
			vertexHelper.GetUIVertexStream(stream);
			return stream;
		}

		private int OpaqueTriangles( UiShapeImage _shape )
		{
			var stream = Stream(_shape);
			int count = 0;
			for (int i = 0; i + 2 < stream.Count; i += 3)
			{
				if (stream[i].color.a == 255 && stream[i + 1].color.a == 255 && stream[i + 2].color.a == 255)
					count++;
			}

			return count;
		}

		/// <summary>The covered area. Overlapping triangles count twice.</summary>
		private float Area( UiShapeImage _shape )
		{
			var stream = Stream(_shape);
			float area = 0f;
			for (int i = 0; i + 2 < stream.Count; i += 3)
			{
				Vector2 a = stream[i].position;
				Vector2 b = stream[i + 1].position;
				Vector2 c = stream[i + 2].position;
				area += 0.5f * Mathf.Abs((b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x));
			}

			return area;
		}

		#endregion
	}
}
