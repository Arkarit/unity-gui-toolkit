using System;
using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit
{
	/// <summary>
	/// Create rounded and antialiased images.
	///
	/// In nearly every project, there's a need for rounded images and frames.
	/// This class handles this by creating an image with rounded corners of an arbitrary radius,
	/// and optional frame (hole) functionality and antialiasing.
	/// It works nearly like the original UnityEngine.UI.Image, where it's based on.
	/// You can add a sprite and set a color for the image.
	/// UV coordinates however are always 0/1 and there is no support for sliced, tiled, preserve aspect etc.
	///
	/// It also has some other improvements compared to UnityEngine.UI.Image; it can be disabled etc.
	///
	/// Unfortunately we can not make it an UiThing in C#, which would be a very simple task in a real programming language: just inherit from UiThing and Image.
	/// We also can't handle the improvements via composition. Thus this class is a bit outside of the common UiThing class hierarchy.
	///
	/// Shape-agnostic infrastructure (frame, fade, material handling, UV mapping, IEnableableInHierarchy)
	/// lives in the abstract UiShapeImage base; this class adds the rounded-rectangle geometry.
	///
	/// Each of the four sides can carry a GAP: a stretch of the frame that is simply not emitted, so the
	/// outline is interrupted. Typical use is a border broken by a heading, or a frame that has to let
	/// something through. Gaps interrupt the FRAME; a filled shape has no outline to interrupt, so they are
	/// ignored when FrameSize is 0.
	/// </summary>
	[ExecuteAlways]
	[RequireComponent(typeof(CanvasRenderer))]
	public class UiRoundedImage : UiShapeImage
	{
		public const int MinCornerSegments = 1;
		public const int MaxCornerSegments = 30;

		public const float MinRadius = 0;
		public const float MaxRadius = 200;

		/// <summary>
		/// An interruption in one side of the frame.
		///
		/// Both measures are fractions of that side's full length, so a gap keeps its proportions when the
		/// rect is resized. Width 0.3 is "three tenths of this side". Offset moves the gap along the side
		/// from its CENTRE, so the common case — a gap in the middle — needs no offset at all.
		///
		/// The gap is clamped to the straight part of the side and can therefore never eat into a rounded
		/// corner, however large it is set.
		/// </summary>
		[Serializable]
		public struct EdgeGap
		{
			[Tooltip("Interrupt this side of the frame.")]
			public bool Active;

			[Tooltip("Length of the interruption, as a fraction of this side's full length.")]
			[UnityEngine.Range(0f, 1f)]
			public float Width;

			[Tooltip("Position of the interruption along the side, measured from the side's centre, as a "
			         + "fraction of the side's full length. 0 is centred.")]
			[UnityEngine.Range(-0.5f, 0.5f)]
			public float Offset;

			public static EdgeGap Centered( float _width ) => new EdgeGap { Active = true, Width = _width };
		}

		private enum QuadFade
		{
			None,
			Left,
			Right,
			Top,
			Bottom,
		}

		/// <summary>
		/// A gap resolved to absolute local coordinates along its side's axis.
		///
		/// Resolved ONCE from the outer rect and then handed to every ring, because a frame with fade is
		/// three concentric rings of different size: normalising per ring would cut each one at a slightly
		/// different place and leave the fade edges bridging the gap.
		/// </summary>
		private readonly struct GapSpan
		{
			public readonly bool Active;
			public readonly float From;
			public readonly float To;

			public GapSpan( bool _active, float _from, float _to )
			{
				Active = _active && _to > _from;
				From = _from;
				To = _to;
			}

			public static readonly GapSpan Inactive = new GapSpan(false, 0, 0);
		}

		[Tooltip("Corner segments. The more, the rounder. But keep an eye on performance; "
				 + "more corner segments mean more triangles and longer creation time. "
				 + "Between 5 and 10 should be sufficient for most tasks.")]
		[UnityEngine.Range(MinCornerSegments, MaxCornerSegments)]
		[SerializeField] protected int m_cornerSegments = 5;

		[Tooltip("Corner radius. To work properly, this should always be greater than frame size (when used with frame)")]
		[UnityEngine.Range(MinRadius, MaxRadius)]
		[SerializeField] protected float m_radius = 10;

		[SerializeField] protected EdgeGap m_gapLeft;
		[SerializeField] protected EdgeGap m_gapRight;
		[SerializeField] protected EdgeGap m_gapTop;
		[SerializeField] protected EdgeGap m_gapBottom;

		// Resolved at the start of a frame generation and read by the edge emitters further down. Held as
		// state rather than threaded through six call sites, of which four are recursive over the fade rings.
		private GapSpan m_spanLeft;
		private GapSpan m_spanRight;
		private GapSpan m_spanTop;
		private GapSpan m_spanBottom;

		public int CornerSegments
		{
			get => m_cornerSegments;
			set
			{
				CheckSetterRange(nameof(CornerSegments), value, MinCornerSegments, MaxCornerSegments);
				m_cornerSegments = value;
				SetVerticesDirty();
			}
		}

		public float Radius
		{
			get => m_radius;
			set
			{
				CheckSetterRange(nameof(Radius), value, MinRadius, MaxRadius);
				m_radius = value;
				SetVerticesDirty();
			}
		}

		public EdgeGap GapLeft
		{
			get => m_gapLeft;
			set { m_gapLeft = value; SetVerticesDirty(); }
		}

		public EdgeGap GapRight
		{
			get => m_gapRight;
			set { m_gapRight = value; SetVerticesDirty(); }
		}

		public EdgeGap GapTop
		{
			get => m_gapTop;
			set { m_gapTop = value; SetVerticesDirty(); }
		}

		public EdgeGap GapBottom
		{
			get => m_gapBottom;
			set { m_gapBottom = value; SetVerticesDirty(); }
		}

		public EdgeGap GetGap( ESide2D _side ) => _side switch
		{
			ESide2D.Left => m_gapLeft,
			ESide2D.Right => m_gapRight,
			ESide2D.Top => m_gapTop,
			ESide2D.Bottom => m_gapBottom,
			_ => throw new ArgumentOutOfRangeException(nameof(_side), _side, null),
		};

		public void SetGap( ESide2D _side, EdgeGap _gap )
		{
			switch (_side)
			{
				case ESide2D.Left: m_gapLeft = _gap; break;
				case ESide2D.Right: m_gapRight = _gap; break;
				case ESide2D.Top: m_gapTop = _gap; break;
				case ESide2D.Bottom: m_gapBottom = _gap; break;
				default: throw new ArgumentOutOfRangeException(nameof(_side), _side, null);
			}

			SetVerticesDirty();
		}

		/// <summary>True when at least one side is interrupted; the frame is then not a closed ring.</summary>
		public bool HasAnyGap => m_gapLeft.Active || m_gapRight.Active || m_gapTop.Active || m_gapBottom.Active;

		protected override void GenerateFrame()
		{
			ResolveGapSpans();

			if (Mathf.Approximately(0, m_radius))
			{
				GenerateFrameRect();
				return;
			}

			GenerateFrameRounded();
		}

		protected override void GenerateFilled()
		{
			// A filled shape has no outline, so there is nothing for a gap to interrupt. Cutting a notch
			// instead would need a depth, which the gap deliberately does not have.
			ClearGapSpans();

			if (Mathf.Approximately(0, m_radius))
			{
				GenerateFilledRect();
				return;
			}

			GenerateFilledRounded();
		}

		// ------------------------------------------------------------------------------------------ gaps

		private void ResolveGapSpans()
		{
			var rect = Rect;

			m_spanLeft = Resolve(m_gapLeft, rect.center.y, rect.height);
			m_spanRight = Resolve(m_gapRight, rect.center.y, rect.height);
			m_spanTop = Resolve(m_gapTop, rect.center.x, rect.width);
			m_spanBottom = Resolve(m_gapBottom, rect.center.x, rect.width);
		}

		private void ClearGapSpans()
		{
			m_spanLeft = GapSpan.Inactive;
			m_spanRight = GapSpan.Inactive;
			m_spanTop = GapSpan.Inactive;
			m_spanBottom = GapSpan.Inactive;
		}

		private static GapSpan Resolve( EdgeGap _gap, float _sideCenter, float _sideLength )
		{
			if (!_gap.Active || _gap.Width <= 0f || _sideLength <= 0f)
				return GapSpan.Inactive;

			float half = _gap.Width * _sideLength * 0.5f;
			float center = _sideCenter + _gap.Offset * _sideLength;
			return new GapSpan(true, center - half, center + half);
		}

		private GapSpan SpanOf( ESide2D _side ) => _side switch
		{
			ESide2D.Left => m_spanLeft,
			ESide2D.Right => m_spanRight,
			ESide2D.Top => m_spanTop,
			ESide2D.Bottom => m_spanBottom,
			_ => GapSpan.Inactive,
		};

		/// <summary>
		/// One straight side of the frame, minus its gap.
		///
		/// The gap is clamped to this quad's own extent, which is what keeps corners intact: the quad
		/// already stops where the corner begins, in the square case as well as the rounded one, so no
		/// separate corner handling is needed.
		/// </summary>
		private void AddEdgeQuad( Rect _quad, ESide2D _side, QuadFade _fade = QuadFade.None )
		{
			var span = SpanOf(_side);
			bool horizontal = _side is ESide2D.Top or ESide2D.Bottom;

			float min = horizontal ? _quad.xMin : _quad.yMin;
			float max = horizontal ? _quad.xMax : _quad.yMax;

			float from = Mathf.Clamp(span.From, min, max);
			float to = Mathf.Clamp(span.To, min, max);

			if (!span.Active || to <= from)
			{
				AddQuad(_quad, _fade);
				return;
			}

			// A gap that swallowed the whole side leaves nothing to emit.
			bool hasBefore = from > min;
			bool hasAfter = to < max;

			if (hasBefore)
				AddQuad(WithSpan(_quad, horizontal, min, from), _fade);

			if (hasAfter)
				AddQuad(WithSpan(_quad, horizontal, to, max), _fade);
		}

		private static Rect WithSpan( Rect _quad, bool _horizontal, float _from, float _to ) =>
			_horizontal
				? new Rect(_from, _quad.y, _to - _from, _quad.height)
				: new Rect(_quad.x, _from, _quad.width, _to - _from);

		// -------------------------------------------------------------------------------- square corners

		private void GenerateFrameRect() => GenerateFrameRect(Rect, m_frameSize);

		private void GenerateFrameRect( Rect _rect, float _frameSize )
		{
			if (Mathf.Approximately(0, m_fadeSize))
			{
				GenerateFrameRectSimple(_rect, _frameSize);
				return;
			}

			int startIndex = GenerateFrameRectSimple(_rect, m_fadeSize);
			FadeFrameRect(startIndex, _rect, Fade.Outer);

			_rect = Deflate(_rect, m_fadeSize);
			GenerateFrameRectSimple(_rect, _frameSize - m_fadeSize * 2);

			_rect = Deflate(_rect, _frameSize - m_fadeSize * 2);
			startIndex = GenerateFrameRectSimple(_rect, m_fadeSize);
			FadeFrameRect(startIndex, _rect, Fade.Inner);
		}

		/// <summary>Emits one square ring and returns the index of its first vertex.</summary>
		private int GenerateFrameRectSimple( Rect _rect, float _frameWidth )
		{
			int startIndex = s_vertices.Count;

			var x = _rect.x;
			var y = _rect.y;
			var w = _rect.width;
			var h = _rect.height;

			AddQuad(new Rect(x, y, _frameWidth, _frameWidth));                                   // bottom left
			AddQuad(new Rect(w + x - _frameWidth, y, _frameWidth, _frameWidth), QuadFade.None, true);  // bottom right
			AddQuad(new Rect(x, h + y - _frameWidth, _frameWidth, _frameWidth), QuadFade.None, true);  // top left
			AddQuad(new Rect(w + x - _frameWidth, h + y - _frameWidth, _frameWidth, _frameWidth));     // top right

			AddEdgeQuad(new Rect(x, y + _frameWidth, _frameWidth, h - _frameWidth * 2), ESide2D.Left);
			AddEdgeQuad(new Rect(w + x - _frameWidth, y + _frameWidth, _frameWidth, h - _frameWidth * 2), ESide2D.Right);
			AddEdgeQuad(new Rect(x + _frameWidth, h + y - _frameWidth, w - _frameWidth * 2, _frameWidth), ESide2D.Top);
			AddEdgeQuad(new Rect(x + _frameWidth, y, w - _frameWidth * 2, _frameWidth), ESide2D.Bottom);

			return startIndex;
		}

		/// <summary>
		/// Recolours the outer or inner boundary vertices of a ring that was just emitted.
		///
		/// Takes the ring's first vertex index rather than assuming a vertex count. It used to walk back a
		/// fixed 32 ("the frame is 8 quads"), which stops being true the moment a gap splits a side into
		/// two quads.
		/// </summary>
		private void FadeFrameRect( int _startIndex, Rect _rect, Fade _fade )
		{
			if (_fade == Fade.None)
				return;

			float top = _rect.yMin;
			float bottom = _rect.yMax;
			float left = _rect.xMin;
			float right = _rect.xMax;

			for (int i = _startIndex; i < s_vertices.Count; i++)
			{
				var vertex = s_vertices[i];
				var position = vertex.Position;

				bool onBoundary =
					Mathf.Approximately(left, position.x) ||
					Mathf.Approximately(right, position.x) ||
					Mathf.Approximately(top, position.y) ||
					Mathf.Approximately(bottom, position.y);

				if (_fade == Fade.Inner)
					onBoundary = !onBoundary;

				if (onBoundary)
					vertex.Color = m_fadeColor;
			}
		}

		private void GenerateFilledRect()
		{
			var rect = Rect;
			if (Mathf.Approximately(0, m_fadeSize))
			{
				AddQuad(rect);
				return;
			}

			int startIndex = GenerateFrameRectSimple(rect, m_fadeSize);
			FadeFrameRect(startIndex, rect, Fade.Outer);
			AddQuad(Deflate(rect, m_fadeSize));
		}

		// ------------------------------------------------------------------------------- rounded corners

		private void GenerateFrameRounded()
		{
			if (Mathf.Approximately(0, m_fadeSize))
			{
				GenerateFrameRounded(Rect, m_radius, m_frameSize, Fade.None);
				return;
			}

			var rect = Rect;
			var radius = m_radius;
			GenerateFrameRounded(ref rect, ref radius, m_fadeSize, Fade.Outer);
			GenerateFrameRounded(ref rect, ref radius, m_frameSize - m_fadeSize * 2, Fade.None);
			GenerateFrameRounded(rect, radius, m_fadeSize, Fade.Inner);
		}

		private void GenerateFrameRounded( ref Rect _rect, ref float _radius, float _frameSize, Fade _fade )
		{
			GenerateFrameRounded(_rect, _radius, _frameSize, _fade);
			_rect = Deflate(_rect, _frameSize);
			_radius -= _frameSize;
		}

		private void GenerateFrameRounded( Rect _rect, float _radius, float _frameSize, Fade _fade )
		{
			var x = _rect.x;
			var y = _rect.y;
			var w = _rect.width;
			var h = _rect.height;

			var left = new Rect(x, y + _radius, _frameSize, h - _radius * 2);
			var right = new Rect(w + x - _frameSize, y + _radius, _frameSize, h - _radius * 2);
			var top = new Rect(x + _radius, h + y - _frameSize, w - _radius * 2, _frameSize);
			var bottom = new Rect(x + _radius, y, w - _radius * 2, _frameSize);

			var (fadeLeft, fadeRight, fadeTop, fadeBottom) = EdgeFades(_fade);

			AddEdgeQuad(left, ESide2D.Left, fadeLeft);
			AddEdgeQuad(right, ESide2D.Right, fadeRight);
			AddEdgeQuad(top, ESide2D.Top, fadeTop);
			AddEdgeQuad(bottom, ESide2D.Bottom, fadeBottom);

			AddFrameSegment(_rect, Corner.TopLeft, _frameSize, _radius, _fade);
			AddFrameSegment(_rect, Corner.TopRight, _frameSize, _radius, _fade);
			AddFrameSegment(_rect, Corner.BottomLeft, _frameSize, _radius, _fade);
			AddFrameSegment(_rect, Corner.BottomRight, _frameSize, _radius, _fade);
		}

		/// <summary>
		/// Which way each side's quad fades. Inner rings fade towards the middle of the shape, outer rings
		/// away from it; the four cases used to be a switch repeated per side.
		/// </summary>
		private static (QuadFade left, QuadFade right, QuadFade top, QuadFade bottom) EdgeFades( Fade _fade ) => _fade switch
		{
			Fade.None => (QuadFade.None, QuadFade.None, QuadFade.None, QuadFade.None),
			Fade.Inner => (QuadFade.Right, QuadFade.Left, QuadFade.Bottom, QuadFade.Top),
			Fade.Outer => (QuadFade.Left, QuadFade.Right, QuadFade.Top, QuadFade.Bottom),
			_ => throw new ArgumentOutOfRangeException(nameof(_fade), _fade, null),
		};

		private void GenerateFilledRounded()
		{
			Rect rect = Rect;
			float radius = m_radius;
			if (!Mathf.Approximately(0, m_fadeSize))
				GenerateFrameRounded(ref rect, ref radius, m_fadeSize, Fade.Outer);

			var x = rect.x;
			var y = rect.y;
			var w = rect.width;
			var h = rect.height;
			var cex = rect.center.x;
			var cey = rect.center.y;

			AddTriangle(x, y + radius, cex, cey, x, y + h - radius);
			AddTriangle(x + radius, y + h, cex, cey, x + w - radius, y + h);
			AddTriangle(x + w, y + radius, cex, cey, x + w, y + h - radius);
			AddTriangle(x + radius, y, cex, cey, x + w - radius, y);

			AddSector(rect, Corner.TopLeft, radius);
			AddSector(rect, Corner.TopRight, radius);
			AddSector(rect, Corner.BottomLeft, radius);
			AddSector(rect, Corner.BottomRight, radius);
		}

		/// <summary>Centre of the arc that rounds this corner.</summary>
		private static Vector2 CornerOrigin( Rect _rect, Corner _corner, float _radius ) => _corner switch
		{
			Corner.TopLeft => new Vector2(_rect.x + _radius, _rect.yMax - _radius),
			Corner.TopRight => new Vector2(_rect.xMax - _radius, _rect.yMax - _radius),
			Corner.BottomRight => new Vector2(_rect.xMax - _radius, _rect.y + _radius),
			Corner.BottomLeft => new Vector2(_rect.x + _radius, _rect.y + _radius),
			_ => throw new ArgumentOutOfRangeException(nameof(_corner), _corner, null),
		};

		/// <summary>Angle at which a corner's arc starts, and the step per segment.</summary>
		private (float angle, float increment) CornerSweep( Corner _corner ) =>
			(((int)_corner + 3) * 90 * Mathf.Deg2Rad, 90f / m_cornerSegments * Mathf.Deg2Rad);

		private void AddFrameSegment( Rect _rect, Corner _corner, float _frameSize, float _radius, Fade _fade )
		{
			var origin = CornerOrigin(_rect, _corner, _radius);
			var (angle, increment) = CornerSweep(_corner);

			float radiusInner = _radius - _frameSize;
			for (int i = 0; i < m_cornerSegments; i++)
			{
				float x1 = Mathf.Sin(angle) * _radius + origin.x;
				float y1 = Mathf.Cos(angle) * _radius + origin.y;
				float x3 = Mathf.Sin(angle) * radiusInner + origin.x;
				float y3 = Mathf.Cos(angle) * radiusInner + origin.y;
				angle += increment;
				float x0 = Mathf.Sin(angle) * _radius + origin.x;
				float y0 = Mathf.Cos(angle) * _radius + origin.y;
				float x2 = Mathf.Sin(angle) * radiusInner + origin.x;
				float y2 = Mathf.Cos(angle) * radiusInner + origin.y;
				AddIrregularQuad(x0, y0, x1, y1, x2, y2, x3, y3, _fade);
			}
		}

		private void AddSector( Rect _rect, Corner _corner, float _radius )
		{
			var origin = CornerOrigin(_rect, _corner, _radius);
			var (angle, increment) = CornerSweep(_corner);
			var center = _rect.center;

			for (int i = 0; i < m_cornerSegments; i++)
			{
				float x1 = Mathf.Sin(angle) * _radius + origin.x;
				float y1 = Mathf.Cos(angle) * _radius + origin.y;
				angle += increment;
				float x0 = Mathf.Sin(angle) * _radius + origin.x;
				float y0 = Mathf.Cos(angle) * _radius + origin.y;
				AddTriangle(x0, y0, center.x, center.y, x1, y1);
			}
		}

		// ------------------------------------------------------------------------------------ primitives

		private static Rect Deflate( Rect _rect, float _amount ) =>
			new Rect(_rect.x + _amount, _rect.y + _amount, _rect.width - _amount * 2, _rect.height - _amount * 2);

		private void AddQuad( Rect _rect, QuadFade _fade = QuadFade.None, bool _left = false ) => AddQuad(_rect.min, _rect.max, _fade, _left);

		private void AddQuad( Vector2 _posMin, Vector2 _posMax, QuadFade _fade = QuadFade.None, bool _left = false )
		{
			int startIndex = s_vertices.Count;

			// Which of the four corners get the fade colour, in the order they are added below:
			// min/min, min/max, max/max, max/min.
			switch (_fade)
			{
				case QuadFade.None:
					AddVert(_posMin.x, _posMin.y, color);
					AddVert(_posMin.x, _posMax.y, color);
					AddVert(_posMax.x, _posMax.y, color);
					AddVert(_posMax.x, _posMin.y, color);
					break;
				case QuadFade.Left:
					AddVert(_posMin.x, _posMin.y, m_fadeColor);
					AddVert(_posMin.x, _posMax.y, m_fadeColor);
					AddVert(_posMax.x, _posMax.y, color);
					AddVert(_posMax.x, _posMin.y, color);
					break;
				case QuadFade.Right:
					AddVert(_posMin.x, _posMin.y, color);
					AddVert(_posMin.x, _posMax.y, color);
					AddVert(_posMax.x, _posMax.y, m_fadeColor);
					AddVert(_posMax.x, _posMin.y, m_fadeColor);
					break;
				case QuadFade.Top:
					AddVert(_posMin.x, _posMin.y, color);
					AddVert(_posMin.x, _posMax.y, m_fadeColor);
					AddVert(_posMax.x, _posMax.y, m_fadeColor);
					AddVert(_posMax.x, _posMin.y, color);
					break;
				case QuadFade.Bottom:
					AddVert(_posMin.x, _posMin.y, m_fadeColor);
					AddVert(_posMin.x, _posMax.y, color);
					AddVert(_posMax.x, _posMax.y, color);
					AddVert(_posMax.x, _posMin.y, m_fadeColor);
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(_fade), _fade, null);
			}

			if (_left)
			{
				s_triangles.Add(new[] { startIndex, startIndex + 1, startIndex + 3 });
				s_triangles.Add(new[] { startIndex + 2, startIndex + 3, startIndex + 1 });
				return;
			}

			s_triangles.Add(new[] { startIndex, startIndex + 1, startIndex + 2 });
			s_triangles.Add(new[] { startIndex + 2, startIndex + 3, startIndex });
		}

		private void AddIrregularQuad( float _ax, float _ay, float _bx, float _by, float _cx, float _cy, float _dx, float _dy, Fade _fade )
		{
			int startIndex = s_vertices.Count;

			var effectiveColor = _fade == Fade.Outer ? m_fadeColor : color;

			AddVert(_ax, _ay, effectiveColor);
			AddVert(_bx, _by, effectiveColor);

			effectiveColor = _fade == Fade.Inner ? m_fadeColor : color;

			AddVert(_cx, _cy, effectiveColor);
			AddVert(_dx, _dy, effectiveColor);

			s_triangles.Add(new[] { startIndex + 3, startIndex + 1, startIndex });
			s_triangles.Add(new[] { startIndex + 2, startIndex + 3, startIndex });
		}
	}
}
