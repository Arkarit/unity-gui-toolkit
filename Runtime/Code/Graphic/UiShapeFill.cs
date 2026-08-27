using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit
{
	/// <summary>
	/// Cuts a generated shape mesh down to a fill amount, the way UnityEngine.UI.Image does it.
	///
	/// Image's own filled geometry never runs on a <see cref="UiShapeImage"/> - the shape overrides both
	/// OnPopulateMesh overloads and UpdateGeometry and builds its own mesh - so Image Type, and with it
	/// fillMethod / fillAmount / fillOrigin / fillClockwise, used to do nothing at all there. This puts them
	/// back by clipping whatever the shape produced, which means every shape gets it - rounded rect, circle,
	/// star - without any of them having to know about fill.
	///
	/// It follows Image's conventions rather than a tidier definition of its own, because a fill that
	/// behaved differently from every other fill in the project would be the worse surprise. Those
	/// conventions were read off Image's actual output, not its source, and three of them are worth knowing:
	///
	/// - The sweep is spent REGION BY REGION. Radial360 fills its first quarter completely before the second
	///   one starts, so the cut inside a region is always a single straight line from the pivot and clipping
	///   stays a half-plane problem.
	/// - A cut is angular in the NORMALIZED space of its region, not in pixels. On a 200x100 rect Image's
	///   "45 degrees" is not 45 degrees on screen, and matching that matters more than being right.
	/// - fillClockwise is ignored by Horizontal and Vertical, as it is on Image.
	///
	/// fillCenter is not used either: on Image it belongs to Sliced and Tiled, not to Filled.
	/// </summary>
	public static class UiShapeFill
	{
		// Corner ids, counter-clockwise from the bottom left. Used both for "which corner of the rect" and
		// for "which corner of a region the pivot sits on".
		private const int BottomLeft = 0;
		private const int TopLeft = 1;
		private const int TopRight = 2;
		private const int BottomRight = 3;

		// Half ids for Radial180.
		private const int LeftHalf = 0;
		private const int TopHalf = 1;
		private const int RightHalf = 2;
		private const int BottomHalf = 3;

		/// <summary>A half-plane, kept where dot(Normal, point) >= Distance.</summary>
		private readonly struct Plane2D
		{
			public Plane2D( Vector2 _normal, float _distance )
			{
				Normal = _normal;
				Distance = _distance;
			}

			public readonly Vector2 Normal;
			public readonly float Distance;

			public float SignedDistance( Vector2 _point ) => Vector2.Dot(Normal, _point) - Distance;
		}

		// Reused across calls: this runs inside mesh generation, which happens on every layout change of
		// every shape on screen.
		private static readonly List<Plane2D> s_planes = new();
		private static readonly List<UiShapeImage.Vertex> s_polygon = new();
		private static readonly List<UiShapeImage.Vertex> s_clipped = new();
		private static readonly List<UiShapeImage.Vertex> s_outVertices = new();
		private static readonly List<int[]> s_outTriangles = new();

		/// <summary>
		/// Clips the mesh in place. Returns false when there was nothing to do, so a full fill - the
		/// overwhelmingly common case - costs one comparison.
		/// </summary>
		public static bool Apply
		(
			List<UiShapeImage.Vertex> _vertices,
			List<int[]> _triangles,
			Rect _rect,
			Image.FillMethod _method,
			float _amount,
			int _origin,
			bool _clockwise
		)
		{
			_amount = Mathf.Clamp01(_amount);

			if (_amount >= 1f)
				return false;

			if (_amount <= 0f || _rect.width <= 0f || _rect.height <= 0f)
			{
				_vertices.Clear();
				_triangles.Clear();
				return true;
			}

			if (_method == Image.FillMethod.Horizontal || _method == Image.FillMethod.Vertical)
				ClipToEdge(_vertices, _triangles, _rect, _method, _amount, _origin);
			else
				ClipToWedge(_vertices, _triangles, _rect, _method, _amount, _origin, _clockwise);

			return true;
		}

		#region Horizontal and vertical

		/// <summary>One straight cut across the rect. Origin 0 is Left / Bottom, 1 is Right / Top.</summary>
		private static void ClipToEdge
		(
			List<UiShapeImage.Vertex> _vertices,
			List<int[]> _triangles,
			Rect _rect,
			Image.FillMethod _method,
			float _amount,
			int _origin
		)
		{
			bool horizontal = _method == Image.FillMethod.Horizontal;
			bool fromMin = _origin == 0;

			var axis = horizontal ? new Vector2(1, 0) : new Vector2(0, 1);
			float min = horizontal ? _rect.xMin : _rect.yMin;
			float size = horizontal ? _rect.width : _rect.height;
			float cut = fromMin ? min + size * _amount : min + size * (1f - _amount);

			s_planes.Clear();
			s_planes.Add(fromMin ? new Plane2D(-axis, -cut) : new Plane2D(axis, cut));

			s_outVertices.Clear();
			s_outTriangles.Clear();
			ClipInto(_vertices, _triangles, s_planes);
			Swap(_vertices, _triangles);
		}

		#endregion

		#region Radial

		/// <summary>
		/// Radial fill as a sequence of regions. Each region is a part of the rect with the pivot on one of
		/// its corners; the sweep is spent on them in turn, and the partly filled one is cut by a single
		/// line from that pivot.
		/// </summary>
		private static void ClipToWedge
		(
			List<UiShapeImage.Vertex> _vertices,
			List<int[]> _triangles,
			Rect _rect,
			Image.FillMethod _method,
			float _amount,
			int _origin,
			bool _clockwise
		)
		{
			int regionCount = _method switch
			{
				Image.FillMethod.Radial90 => 1,
				Image.FillMethod.Radial180 => 2,
				_ => 4,
			};

			s_outVertices.Clear();
			s_outTriangles.Clear();

			for (int step = 0; step < regionCount; step++)
			{
				// How much of THIS region is filled. The sweep is spent in order, so a region is full
				// before the next one starts.
				float here = Mathf.Clamp01(_amount * regionCount - step);
				if (here <= 0f)
					break;

				GetRegion(_rect, _method, _origin, _clockwise, step,
					out Rect region, out Vector2 pivot, out int pivotCorner);

				if (region.width <= 0f || region.height <= 0f)
					continue;

				s_planes.Clear();

				// The region's own bounds, so it cannot claim its neighbour's geometry. Radial90's single
				// region is the whole rect, where those four planes would only cost time.
				if (regionCount > 1)
				{
					s_planes.Add(new Plane2D(new Vector2(1, 0), region.xMin));
					s_planes.Add(new Plane2D(new Vector2(-1, 0), -region.xMax));
					s_planes.Add(new Plane2D(new Vector2(0, 1), region.yMin));
					s_planes.Add(new Plane2D(new Vector2(0, -1), -region.yMax));
				}

				if (here < 1f)
					s_planes.Add(CutPlane(region, pivot, pivotCorner, _clockwise, here));

				ClipInto(_vertices, _triangles, s_planes);
			}

			Swap(_vertices, _triangles);
		}

		/// <summary>
		/// The line the sweep has reached, as a half-plane keeping the side it started from.
		///
		/// The two edges meeting at the pivot are a quarter turn apart, and the angle between them is
		/// measured in the region's UNIT SQUARE - which is what makes a 200x100 rect cut where Image cuts it
		/// rather than where a protractor would.
		/// </summary>
		private static Plane2D CutPlane( Rect _region, Vector2 _pivot, int _pivotCorner, bool _clockwise, float _t )
		{
			// Inward directions along the region's edges from the pivot corner.
			var horizontal = new Vector2(_pivotCorner == BottomLeft || _pivotCorner == TopLeft ? 1 : -1, 0);
			var vertical = new Vector2(0, _pivotCorner == BottomLeft || _pivotCorner == BottomRight ? 1 : -1);

			// Which of the two the sweep leaves from. Read off Image: at an even corner a clockwise sweep
			// starts along the vertical edge, at an odd one along the horizontal, and turning the other way
			// swaps them.
			bool startHorizontal = _pivotCorner % 2 == 1 == _clockwise;
			var start = startHorizontal ? horizontal : vertical;
			var end = startHorizontal ? vertical : horizontal;

			float width = Mathf.Max(_region.width, 1e-4f);
			float height = Mathf.Max(_region.height, 1e-4f);

			var startUnit = new Vector2(start.x / width, start.y / height).normalized;
			var endUnit = new Vector2(end.x / width, end.y / height).normalized;

			float from = Mathf.Atan2(startUnit.y, startUnit.x);
			float to = Mathf.Atan2(endUnit.y, endUnit.x);
			float sweep = Mathf.DeltaAngle(from * Mathf.Rad2Deg, to * Mathf.Rad2Deg) * Mathf.Deg2Rad;

			float angle = from + sweep * _t;
			var unit = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
			var direction = new Vector2(unit.x * width, unit.y * height);

			var normal = new Vector2(-direction.y, direction.x);
			if (Vector2.Dot(normal, start) < 0f)
				normal = -normal;

			return new Plane2D(normal, Vector2.Dot(normal, _pivot));
		}

		/// <summary>
		/// Which part of the rect the sweep is in at this step, where its pivot is, and which corner of that
		/// part the pivot occupies.
		///
		/// The tables are Image's own behaviour, checked against it for every method, origin, direction and
		/// a spread of amounts.
		/// </summary>
		private static void GetRegion
		(
			Rect _rect,
			Image.FillMethod _method,
			int _origin,
			bool _clockwise,
			int _step,
			out Rect _region,
			out Vector2 _pivot,
			out int _pivotCorner
		)
		{
			switch (_method)
			{
				case Image.FillMethod.Radial90:
					// Origin names a corner of the rect, and the single region is the whole thing.
					_pivotCorner = Wrap(_origin);
					_pivot = CornerOf(_rect, _pivotCorner);
					_region = _rect;
					return;

				case Image.FillMethod.Radial180:
				{
					// Origin names an edge; its midpoint is the pivot, and the two regions are the halves to
					// either side. The first one clockwise is the one the origin points along.
					_pivot = EdgeMidpoint(_rect, _origin);

					int firstHalf = Wrap(_origin);
					int secondHalf = Wrap(_origin + 2);
					int half = (_clockwise ? _step == 0 : _step != 0) ? firstHalf : secondHalf;

					_region = HalfRect(_rect, half);

					// On the half the origin points along, the pivot sits one corner back; on the other one
					// it sits on the origin's own corner index.
					_pivotCorner = half == _origin ? Wrap(_origin + 3) : Wrap(_origin);
					return;
				}

				default:
				{
					// Radial360: four quadrants around the centre, so the pivot is always the corner of a
					// quadrant that faces the middle - the opposite one.
					_pivot = _rect.center;

					int start = _origin switch
					{
						1 => _clockwise ? BottomRight : TopRight,       // Right
						2 => _clockwise ? TopRight : TopLeft,           // Top
						3 => _clockwise ? TopLeft : BottomLeft,         // Left
						_ => _clockwise ? BottomLeft : BottomRight,     // Bottom
					};

					int quadrant = Wrap(_clockwise ? start + _step : start - _step);
					_region = QuadrantRect(_rect, quadrant);
					_pivotCorner = Wrap(quadrant + 2);
					return;
				}
			}
		}

		private static Vector2 CornerOf( Rect _rect, int _corner ) => _corner switch
		{
			TopLeft => new Vector2(_rect.xMin, _rect.yMax),
			TopRight => new Vector2(_rect.xMax, _rect.yMax),
			BottomRight => new Vector2(_rect.xMax, _rect.yMin),
			_ => new Vector2(_rect.xMin, _rect.yMin),
		};

		/// <summary>Radial180 origins: 0 Bottom, 1 Left, 2 Top, 3 Right.</summary>
		private static Vector2 EdgeMidpoint( Rect _rect, int _origin ) => Wrap(_origin) switch
		{
			1 => new Vector2(_rect.xMin, _rect.center.y),
			2 => new Vector2(_rect.center.x, _rect.yMax),
			3 => new Vector2(_rect.xMax, _rect.center.y),
			_ => new Vector2(_rect.center.x, _rect.yMin),
		};

		private static Rect HalfRect( Rect _rect, int _half )
		{
			var center = _rect.center;
			return _half switch
			{
				TopHalf => Rect.MinMaxRect(_rect.xMin, center.y, _rect.xMax, _rect.yMax),
				RightHalf => Rect.MinMaxRect(center.x, _rect.yMin, _rect.xMax, _rect.yMax),
				BottomHalf => Rect.MinMaxRect(_rect.xMin, _rect.yMin, _rect.xMax, center.y),
				_ => Rect.MinMaxRect(_rect.xMin, _rect.yMin, center.x, _rect.yMax),
			};
		}

		private static Rect QuadrantRect( Rect _rect, int _quadrant )
		{
			var center = _rect.center;
			return _quadrant switch
			{
				TopLeft => Rect.MinMaxRect(_rect.xMin, center.y, center.x, _rect.yMax),
				TopRight => Rect.MinMaxRect(center.x, center.y, _rect.xMax, _rect.yMax),
				BottomRight => Rect.MinMaxRect(center.x, _rect.yMin, _rect.xMax, center.y),
				_ => Rect.MinMaxRect(_rect.xMin, _rect.yMin, center.x, center.y),
			};
		}

		private static int Wrap( int _value ) => (_value % 4 + 4) % 4;

		#endregion

		#region Clipping

		/// <summary>
		/// Clips every triangle against all planes and appends what survives to the output lists. Called
		/// once per region, so the regions add up.
		/// </summary>
		private static void ClipInto
		(
			List<UiShapeImage.Vertex> _vertices,
			List<int[]> _triangles,
			List<Plane2D> _planes
		)
		{
			foreach (var triangle in _triangles)
			{
				s_polygon.Clear();
				s_polygon.Add(_vertices[triangle[0]]);
				s_polygon.Add(_vertices[triangle[1]]);
				s_polygon.Add(_vertices[triangle[2]]);

				foreach (var plane in _planes)
				{
					ClipPolygon(s_polygon, s_clipped, plane);
					s_polygon.Clear();
					s_polygon.AddRange(s_clipped);

					if (s_polygon.Count < 3)
						break;
				}

				if (s_polygon.Count < 3)
					continue;

				// Clipping a triangle by half-planes leaves a convex polygon, so a fan triangulates it.
				int first = s_outVertices.Count;
				foreach (var vertex in s_polygon)
					s_outVertices.Add(vertex);

				for (int i = 1; i + 1 < s_polygon.Count; i++)
					s_outTriangles.Add(new[] { first, first + i, first + i + 1 });
			}
		}

		private static void Swap( List<UiShapeImage.Vertex> _vertices, List<int[]> _triangles )
		{
			_vertices.Clear();
			_triangles.Clear();
			_vertices.AddRange(s_outVertices);
			_triangles.AddRange(s_outTriangles);
		}

		/// <summary>Sutherland-Hodgman against one half-plane, interpolating everything a vertex carries.</summary>
		private static void ClipPolygon
		(
			List<UiShapeImage.Vertex> _polygon,
			List<UiShapeImage.Vertex> _result,
			Plane2D _plane
		)
		{
			_result.Clear();
			int count = _polygon.Count;

			for (int i = 0; i < count; i++)
			{
				var current = _polygon[i];
				var next = _polygon[(i + 1) % count];

				float dCurrent = _plane.SignedDistance(current.Position);
				float dNext = _plane.SignedDistance(next.Position);

				if (dCurrent >= 0f)
					_result.Add(current);

				// A sign change means the edge crosses the plane, and the crossing point joins the polygon.
				if (dCurrent >= 0f != dNext >= 0f)
				{
					float t = dCurrent / (dCurrent - dNext);
					_result.Add(Lerp(current, next, t));
				}
			}
		}

		private static UiShapeImage.Vertex Lerp( UiShapeImage.Vertex _a, UiShapeImage.Vertex _b, float _t )
			=> new UiShapeImage.Vertex
			{
				Position = Vector2.LerpUnclamped(_a.Position, _b.Position, _t),
				Uv = Vector2.LerpUnclamped(_a.Uv, _b.Uv, _t),
				Color = Color.LerpUnclamped(_a.Color, _b.Color, _t),
			};

		#endregion
	}
}
