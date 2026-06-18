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
	/// </summary>
	[ExecuteAlways]
	[RequireComponent(typeof(CanvasRenderer))]
	public class UiRoundedImage : UiShapeImage
	{
		public const int MinCornerSegments = 1;
		public const int MaxCornerSegments = 30;

		public const float MinRadius = 0;
		public const float MaxRadius = 200;

		private enum QuadFade
		{
			None,
			Left,
			Right,
			Top,
			Bottom,
		}

		[Tooltip("Corner segments. The more, the rounder. But keep an eye on performance; "
				 + "more corner segments mean more triangles and longer creation time. "
				 + "Between 5 and 10 should be sufficient for most tasks.")]
		[UnityEngine.Range(MinCornerSegments, MaxCornerSegments)]
		[SerializeField] protected int m_cornerSegments = 5;

		[Tooltip("Corner radius. To work properly, this should always be greater than frame size (when used with frame)")]
		[UnityEngine.Range(MinRadius, MaxRadius)]
		[SerializeField] protected float m_radius = 10;

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

		protected override void GenerateFrame()
		{
			if (Mathf.Approximately(0, m_radius))
			{
				GenerateFrameRect();
				return;
			}

			GenerateFrameRounded();
		}

		protected override void GenerateFilled()
		{
			if (Mathf.Approximately(0, m_radius))
			{
				GenerateFilledRect();
				return;
			}

			GenerateFilledRounded();
		}

		private void GenerateFrameRect() => GenerateFrameRect(Rect, m_frameSize);

		private void GenerateFrameRect( Rect _rect, float _frameSize )
		{
			if (!Mathf.Approximately(0, m_fadeSize))
			{
				GenerateFrameRectSimple(_rect, m_fadeSize);
				FadeFrameRect(_rect, Fade.Outer);
				_rect.x += m_fadeSize;
				_rect.y += m_fadeSize;
				_rect.width -= m_fadeSize * 2;
				_rect.height -= m_fadeSize * 2;
				GenerateFrameRectSimple(_rect, _frameSize - m_fadeSize * 2);
				_rect.x += _frameSize - m_fadeSize * 2;
				_rect.y += _frameSize - m_fadeSize * 2;
				_rect.width -= (_frameSize - m_fadeSize * 2) * 2;
				_rect.height -= (_frameSize - m_fadeSize * 2) * 2;
				GenerateFrameRectSimple(_rect, m_fadeSize);
				FadeFrameRect(_rect, Fade.Inner);

				return;
			}

			GenerateFrameRectSimple(_rect, _frameSize);
		}

		private void GenerateFrameRectSimple( Rect _rect, float _frameWidth )
		{
			var x = _rect.x;
			var y = _rect.y;
			var w = _rect.width;
			var h = _rect.height;

			Rect bl = new Rect(x, y, _frameWidth, _frameWidth);
			Rect br = new Rect(w + x - _frameWidth, y, _frameWidth, _frameWidth);
			Rect tl = new Rect(x, h + y - _frameWidth, _frameWidth, _frameWidth);
			Rect tr = new Rect(w + x - _frameWidth, h + y - _frameWidth, _frameWidth, _frameWidth);

			AddQuad(bl);
			AddQuad(br, QuadFade.None, true);
			AddQuad(tl, QuadFade.None, true);
			AddQuad(tr);

			Rect l = new Rect(x, y + _frameWidth, _frameWidth, h - _frameWidth * 2);
			Rect r = new Rect(w + x - _frameWidth, y + _frameWidth, _frameWidth, h - _frameWidth * 2);
			Rect t = new Rect(x + _frameWidth, h + y - _frameWidth, w - _frameWidth * 2, _frameWidth);
			Rect b = new Rect(x + _frameWidth, y, w - _frameWidth * 2, _frameWidth);

			AddQuad(l);
			AddQuad(r);
			AddQuad(t);
			AddQuad(b);
		}

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
			_rect.x += _frameSize;
			_rect.y += _frameSize;
			_rect.width -= _frameSize * 2;
			_rect.height -= _frameSize * 2;
			_radius -= _frameSize;
		}

		private void GenerateFrameRounded( Rect _rect, float _radius, float _frameSize, Fade _fade )
		{
			var x = _rect.x;
			var y = _rect.y;
			var w = _rect.width;
			var h = _rect.height;

			Rect l = new Rect(x, y + _radius, _frameSize, h - _radius * 2);
			Rect r = new Rect(w + x - _frameSize, y + _radius, _frameSize, h - _radius * 2);
			Rect t = new Rect(x + _radius, h + y - _frameSize, w - _radius * 2, _frameSize);
			Rect b = new Rect(x + _radius, y, w - _radius * 2, _frameSize);

			switch (_fade)
			{
				case Fade.None:
					AddQuad(l);
					AddQuad(r);
					AddQuad(t);
					AddQuad(b);
					break;
				case Fade.Inner:
					AddQuad(l, QuadFade.Right);
					AddQuad(r, QuadFade.Left);
					AddQuad(t, QuadFade.Bottom);
					AddQuad(b, QuadFade.Top);
					break;
				case Fade.Outer:
					AddQuad(l, QuadFade.Left);
					AddQuad(r, QuadFade.Right);
					AddQuad(t, QuadFade.Top);
					AddQuad(b, QuadFade.Bottom);
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(_fade), _fade, null);
			}

			AddFrameSegment(_rect, Corner.TopLeft, _frameSize, _radius, _fade);
			AddFrameSegment(_rect, Corner.TopRight, _frameSize, _radius, _fade);
			AddFrameSegment(_rect, Corner.BottomLeft, _frameSize, _radius, _fade);
			AddFrameSegment(_rect, Corner.BottomRight, _frameSize, _radius, _fade);
		}

		private void GenerateFilledRect()
		{
			var rect = Rect;
			if (Mathf.Approximately(0, m_fadeSize))
			{
				AddQuad(rect);
				return;
			}

			GenerateFrameRectSimple(rect, m_fadeSize);
			FadeFrameRect(rect, Fade.Outer);
			rect.x += m_fadeSize;
			rect.y += m_fadeSize;
			rect.width -= m_fadeSize * 2;
			rect.height -= m_fadeSize * 2;
			AddQuad(rect);
		}

		private void FadeFrameRect( Rect _rect, Fade _fade )
		{
			if (_fade == Fade.None)
				return;

			float top = _rect.yMin;
			float bottom = _rect.yMax;
			float left = _rect.xMin;
			float right = _rect.xMax;

			// frame is 8 quads, 16 tris, 32 verts
			for (int i = s_vertices.Count - 32; i < s_vertices.Count; i++)
			{
				var vertex = s_vertices[i];
				var position = vertex.Position;

				bool condition =
					Mathf.Approximately(left, position.x) ||
					Mathf.Approximately(right, position.x) ||
					Mathf.Approximately(top, position.y) ||
					Mathf.Approximately(bottom, position.y);

				if (_fade == Fade.Inner)
					condition = !condition;

				if (condition)
				{
					vertex.Color = m_fadeColor;
				}
			}
		}

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

			AddTriangle
			(
				x, y + radius,
				cex, cey,
				x, y + h - radius
			);
			AddTriangle
			(
				x + radius, y + h,
				cex, cey,
				x + w - radius, y + h
			);
			AddTriangle
			(
				x + w, y + radius,
				cex, cey,
				x + w, y + h - radius
			);
			AddTriangle
			(
				x + radius, y,
				cex, cey,
				x + w - radius, y
			);

			AddSector(rect, Corner.TopLeft, radius);
			AddSector(rect, Corner.TopRight, radius);
			AddSector(rect, Corner.BottomLeft, radius);
			AddSector(rect, Corner.BottomRight, radius);
		}

		private void AddFrameSegment( Rect _rect, Corner _corner, float _frameSize, float _radius, Fade _fade )
		{
			var x = _rect.x;
			var y = _rect.y;
			var w = _rect.width;
			var h = _rect.height;

			float angle = ((int)_corner + 3) * 90 * Mathf.Deg2Rad;
			float angleIncrement = 90f / m_cornerSegments * Mathf.Deg2Rad;

			float ox, oy;
			switch (_corner)
			{
				case Corner.TopLeft:
					ox = x + _radius;
					oy = y + h - _radius;
					break;
				case Corner.TopRight:
					ox = x + w - _radius;
					oy = y + h - _radius;
					break;
				case Corner.BottomRight:
					ox = x + w - _radius;
					oy = y + _radius;
					break;
				case Corner.BottomLeft:
					ox = x + _radius;
					oy = y + _radius;
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(_corner), _corner, null);
			}

			float radiusInner = _radius - _frameSize;
			for (int i = 0; i < m_cornerSegments; i++)
			{
				float x1 = Mathf.Sin(angle) * _radius + ox;
				float y1 = Mathf.Cos(angle) * _radius + oy;
				float x3 = Mathf.Sin(angle) * radiusInner + ox;
				float y3 = Mathf.Cos(angle) * radiusInner + oy;
				angle += angleIncrement;
				float x0 = Mathf.Sin(angle) * _radius + ox;
				float y0 = Mathf.Cos(angle) * _radius + oy;
				float x2 = Mathf.Sin(angle) * radiusInner + ox;
				float y2 = Mathf.Cos(angle) * radiusInner + oy;
				AddIrregularQuad(x0, y0, x1, y1, x2, y2, x3, y3, _fade);
			}

		}

		private void AddSector( Rect _rect, Corner _corner, float _radius )
		{
			var x = _rect.x;
			var y = _rect.y;
			var w = _rect.width;
			var h = _rect.height;
			var cex = _rect.center.x;
			var cey = _rect.center.y;

			float angle = ((int)_corner + 3) * 90 * Mathf.Deg2Rad;
			float angleIncrement = 90f / m_cornerSegments * Mathf.Deg2Rad;

			float ox, oy;
			switch (_corner)
			{
				case Corner.TopLeft:
					ox = x + _radius;
					oy = y + h - _radius;
					break;
				case Corner.TopRight:
					ox = x + w - _radius;
					oy = y + h - _radius;
					break;
				case Corner.BottomRight:
					ox = x + w - _radius;
					oy = y + _radius;
					break;
				case Corner.BottomLeft:
					ox = x + _radius;
					oy = y + _radius;
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(_corner), _corner, null);
			}

			for (int i = 0; i < m_cornerSegments; i++)
			{
				float x1 = Mathf.Sin(angle) * _radius + ox;
				float y1 = Mathf.Cos(angle) * _radius + oy;
				angle += angleIncrement;
				float x0 = Mathf.Sin(angle) * _radius + ox;
				float y0 = Mathf.Cos(angle) * _radius + oy;
				AddTriangle(x0, y0, cex, cey, x1, y1);
			}
		}

		private void AddQuad( Rect _rect, QuadFade _fade = QuadFade.None, bool _left = false ) => AddQuad(_rect.min, _rect.max, _fade, _left);

		private void AddQuad( Vector2 _posMin, Vector2 _posMax, QuadFade _fade = QuadFade.None, bool _left = false )
		{
			int startIndex = s_vertices.Count;

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
