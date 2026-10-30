using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit.Test
{
	/// <summary>
	/// The corners of a UiRoundedImage, measured by the area their mesh covers.
	///
	/// Area is the one number that catches both ways a tiling goes wrong: a hole makes it too small, an
	/// overlap makes it too large (overlapping triangles count twice). Each expected value is the exact
	/// area of the polygon the shape is meant to be - an arc with n segments is a polygon too - so the
	/// comparisons can be tight.
	/// </summary>
	[EditorAware]
	public class TestUiRoundedImageCorners
	{
		private const float Width = 200f;
		private const float Height = 100f;
		private const float Radius = 30f;
		private const int Segments = 6;
		private const float Tolerance = 0.05f;

		private static readonly float ChamferShrink = 2f - Mathf.Sqrt(2f);

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

		[Test]
		public void ANewImageRoundsAllCornersAndChamfersNone()
		{
			var shape = Build();
			Assert.That(shape.Corners, Is.EqualTo(UiRoundedImage.AllCorners));
			Assert.That(shape.ChamferedCorners, Is.EqualTo((CornerFlags)0));
		}

		[Test]
		public void WithoutCornersItIsARectangle()
		{
			var shape = Build();
			shape.Corners = 0;
			Assert.That(shape.HasShapedCorner, Is.False);
			Assert.That(Area(shape), Is.EqualTo(Width * Height).Within(Tolerance));
		}

		[Test]
		public void FilledRoundCornersCoverTheRoundedRect()
		{
			var shape = Build();
			Assert.That(Area(shape), Is.EqualTo(Width * Height - 4 * RoundCut(Radius)).Within(Tolerance));
		}

		[Test]
		public void FilledOneChamferCutsOffOneTriangle()
		{
			var shape = Build();
			shape.Corners = CornerFlags.TopLeft;
			shape.ChamferedCorners = CornerFlags.TopLeft;
			Assert.That(Area(shape), Is.EqualTo(Width * Height - ChamferCut(Radius)).Within(Tolerance));
		}

		[Test]
		public void FilledMixedCornersCutEachTheirOwn()
		{
			var shape = Build();
			shape.Corners = CornerFlags.TopLeft | CornerFlags.TopRight | CornerFlags.BottomRight;
			shape.ChamferedCorners = CornerFlags.TopRight | CornerFlags.BottomLeft;

			// Top left round, top right chamfered, bottom right round, bottom left square: its chamfer flag
			// does nothing, because the corner has no radius.
			float expected = Width * Height - 2 * RoundCut(Radius) - ChamferCut(Radius);
			Assert.That(Area(shape), Is.EqualTo(expected).Within(Tolerance));
		}

		[Test]
		public void FilledWithFadeLeavesNoSeamBetweenRingAndBody()
		{
			var shape = Build();
			shape.FadeSize = 4f;
			shape.ChamferedCorners = CornerFlags.TopLeft | CornerFlags.BottomRight;

			float expected = Width * Height - 2 * RoundCut(Radius) - 2 * ChamferCut(Radius);
			Assert.That(Area(shape), Is.EqualTo(expected).Within(Tolerance));
		}

		[Test]
		public void AChamferedFrameKeepsItsThicknessAcrossTheCut()
		{
			const float frame = 8f;
			var shape = Build();
			shape.FrameSize = frame;
			shape.ChamferedCorners = UiRoundedImage.AllCorners;

			// The inner boundary is the outer one moved inwards by the frame, square to every edge - the
			// chamfer included. Its cut is then shorter by frame * (2 - sqrt 2) along the inner sides.
			float inner = Radius - frame * ChamferShrink;
			float outerArea = Width * Height - 4 * ChamferCut(Radius);
			float innerArea = (Width - 2 * frame) * (Height - 2 * frame) - 4 * ChamferCut(inner);
			Assert.That(Area(shape), Is.EqualTo(outerArea - innerArea).Within(Tolerance));
		}

		[Test]
		public void AFrameWithMixedCornersCoversItsRing()
		{
			const float frame = 8f;
			var shape = Build();
			shape.FrameSize = frame;
			shape.Corners = CornerFlags.TopLeft | CornerFlags.TopRight | CornerFlags.BottomRight;
			shape.ChamferedCorners = CornerFlags.TopLeft;

			float outerArea = Width * Height - ChamferCut(Radius) - 2 * RoundCut(Radius);
			float innerArea = (Width - 2 * frame) * (Height - 2 * frame)
			                  - ChamferCut(Radius - frame * ChamferShrink) - 2 * RoundCut(Radius - frame);
			Assert.That(Area(shape), Is.EqualTo(outerArea - innerArea).Within(Tolerance));
		}

		[Test]
		public void AFrameThickerThanItsChamferHasASquareInside()
		{
			const float frame = 30f;
			const float radius = 10f;
			var shape = Build();
			shape.Radius = radius;
			shape.FrameSize = frame;
			shape.ChamferedCorners = UiRoundedImage.AllCorners;

			float outerArea = Width * Height - 4 * ChamferCut(radius);
			float innerArea = (Width - 2 * frame) * (Height - 2 * frame);
			Assert.That(Area(shape), Is.EqualTo(outerArea - innerArea).Within(Tolerance));
		}

		[Test]
		public void AFadedMixedFrameCoversItsRingWithoutOverlap()
		{
			const float frame = 12f;
			const float fade = 3f;
			var shape = Build();
			shape.FrameSize = frame;
			shape.FadeSize = fade;
			shape.ChamferedCorners = CornerFlags.BottomLeft | CornerFlags.TopRight;

			float outerArea = Width * Height - 2 * ChamferCut(Radius) - 2 * RoundCut(Radius);
			float innerArea = (Width - 2 * frame) * (Height - 2 * frame)
			                  - 2 * ChamferCut(Radius - frame * ChamferShrink) - 2 * RoundCut(Radius - frame);
			Assert.That(Area(shape), Is.EqualTo(outerArea - innerArea).Within(Tolerance));
		}

		[Test]
		public void EveryTriangleIsWoundClockwise()
		{
			var shape = Build();
			shape.FrameSize = 12f;
			shape.FadeSize = 3f;
			shape.Corners = CornerFlags.TopLeft | CornerFlags.TopRight | CornerFlags.BottomLeft;
			shape.ChamferedCorners = CornerFlags.TopRight;

			foreach (float cross in Crosses(shape))
				Assert.That(cross, Is.LessThan(0f));

			shape.FrameSize = 0f;
			foreach (float cross in Crosses(shape))
				Assert.That(cross, Is.LessThan(0f));
		}

		#region Helpers

		/// <summary>What a rounded corner takes away from the rect: its square minus the n-gon quarter disc.</summary>
		private static float RoundCut( float _radius ) =>
			_radius * _radius - Segments * 0.5f * _radius * _radius * Mathf.Sin(0.5f * Mathf.PI / Segments);

		private static float ChamferCut( float _radius ) => 0.5f * _radius * _radius;

		private UiRoundedImage Build()
		{
			var canvas = new GameObject("CornerTestCanvas", typeof(RectTransform), typeof(Canvas));
			m_created.Add(canvas);

			var go = new GameObject("Shape", typeof(RectTransform));
			go.transform.SetParent(canvas.transform, false);
			((RectTransform)go.transform).sizeDelta = new Vector2(Width, Height);

			var shape = go.AddComponent<UiRoundedImage>();
			shape.Radius = Radius;
			shape.CornerSegments = Segments;
			shape.FrameSize = 0f;
			shape.FadeSize = 0f;
			return shape;
		}

		private List<UIVertex> Stream( UiRoundedImage _shape )
		{
			using var vertexHelper = new VertexHelper();
			m_populate.Invoke(_shape, new object[] { vertexHelper });

			var stream = new List<UIVertex>();
			vertexHelper.GetUIVertexStream(stream);
			return stream;
		}

		/// <summary>The covered area. Overlapping triangles count twice, so an overlap shows as too much.</summary>
		private float Area( UiRoundedImage _shape )
		{
			float area = 0f;
			foreach (float cross in Crosses(_shape))
				area += 0.5f * Mathf.Abs(cross);

			return area;
		}

		private IEnumerable<float> Crosses( UiRoundedImage _shape )
		{
			var stream = Stream(_shape);
			for (int i = 0; i + 2 < stream.Count; i += 3)
			{
				Vector2 a = stream[i].position;
				Vector2 b = stream[i + 1].position;
				Vector2 c = stream[i + 2].position;
				yield return (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x);
			}
		}

		#endregion
	}
}
