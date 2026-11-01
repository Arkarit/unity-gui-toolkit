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

			Assert.That(Area(shape), Is.EqualTo(RingArea(shape, frame)).Within(Tolerance));
		}

		[TestCase(1)]
		[TestCase(2)]
		[TestCase(3)]
		[TestCase(6)]
		public void ARoundFrameIsAsThickOnEverySegmentAsOnTheSides( int _segments )
		{
			const float frame = 8f;
			var shape = Build();
			shape.FrameSize = frame;
			shape.CornerSegments = _segments;

			// An inner arc of radius r - f made the frame f * cos(half a segment) thick on each segment:
			// 71% on one, 92% on two - and the ring that much smaller than this.
			Assert.That(Area(shape), Is.EqualTo(RingArea(shape, frame)).Within(Tolerance));
		}

		[TestCase(2, 9.5f)]  // past the centre: only the first and last segment still bound the inside
		[TestCase(2, 30f)]   // past the corner entirely: the inside is the inner rect's square corner
		[TestCase(6, 9.8f)]
		public void AFrameAsThickAsItsRoundCornerKeepsItsThickness( int _segments, float _frame )
		{
			var shape = Build();
			shape.Radius = 10f;
			shape.FrameSize = _frame;
			shape.CornerSegments = _segments;

			Assert.That(Area(shape), Is.EqualTo(RingArea(shape, _frame)).Within(Tolerance));
		}

		[Test]
		public void AFadedFewSegmentFrameCoversItsRingWithoutOverlap()
		{
			const float frame = 12f;
			var shape = Build();
			shape.FrameSize = frame;
			shape.FadeSize = 3f;
			shape.CornerSegments = 2;

			Assert.That(Area(shape), Is.EqualTo(RingArea(shape, frame)).Within(Tolerance));
		}

		[Test]
		public void AFilledFewSegmentShapeWithFadeLeavesNoSeam()
		{
			var shape = Build();
			shape.FadeSize = 5f;
			shape.CornerSegments = 2;

			// The fade ring's inner boundary and the body's corners have to be the same polygon.
			Assert.That(Area(shape), Is.EqualTo(PolygonArea(Outline(shape))).Within(Tolerance));
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

			Assert.That(Area(shape), Is.EqualTo(RingArea(shape, frame)).Within(Tolerance));
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

		/// <summary>
		/// What a frame _frame thick around this shape covers: its outline minus that outline with every edge
		/// moved inwards by _frame. Worked out on the polygon alone, without the shape's own corner maths.
		/// </summary>
		private static float RingArea( UiRoundedImage _shape, float _frame )
		{
			var outline = Outline(_shape);
			return PolygonArea(outline) - PolygonArea(Erode(outline, _frame));
		}

		/// <summary>The shape's outline, counter-clockwise: per corner its arc (one segment if chamfered) or the corner point.</summary>
		private static List<Vector2> Outline( UiRoundedImage _shape )
		{
			float r = _shape.Radius;
			var points = new List<Vector2>();
			AddCorner(points, _shape, CornerFlags.BottomLeft, new Vector2(r, r), new Vector2(0, 0), 180f);
			AddCorner(points, _shape, CornerFlags.BottomRight, new Vector2(Width - r, r), new Vector2(Width, 0), 270f);
			AddCorner(points, _shape, CornerFlags.TopRight, new Vector2(Width - r, Height - r), new Vector2(Width, Height), 0f);
			AddCorner(points, _shape, CornerFlags.TopLeft, new Vector2(r, Height - r), new Vector2(0, Height), 90f);
			return points;
		}

		private static void AddCorner( List<Vector2> _points, UiRoundedImage _shape, CornerFlags _corner, Vector2 _centre, Vector2 _square, float _startDegrees )
		{
			if ((_shape.Corners & _corner) == 0 || _shape.Radius <= 0f)
			{
				_points.Add(_square);
				return;
			}

			int segments = (_shape.ChamferedCorners & _corner) != 0 ? 1 : _shape.CornerSegments;
			for (int i = 0; i <= segments; i++)
			{
				float angle = (_startDegrees + 90f * i / segments) * Mathf.Deg2Rad;
				_points.Add(_centre + _shape.Radius * new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)));
			}
		}

		/// <summary>A convex counter-clockwise polygon with every edge moved inwards by _depth: clipped by each moved edge in turn.</summary>
		private static List<Vector2> Erode( List<Vector2> _polygon, float _depth )
		{
			var result = new List<Vector2>(_polygon);
			for (int i = 0; i < _polygon.Count && result.Count > 0; i++)
			{
				Vector2 a = _polygon[i];
				Vector2 edge = _polygon[(i + 1) % _polygon.Count] - a;
				if (edge.sqrMagnitude < 1e-8f)
					continue;

				var inward = new Vector2(-edge.y, edge.x).normalized;
				float Inside( Vector2 _p ) => Vector2.Dot(_p - a, inward) - _depth;

				var clipped = new List<Vector2>();
				for (int j = 0; j < result.Count; j++)
				{
					Vector2 p = result[j];
					Vector2 q = result[(j + 1) % result.Count];
					float dp = Inside(p);
					float dq = Inside(q);

					if (dp >= 0f)
						clipped.Add(p);
					if ((dp >= 0f) != (dq >= 0f))
						clipped.Add(p + (q - p) * (dp / (dp - dq)));
				}

				result = clipped;
			}

			return result;
		}

		private static float PolygonArea( List<Vector2> _polygon )
		{
			float twice = 0f;
			for (int i = 0; i < _polygon.Count; i++)
			{
				Vector2 a = _polygon[i];
				Vector2 b = _polygon[(i + 1) % _polygon.Count];
				twice += a.x * b.y - b.x * a.y;
			}

			return Mathf.Abs(twice) * 0.5f;
		}

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
