using System.Collections.Generic;
using System.Reflection;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit.Test
{
	/// <summary>
	/// Filled shapes, measured against the thing they are supposed to look like.
	///
	/// A UiShapeImage builds its own mesh, so Image's filled geometry never runs on it and Image Type used
	/// to do nothing at all. UiShapeFill puts it back by clipping the finished mesh - and the only sensible
	/// definition of "correct" there is "the same region Image would have covered", because a fill that
	/// behaved differently from every other fill in the project would be the worse surprise.
	///
	/// So the main test is a comparison: a rounded image with radius 0 IS a rectangle, and its clipped mesh
	/// has to cover what Image's filled mesh covers. Area and centroid together pin both how much is filled
	/// and where - two numbers that no plausible wrong implementation gets right by accident, least of all
	/// across a non-square rect, where Image's radial cut is angular in normalised space rather than in
	/// pixels.
	/// </summary>
	[EditorAware]
	public class TestUiShapeFill
	{
		private static readonly Image.FillMethod[] Methods =
		{
			Image.FillMethod.Horizontal,
			Image.FillMethod.Vertical,
			Image.FillMethod.Radial90,
			Image.FillMethod.Radial180,
			Image.FillMethod.Radial360,
		};

		private readonly List<Object> m_created = new();
		private MethodInfo m_imagePopulate;
		private MethodInfo m_shapePopulate;

		[SetUp]
		public void SetUp()
		{
			const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
			m_imagePopulate = typeof(Image).GetMethod("OnPopulateMesh", flags, null,
				new[] { typeof(VertexHelper) }, null);
			m_shapePopulate = typeof(UiShapeImage).GetMethod("OnPopulateMesh", flags, null,
				new[] { typeof(VertexHelper) }, null);

			Assert.That(m_imagePopulate, Is.Not.Null);
			Assert.That(m_shapePopulate, Is.Not.Null);
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
		public void EveryFillSettingCoversWhatImageCovers()
		{
			var complaints = new StringBuilder();
			int compared = 0;

			// Two aspect ratios and a square: the square would let a wrong "angular in pixels" reading pass.
			foreach (var size in new[] { new Vector2(200, 100), new Vector2(100, 100), new Vector2(60, 180) })
			{
				Build(size, out var image, out var shape);

				foreach (var method in Methods)
				{
					image.fillMethod = shape.fillMethod = method;
					int origins = method == Image.FillMethod.Horizontal
					           || method == Image.FillMethod.Vertical ? 2 : 4;

					for (int origin = 0; origin < origins; origin++)
					{
						image.fillOrigin = shape.fillOrigin = origin;

						foreach (bool clockwise in new[] { true, false })
						{
							image.fillClockwise = shape.fillClockwise = clockwise;

							foreach (float amount in new[] { 0.1f, 0.25f, 0.5f, 0.75f, 0.9f })
							{
								image.fillAmount = shape.fillAmount = amount;
								compared++;

								var expected = Measure(m_imagePopulate, image);
								var actual = Measure(m_shapePopulate, shape);

								float full = size.x * size.y;
								bool ok = Mathf.Abs(expected.Area - actual.Area) < full * 0.003f
								       && Vector2.Distance(expected.Centroid, actual.Centroid)
								          < Mathf.Max(size.x, size.y) * 0.01f;

								if (!ok)
								{
									complaints.Append($"{size} {method} origin {origin} ")
										.Append(clockwise ? "cw" : "ccw").Append($" amount {amount}: ")
										.Append($"expected {expected.Area:F0}@{expected.Centroid}, ")
										.Append($"got {actual.Area:F0}@{actual.Centroid}\n");
								}
							}
						}
					}
				}
			}

			Assert.That(compared, Is.GreaterThan(300), "The matrix shrank - is a method missing?");
			Assert.That(complaints.ToString(), Is.Empty);
		}

		[Test]
		public void AFullFillIsTheUntouchedShape()
		{
			Build(new Vector2(200, 100), out _, out var shape);
			shape.Radius = 30;

			shape.type = Image.Type.Simple;
			var plain = Measure(m_shapePopulate, shape);

			shape.type = Image.Type.Filled;
			shape.fillAmount = 1f;
			var full = Measure(m_shapePopulate, shape);

			Assert.That(full.Area, Is.EqualTo(plain.Area).Within(0.01f));
			Assert.That(full.Centroid, Is.EqualTo(plain.Centroid).Using(new Vector2Comparer(0.01f)));
		}

		[Test]
		public void AnEmptyFillDrawsNothing()
		{
			Build(new Vector2(200, 100), out _, out var shape);
			shape.Radius = 30;
			shape.type = Image.Type.Filled;
			shape.fillAmount = 0f;

			Assert.That(Measure(m_shapePopulate, shape).Area, Is.EqualTo(0f).Within(0.01f));
		}

		/// <summary>
		/// The clip works on the finished mesh, so a shape drawn as a ring is clipped as a ring - there is
		/// no separate code path that could forget about it.
		/// </summary>
		[Test]
		public void AFrameIsClippedLikeEverythingElse()
		{
			Build(new Vector2(200, 100), out _, out var shape);
			shape.Radius = 30;
			shape.FrameSize = 8;

			shape.type = Image.Type.Simple;
			float ring = Measure(m_shapePopulate, shape).Area;
			Assert.That(ring, Is.GreaterThan(0));

			shape.type = Image.Type.Filled;
			shape.fillMethod = Image.FillMethod.Radial360;
			shape.fillAmount = 0.5f;

			float half = Measure(m_shapePopulate, shape).Area;
			Assert.That(half, Is.EqualTo(ring * 0.5f).Within(ring * 0.02f),
				"Half a symmetric ring is half its area, not half a disc.");
		}

		[Test]
		public void MoreFillIsNeverLessArea()
		{
			Build(new Vector2(160, 90), out _, out var shape);
			shape.Radius = 20;
			shape.type = Image.Type.Filled;

			foreach (var method in Methods)
			{
				shape.fillMethod = method;
				float previous = -1f;

				for (float amount = 0f; amount <= 1.0001f; amount += 0.05f)
				{
					shape.fillAmount = amount;
					float area = Measure(m_shapePopulate, shape).Area;
					Assert.That(area, Is.GreaterThanOrEqualTo(previous - 0.01f),
						$"{method} went backwards at {amount}");
					previous = area;
				}
			}
		}

		#region Helpers

		private void Build( Vector2 _size, out Image _image, out UiRoundedImage _shape )
		{
			var texture = new Texture2D(4, 4);
			var sprite = Sprite.Create(texture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 100f);
			m_created.Add(texture);
			m_created.Add(sprite);

			var canvas = new GameObject("FillTestCanvas", typeof(RectTransform), typeof(Canvas));
			m_created.Add(canvas);

			_image = NewChild<Image>(canvas, _size);
			_shape = NewChild<UiRoundedImage>(canvas, _size);

			_image.sprite = sprite;
			_image.type = Image.Type.Filled;

			_shape.sprite = sprite;
			_shape.type = Image.Type.Filled;

			// A rounded image with no radius is a rectangle, which is the only shape Image can draw too.
			_shape.Radius = 0;
			_shape.CornerSegments = 1;
			_shape.FrameSize = 0;
			_shape.FadeSize = 0;
		}

		private static T NewChild<T>( GameObject _parent, Vector2 _size ) where T : Component
		{
			var go = new GameObject(typeof(T).Name, typeof(RectTransform));
			go.transform.SetParent(_parent.transform, false);
			((RectTransform)go.transform).sizeDelta = _size;
			return go.AddComponent<T>();
		}

		private readonly struct Coverage
		{
			public Coverage( float _area, Vector2 _centroid )
			{
				Area = _area;
				Centroid = _centroid;
			}

			public float Area { get; }
			public Vector2 Centroid { get; }
		}

		/// <summary>
		/// How much of the plane a mesh covers, and where its weight sits. Overlapping triangles would be
		/// counted twice - neither of these meshes has any.
		/// </summary>
		private static Coverage Measure( MethodInfo _populate, Object _target )
		{
			using var vertexHelper = new VertexHelper();
			_populate.Invoke(_target, new object[] { vertexHelper });

			var stream = new List<UIVertex>();
			vertexHelper.GetUIVertexStream(stream);

			float area = 0f;
			var weighted = Vector2.zero;

			for (int i = 0; i + 2 < stream.Count; i += 3)
			{
				Vector2 a = stream[i].position;
				Vector2 b = stream[i + 1].position;
				Vector2 c = stream[i + 2].position;

				float triangle = 0.5f * Mathf.Abs((b.x - a.x) * (c.y - a.y) - (c.x - a.x) * (b.y - a.y));
				area += triangle;
				weighted += triangle * (a + b + c) / 3f;
			}

			return new Coverage(area, area > 0.0001f ? weighted / area : Vector2.zero);
		}

		private class Vector2Comparer : IEqualityComparer<Vector2>
		{
			private readonly float m_tolerance;

			public Vector2Comparer( float _tolerance ) => m_tolerance = _tolerance;

			public bool Equals( Vector2 _a, Vector2 _b ) => Vector2.Distance(_a, _b) <= m_tolerance;
			public int GetHashCode( Vector2 _v ) => _v.GetHashCode();
		}

		#endregion
	}
}
