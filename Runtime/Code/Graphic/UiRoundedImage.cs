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
	///
	/// The radius applies to the corners in Corners only; the others stay square. A corner in Chamfered
	/// Corners is cut straight instead of rounded - the "one corner bevelled" look that otherwise needs a
	/// sprite per variant. A chamfered frame keeps its thickness across the cut.
	/// </summary>
	[ExecuteAlways]
	[RequireComponent(typeof(CanvasRenderer))]
	public class UiRoundedImage : UiShapeImage
	{
		public const int MinCornerSegments = 1;
		public const int MaxCornerSegments = 30;

		public const float MinRadius = 0;
		public const float MaxRadius = 200;

		public const CornerFlags AllCorners = CornerFlags.TopLeft | CornerFlags.TopRight | CornerFlags.BottomRight | CornerFlags.BottomLeft;

		// A frame ring of thickness f moves a chamfer's cut inwards by f, measured square to the cut. Along
		// the sides of the ring's inner rect that leaves the chamfer f * (2 - sqrt 2) shorter, not f shorter
		// as with an arc.
		private static readonly float ChamferShrink = 2f - Mathf.Sqrt(2f);

		// Where the inner end of a chamfer of a frame f thick lies along a side, past the outer one.
		private static readonly float ChamferReach = Mathf.Sqrt(2f) - 1f;

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
			[Tooltip("Switch this gap on.")]
			public bool Active;

			[Tooltip("Length of the gap. Read as a fraction of the side (0..1) or as pixels, depending "
			         + "on Gap Unit.")]
			public float Size;

			[Tooltip("Position of the gap, measured from the centre. Read as a fraction of the side "
			         + "(-0.5..0.5) or as pixels, depending on Gap Unit.")]
			public float Offset;

			public static EdgeGap Centered( float _size ) => new EdgeGap { Active = true, Size = _size };
		}

		/// <summary>How a gap's Size and Offset are to be read.</summary>
		public enum EGapUnit
		{
			/// <summary>Fraction of the side, so a gap keeps its proportion when the rect resizes.</summary>
			Normalized,

			/// <summary>Absolute pixels, so a gap keeps its measurement when the rect resizes.</summary>
			Pixels,
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

		[Tooltip("The corners the radius applies to. The others stay square.")]
		[SerializeField] protected CornerFlags m_corners = AllCorners;

		[Tooltip("Of the corners above, the ones cut straight (a chamfer) instead of rounded.")]
		[SerializeField] protected CornerFlags m_chamferedCorners;

		[Tooltip("Whether gap sizes and offsets are fractions of the side, or pixels.")]
		[SerializeField] protected EGapUnit m_gapUnit = EGapUnit.Normalized;

		// With a frame, a gap interrupts one SIDE of the outline, so there is one per side.
		[SerializeField] protected EdgeGap m_gapLeft;
		[SerializeField] protected EdgeGap m_gapRight;
		[SerializeField] protected EdgeGap m_gapTop;
		[SerializeField] protected EdgeGap m_gapBottom;

		// Filled, there is no outline to interrupt, so a gap is a BAND cut right through the shape.
		// Both bands at once leave the four corners standing.
		[SerializeField] protected EdgeGap m_gapHorizontal;
		[SerializeField] protected EdgeGap m_gapVertical;

		// Resolved at the start of a frame generation and read by the edge emitters further down. Held as
		// state rather than threaded through six call sites, of which four are recursive over the fade rings.
		private GapSpan m_spanLeft;
		private GapSpan m_spanRight;
		private GapSpan m_spanTop;
		private GapSpan m_spanBottom;

		// The bands of the filled case, each along its own axis: m_bandX cuts x, m_bandY cuts y.
		private GapSpan m_bandX;
		private GapSpan m_bandY;

		// Reused, because a rect is split against the bands several times per mesh rebuild.
		private static readonly float[] s_xPieces = new float[4];
		private static readonly float[] s_yPieces = new float[4];

		// Scratch for the shaped corners, indexed by Corner: radii, how far each corner piece reaches along
		// its sides, the cuts of the filled body, and one corner piece's outer and inner boundary.
		private static readonly float[] s_radii = new float[4];
		private static readonly float[] s_insets = new float[4];
		private static readonly float[] s_cutsX = new float[6];
		private static readonly float[] s_cutsY = new float[6];
		private static readonly System.Collections.Generic.List<Vector2> s_outer = new();
		private static readonly System.Collections.Generic.List<Vector2> s_inner = new();

		public int CornerSegments
		{
			get => m_cornerSegments;
			set
			{
				value = ClampSetterRange(nameof(CornerSegments), value, MinCornerSegments, MaxCornerSegments);

				if (m_cornerSegments == value)
					return;

				m_cornerSegments = value;
				SetVerticesDirty();
			}
		}

		public float Radius
		{
			get => m_radius;
			set
			{
				value = ClampSetterRange(nameof(Radius), value, MinRadius, MaxRadius);

				if (m_radius == value)
					return;

				m_radius = value;
				SetVerticesDirty();
			}
		}

		/// <summary>The corners the radius applies to. The others stay square.</summary>
		public CornerFlags Corners
		{
			get => m_corners;
			set
			{
				if (m_corners == value)
					return;

				m_corners = value;
				SetVerticesDirty();
			}
		}

		/// <summary>The corners cut straight instead of rounded. Only those also in <see cref="Corners"/> count.</summary>
		public CornerFlags ChamferedCorners
		{
			get => m_chamferedCorners;
			set
			{
				if (m_chamferedCorners == value)
					return;

				m_chamferedCorners = value;
				SetVerticesDirty();
			}
		}

		public EdgeGap GapLeft
		{
			get => m_gapLeft;
			set
			{
				if (SameGap(m_gapLeft, value))
					return;

				m_gapLeft = value;
				SetVerticesDirty();
			}
		}

		public EdgeGap GapRight
		{
			get => m_gapRight;
			set
			{
				if (SameGap(m_gapRight, value))
					return;

				m_gapRight = value;
				SetVerticesDirty();
			}
		}

		public EdgeGap GapTop
		{
			get => m_gapTop;
			set
			{
				if (SameGap(m_gapTop, value))
					return;

				m_gapTop = value;
				SetVerticesDirty();
			}
		}

		public EdgeGap GapBottom
		{
			get => m_gapBottom;
			set
			{
				if (SameGap(m_gapBottom, value))
					return;

				m_gapBottom = value;
				SetVerticesDirty();
			}
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
			if (SameGap(GetGap(_side), _gap))
				return;

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

		// EdgeGap is a plain struct, so the default Equals boxes and reflects. Spelled out because these
		// setters are on the hot path: a style applier writes every value it owns on every Apply().
		private static bool SameGap( EdgeGap _a, EdgeGap _b )
			=> _a.Active == _b.Active && _a.Size == _b.Size && _a.Offset == _b.Offset;

		public EdgeGap GapHorizontal
		{
			get => m_gapHorizontal;
			set
			{
				if (SameGap(m_gapHorizontal, value))
					return;

				m_gapHorizontal = value;
				SetVerticesDirty();
			}
		}

		public EdgeGap GapVertical
		{
			get => m_gapVertical;
			set
			{
				if (SameGap(m_gapVertical, value))
					return;

				m_gapVertical = value;
				SetVerticesDirty();
			}
		}

		public EGapUnit GapUnit
		{
			get => m_gapUnit;
			set
			{
				if (m_gapUnit == value)
					return;

				m_gapUnit = value;
				SetVerticesDirty();
			}
		}

		/// <summary>True when at least one side of the frame is interrupted.</summary>
		public bool HasAnySideGap => m_gapLeft.Active || m_gapRight.Active || m_gapTop.Active || m_gapBottom.Active;

		/// <summary>True when at least one band cuts through the filled shape.</summary>
		public bool HasAnyBandGap => m_gapHorizontal.Active || m_gapVertical.Active;

		/// <summary>True when a gap is in effect for the mode this shape is currently in.</summary>
		public bool HasAnyGap => m_frameSize > 0 ? HasAnySideGap : HasAnyBandGap;

		protected override void GenerateFrame()
		{
			ResolveGapSpans();

			if (!HasShapedCorner)
			{
				GenerateFrameRect();
				return;
			}

			GenerateFrameRounded();
		}

		/// <summary>True when at least one corner is rounded or chamfered, so the shape is not a plain rectangle.</summary>
		public bool HasShapedCorner => m_radius > 0f && (m_corners & AllCorners) != 0;

		private bool HasCorner( Corner _corner ) => (m_corners & Flag(_corner)) != 0;

		private bool IsChamfered( Corner _corner ) => (m_chamferedCorners & Flag(_corner)) != 0;

		private static CornerFlags Flag( Corner _corner ) => _corner switch
		{
			Corner.TopLeft => CornerFlags.TopLeft,
			Corner.TopRight => CornerFlags.TopRight,
			Corner.BottomRight => CornerFlags.BottomRight,
			Corner.BottomLeft => CornerFlags.BottomLeft,
			_ => throw new ArgumentOutOfRangeException(nameof(_corner), _corner, null),
		};

		/// <summary>The radius of each corner, indexed by <see cref="Corner"/>: the radius where it applies, else 0.</summary>
		private void CornerRadii( float[] _radii )
		{
			float r = Mathf.Max(0f, m_radius);
			for (int i = 0; i < 4; i++)
				_radii[i] = HasCorner((Corner)i) ? r : 0f;
		}

		/// <summary>The radii of the next ring inwards, after one of thickness _frameSize.</summary>
		private void ShrinkRadii( float[] _radii, float _frameSize )
		{
			for (int i = 0; i < 4; i++)
				_radii[i] = Mathf.Max(0f, _radii[i] - _frameSize * (IsChamfered((Corner)i) ? ChamferShrink : 1f));
		}

		private static bool AnyRadius( float[] _radii ) => _radii[0] > 0f || _radii[1] > 0f || _radii[2] > 0f || _radii[3] > 0f;

		protected override void GenerateFilled()
		{
			// Filled, a gap cannot interrupt an outline -- there is none. It cuts straight through instead,
			// which needs no depth: one band horizontally, one vertically, both together leave the corners.
			ResolveBandSpans();

			if (!HasShapedCorner)
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
			m_bandX = GapSpan.Inactive;
			m_bandY = GapSpan.Inactive;
		}

		/// <summary>
		/// The two bands of the filled case, plus what they do to a ring the fade may have added.
		///
		/// Bands are clamped to the straight run of their axis, so a corner disc is never cut into. That
		/// clamp is also what makes the maximum useful: at full size the two bands leave exactly the four
		/// rounded corners standing.
		/// </summary>
		private void ResolveBandSpans()
		{
			ClearGapSpans();

			var rect = Rect;
			CornerRadii(s_radii);
			float tl = s_radii[(int)Corner.TopLeft];
			float tr = s_radii[(int)Corner.TopRight];
			float br = s_radii[(int)Corner.BottomRight];
			float bl = s_radii[(int)Corner.BottomLeft];

			// Each band stays clear of the larger corner at either of its ends.
			m_bandY = ClampSpan(Resolve(m_gapHorizontal, rect.center.y, rect.height),
				rect.yMin + Mathf.Max(bl, br), rect.yMax - Mathf.Max(tl, tr));
			m_bandX = ClampSpan(Resolve(m_gapVertical, rect.center.x, rect.width),
				rect.xMin + Mathf.Max(tl, bl), rect.xMax - Mathf.Max(tr, br));

			// A horizontal band crosses the LEFT and RIGHT sides of a ring, a vertical one TOP and BOTTOM.
			// Saying it that way lets the existing side machinery cut the fade ring for free.
			m_spanLeft = m_spanRight = m_bandY;
			m_spanTop = m_spanBottom = m_bandX;
		}

		private GapSpan Resolve( EdgeGap _gap, float _sideCenter, float _sideLength )
		{
			if (!_gap.Active || _gap.Size <= 0f || _sideLength <= 0f)
				return GapSpan.Inactive;

			// Normalized keeps its proportion when the rect resizes; pixels keep their measurement.
			bool normalized = m_gapUnit == EGapUnit.Normalized;
			float size = normalized ? _gap.Size * _sideLength : _gap.Size;
			float offset = normalized ? _gap.Offset * _sideLength : _gap.Offset;

			float center = _sideCenter + offset;
			return new GapSpan(true, center - size * 0.5f, center + size * 0.5f);
		}

		private static GapSpan ClampSpan( GapSpan _span, float _min, float _max )
		{
			if (!_span.Active || _max <= _min)
				return GapSpan.Inactive;

			return new GapSpan(true, Mathf.Clamp(_span.From, _min, _max), Mathf.Clamp(_span.To, _min, _max));
		}

		/// <summary>
		/// Writes the one or two ranges of [_min.._max] that survive _span, as from/to pairs, and returns
		/// how many there are.
		/// </summary>
		private static int Pieces( float _min, float _max, GapSpan _span, float[] _out )
		{
			float from = Mathf.Clamp(_span.From, _min, _max);
			float to = Mathf.Clamp(_span.To, _min, _max);

			if (!_span.Active || to <= from)
			{
				_out[0] = _min;
				_out[1] = _max;
				return 1;
			}

			int n = 0;
			if (from > _min)
			{
				_out[0] = _min;
				_out[1] = from;
				n++;
			}

			if (to < _max)
			{
				_out[n * 2] = to;
				_out[n * 2 + 1] = _max;
				n++;
			}

			return n;
		}

		/// <summary>A rectangle with both bands taken out of it: one, two or four pieces.</summary>
		private void AddRectMinusBands( Rect _rect )
		{
			if (_rect.width <= 0.0001f || _rect.height <= 0.0001f)
				return;

			int nx = Pieces(_rect.xMin, _rect.xMax, m_bandX, s_xPieces);
			int ny = Pieces(_rect.yMin, _rect.yMax, m_bandY, s_yPieces);

			for (int i = 0; i < nx; i++)
				for (int j = 0; j < ny; j++)
					AddQuad(new Rect(s_xPieces[i * 2], s_yPieces[j * 2],
						s_xPieces[i * 2 + 1] - s_xPieces[i * 2],
						s_yPieces[j * 2 + 1] - s_yPieces[j * 2]));
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
			// A frame thick enough to swallow the side leaves no quad to draw, and a negative one would
			// come out inside-out.
			if (_quad.width <= 0.0001f || _quad.height <= 0.0001f)
				return;

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
				AddRectMinusBands(rect);
				return;
			}

			int startIndex = GenerateFrameRectSimple(rect, m_fadeSize);
			FadeFrameRect(startIndex, rect, Fade.Outer);
			AddRectMinusBands(Deflate(rect, m_fadeSize));
		}

		// -------------------------------------------------------------------------------- shaped corners
		//
		// Every corner is built in its own frame of reference: p runs along one side away from the corner,
		// q along the other (see ToWorld). In it all four corners are the same shape, so one routine draws a
		// round, a chamfered and a square corner for any of them.

		private void GenerateFrameRounded()
		{
			var rect = Rect;
			CornerRadii(s_radii);

			if (Mathf.Approximately(0, m_fadeSize))
			{
				GenerateRing(rect, s_radii, m_frameSize, Fade.None);
				return;
			}

			GenerateRing(ref rect, s_radii, m_fadeSize, Fade.Outer);
			GenerateRing(ref rect, s_radii, m_frameSize - m_fadeSize * 2, Fade.None);
			GenerateRing(rect, s_radii, m_fadeSize, Fade.Inner);
		}

		private void GenerateRing( ref Rect _rect, float[] _radii, float _frameSize, Fade _fade )
		{
			GenerateRing(_rect, _radii, _frameSize, _fade);
			_rect = Deflate(_rect, _frameSize);

			// A ring thicker than a corner leaves nothing shaped behind it there. Letting the radius go
			// negative is what made the next ring fold through its own corners.
			ShrinkRadii(_radii, _frameSize);
		}

		private void GenerateRing( Rect _rect, float[] _radii, float _frameSize, Fade _fade )
		{
			// Earlier rings used every corner up, so this one is a plain rectangular ring, which the square
			// code draws exactly.
			if (!AnyRadius(_radii))
			{
				int startIndex = GenerateFrameRectSimple(_rect, _frameSize);
				FadeFrameRect(startIndex, _rect, _fade);
				return;
			}

			// A frame thicker than half the shape would have its two inner corners cross each other.
			float inner = Mathf.Min(_frameSize, Mathf.Min(_rect.width, _rect.height) * 0.5f);

			// Each corner piece reports how far along its two sides it reaches; the straight sides fill
			// the rest.
			for (int i = 0; i < 4; i++)
				s_insets[i] = AddFrameCorner(_rect, (Corner)i, _radii[i], inner, _fade);

			float tl = s_insets[(int)Corner.TopLeft];
			float tr = s_insets[(int)Corner.TopRight];
			float br = s_insets[(int)Corner.BottomRight];
			float bl = s_insets[(int)Corner.BottomLeft];

			var x = _rect.x;
			var y = _rect.y;
			var w = _rect.width;
			var h = _rect.height;

			var left = new Rect(x, y + bl, _frameSize, h - bl - tl);
			var right = new Rect(w + x - _frameSize, y + br, _frameSize, h - br - tr);
			var top = new Rect(x + tl, h + y - _frameSize, w - tl - tr, _frameSize);
			var bottom = new Rect(x + bl, y, w - bl - br, _frameSize);

			var (fadeLeft, fadeRight, fadeTop, fadeBottom) = EdgeFades(_fade);

			AddEdgeQuad(left, ESide2D.Left, fadeLeft);
			AddEdgeQuad(right, ESide2D.Right, fadeRight);
			AddEdgeQuad(top, ESide2D.Top, fadeTop);
			AddEdgeQuad(bottom, ESide2D.Bottom, fadeBottom);
		}

		/// <summary>
		/// One corner piece of a frame ring: a strip between the ring's outer and inner boundary in that
		/// corner. Returns how far along both sides the piece reaches, which is where the straight sides start.
		/// </summary>
		private float AddFrameCorner( Rect _rect, Corner _corner, float _radius, float _frameSize, Fade _fade )
		{
			s_outer.Clear();
			s_inner.Clear();

			float r = _radius;
			float f = _frameSize;
			float reach;

			// The corner of the ring's inner rectangle. Where the frame is thicker than the corner is
			// deep, the inner boundary IS that corner, and the piece fans onto it.
			var apex = new Vector2(f, f);

			if (r <= 0f)
			{
				reach = f;
				s_outer.Add(new Vector2(f, 0f));
				s_outer.Add(Vector2.zero);
				s_outer.Add(new Vector2(0f, f));
				Repeat(s_inner, apex, 3);
			}
			else if (IsChamfered(_corner))
			{
				// The cut moved inwards by f, square to itself, meets the inner sides further from the
				// corner than the outer cut meets the outer ones. Taking the inner cut as the outer one
				// moved along the sides by f instead is what thins a chamfered frame on the diagonal.
				float innerEnd = r + f * ChamferReach;
				reach = Mathf.Max(innerEnd, f);

				s_outer.Add(new Vector2(reach, 0f));
				s_outer.Add(new Vector2(r, 0f));
				s_outer.Add(new Vector2(0f, r));
				s_outer.Add(new Vector2(0f, reach));

				if (innerEnd > f)
				{
					s_inner.Add(new Vector2(reach, f));
					s_inner.Add(new Vector2(reach, f));
					s_inner.Add(new Vector2(f, reach));
					s_inner.Add(new Vector2(f, reach));
				}
				else
				{
					Repeat(s_inner, apex, 4);
				}
			}
			else if (f < r)
			{
				reach = r;
				for (int i = 0; i <= m_cornerSegments; i++)
				{
					s_outer.Add(ArcPoint(r, r, i, m_cornerSegments));
					s_inner.Add(ArcPoint(r, r - f, i, m_cornerSegments));
				}
			}
			else
			{
				// Past the radius the inner arc would have a NEGATIVE radius and fold through the corner's
				// own centre. The piece runs over the arc AND over the straight bit of each side between
				// the arc's end and the inset corner; without those two points it would stop at the arc
				// and leave a notch where it meets the straight sides.
				reach = f;
				s_outer.Add(new Vector2(f, 0f));
				for (int i = 0; i <= m_cornerSegments; i++)
					s_outer.Add(ArcPoint(r, r, i, m_cornerSegments));
				s_outer.Add(new Vector2(0f, f));
				Repeat(s_inner, apex, s_outer.Count);
			}

			var outerColor = _fade == Fade.Outer ? m_fadeColor : color;
			var innerColor = _fade == Fade.Inner ? m_fadeColor : color;

			for (int i = 0; i < s_outer.Count - 1; i++)
			{
				var o0 = ToWorld(_rect, _corner, s_outer[i]);
				var o1 = ToWorld(_rect, _corner, s_outer[i + 1]);
				var i0 = ToWorld(_rect, _corner, s_inner[i]);
				var i1 = ToWorld(_rect, _corner, s_inner[i + 1]);

				AddTriangleClockwise(o0, o1, i1, outerColor, outerColor, innerColor);
				AddTriangleClockwise(o0, i1, i0, outerColor, innerColor, innerColor);
			}

			return reach;
		}

		private static void Repeat( System.Collections.Generic.List<Vector2> _list, Vector2 _point, int _count )
		{
			for (int i = 0; i < _count; i++)
				_list.Add(_point);
		}

		/// <summary>
		/// Point _index of _segments on a quarter arc of _arcRadius around the corner's centre, which sits
		/// at (_cornerRadius, _cornerRadius). Index 0 lies on the p side, the last on the q side.
		/// </summary>
		private static Vector2 ArcPoint( float _cornerRadius, float _arcRadius, int _index, int _segments )
		{
			float angle = 0.5f * Mathf.PI * _index / _segments;
			return new Vector2(_cornerRadius - _arcRadius * Mathf.Sin(angle), _cornerRadius - _arcRadius * Mathf.Cos(angle));
		}

		/// <summary>
		/// A corner's own coordinates in the shape's: p along one of its sides away from the corner, q along
		/// the other, both into the shape.
		/// </summary>
		private static Vector2 ToWorld( Rect _rect, Corner _corner, Vector2 _pq ) => _corner switch
		{
			Corner.TopLeft => new Vector2(_rect.xMin + _pq.y, _rect.yMax - _pq.x),
			Corner.TopRight => new Vector2(_rect.xMax - _pq.x, _rect.yMax - _pq.y),
			Corner.BottomRight => new Vector2(_rect.xMax - _pq.y, _rect.yMin + _pq.x),
			Corner.BottomLeft => new Vector2(_rect.xMin + _pq.x, _rect.yMin + _pq.y),
			_ => throw new ArgumentOutOfRangeException(nameof(_corner), _corner, null),
		};

		/// <summary>
		/// A triangle wound clockwise like every other in the mesh, whatever order the points come in.
		/// Triangles of no area are dropped: the strips repeat inner points on purpose, and a degenerate
		/// triangle's winding would be decided by rounding error.
		/// </summary>
		private void AddTriangleClockwise( Vector2 _a, Vector2 _b, Vector2 _c, Color _colorA, Color _colorB, Color _colorC )
		{
			float cross = (_b.x - _a.x) * (_c.y - _a.y) - (_b.y - _a.y) * (_c.x - _a.x);
			if (Mathf.Abs(cross) < 0.0001f)
				return;

			if (cross > 0f)
			{
				AddTriangle(_a, _c, _b, _colorA, _colorC, _colorB);
				return;
			}

			AddTriangle(_a, _b, _c, _colorA, _colorB, _colorC);
		}

		private void AddTriangle( Vector2 _a, Vector2 _b, Vector2 _c, Color _colorA, Color _colorB, Color _colorC )
		{
			int startIndex = s_vertices.Count;
			AddVert(_a.x, _a.y, _colorA);
			AddVert(_b.x, _b.y, _colorB);
			AddVert(_c.x, _c.y, _colorC);
			s_triangles.Add(new[] { startIndex, startIndex + 1, startIndex + 2 });
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
			CornerRadii(s_radii);
			if (!Mathf.Approximately(0, m_fadeSize))
				GenerateRing(ref rect, s_radii, m_fadeSize, Fade.Outer);

			// A filled shape tiles exactly as rectangles plus one fan per shaped corner. Rectangles split
			// against a band trivially, and the fans sit in the corners, which the bands are clamped away
			// from. (One fan from the centre, as this once was, cannot be cut at all.)
			AddFilledBody(rect, s_radii);

			for (int i = 0; i < 4; i++)
				AddCornerFan(rect, (Corner)i, s_radii[i]);
		}

		/// <summary>
		/// Everything of a filled shape but its shaped corners, as rectangles.
		///
		/// The rect is cut along every corner square's edges. A cell inside a corner square is the fan's;
		/// the cells left in a row are merged, so equal corners still come out as the three rectangles
		/// they always were.
		/// </summary>
		private void AddFilledBody( Rect _rect, float[] _radii )
		{
			float tl = _radii[(int)Corner.TopLeft];
			float tr = _radii[(int)Corner.TopRight];
			float br = _radii[(int)Corner.BottomRight];
			float bl = _radii[(int)Corner.BottomLeft];

			int nx = Cuts(s_cutsX, _rect.xMin, _rect.xMax, _rect.xMin + tl, _rect.xMin + bl, _rect.xMax - tr, _rect.xMax - br);
			int ny = Cuts(s_cutsY, _rect.yMin, _rect.yMax, _rect.yMin + bl, _rect.yMin + br, _rect.yMax - tl, _rect.yMax - tr);

			for (int j = 0; j < ny - 1; j++)
			{
				float y0 = s_cutsY[j];
				float y1 = s_cutsY[j + 1];
				float cy = (y0 + y1) * 0.5f;
				int start = -1;

				// One step past the last cell, to flush the run that reaches the right side.
				for (int i = 0; i < nx; i++)
				{
					bool inBody = i < nx - 1 && !InCornerSquare(_rect, _radii, (s_cutsX[i] + s_cutsX[i + 1]) * 0.5f, cy);

					if (inBody && start < 0)
					{
						start = i;
					}
					else if (!inBody && start >= 0)
					{
						AddRectMinusBands(new Rect(s_cutsX[start], y0, s_cutsX[i] - s_cutsX[start], y1 - y0));
						start = -1;
					}
				}
			}
		}

		/// <summary>
		/// _min, _max and the four cuts between them, sorted and without duplicates. Returns the count.
		/// </summary>
		private static int Cuts( float[] _out, float _min, float _max, float _a, float _b, float _c, float _d )
		{
			_out[0] = _min;
			_out[1] = _max;
			_out[2] = Mathf.Clamp(_a, _min, _max);
			_out[3] = Mathf.Clamp(_b, _min, _max);
			_out[4] = Mathf.Clamp(_c, _min, _max);
			_out[5] = Mathf.Clamp(_d, _min, _max);
			Array.Sort(_out, 0, 6);

			int count = 1;
			for (int i = 1; i < 6; i++)
			{
				if (_out[i] - _out[count - 1] > 0.0001f)
					_out[count++] = _out[i];
			}

			return count;
		}

		private static bool InCornerSquare( Rect _rect, float[] _radii, float _x, float _y )
		{
			float tl = _radii[(int)Corner.TopLeft];
			float tr = _radii[(int)Corner.TopRight];
			float br = _radii[(int)Corner.BottomRight];
			float bl = _radii[(int)Corner.BottomLeft];

			return (_x < _rect.xMin + tl && _y > _rect.yMax - tl)
			       || (_x > _rect.xMax - tr && _y > _rect.yMax - tr)
			       || (_x > _rect.xMax - br && _y < _rect.yMin + br)
			       || (_x < _rect.xMin + bl && _y < _rect.yMin + bl);
		}

		/// <summary>
		/// A shaped corner of a filled shape, fanned from the centre of its arc. A chamfer is the same fan
		/// with a single segment: one triangle that leaves the cut-off tip out.
		/// </summary>
		private void AddCornerFan( Rect _rect, Corner _corner, float _radius )
		{
			if (_radius <= 0f)
				return;

			int segments = IsChamfered(_corner) ? 1 : m_cornerSegments;
			var centre = ToWorld(_rect, _corner, new Vector2(_radius, _radius));
			var previous = ToWorld(_rect, _corner, ArcPoint(_radius, _radius, 0, segments));

			for (int i = 1; i <= segments; i++)
			{
				var next = ToWorld(_rect, _corner, ArcPoint(_radius, _radius, i, segments));
				AddTriangleClockwise(previous, centre, next, color, color, color);
				previous = next;
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
