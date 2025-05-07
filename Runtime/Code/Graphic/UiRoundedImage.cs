using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit
{
	[ExecuteAlways]
	[RequireComponent(typeof(CanvasRenderer))]
	public class UiRoundedImage : Image
	{
		public enum Corner
		{
			TopLeft,
			TopRight,
			BottomRight,
			BottomLeft,
		}

		private enum Fade
		{
			None,
			Inner,
			Outer,
		}
		
		private enum QuadFade
		{
			None,
			Left,
			Right,
			Top,
			Bottom,
		}

		private class Vertex
		{
			public Vector2 Position;
			public Vector2 Uv;
			public Color Color;
		}
		
		[UnityEngine.Range(2, 50)]
		[SerializeField] protected int m_cornerSegments = 5;
		[UnityEngine.Range(0, 200)]
		[SerializeField] protected float m_radius = 10;
		[UnityEngine.Range(0, 200)]
		[SerializeField] protected float m_frameSize = 0;
		[UnityEngine.Range(0, 30)]
		[SerializeField] protected float m_fadeSize = 0;

		private static readonly List<Vertex> s_vertices = new();
		private static readonly List<int[]> s_triangles = new();

		protected override void OnPopulateMesh( Mesh _mesh )
		{
			if (m_frameSize > 0)
			{
				GenerateFrame();
				ApplyToMesh(_mesh);
				return;
			}

			GenerateFilled();
			ApplyToMesh(_mesh);
		}

		protected override void OnPopulateMesh( VertexHelper _vh )
		{
			if (m_frameSize > 0)
			{
				GenerateFrame();
				ApplyToVertexHelper(_vh);
				return;
			}

			GenerateFilled();
			ApplyToVertexHelper(_vh);
		}

		protected override void UpdateGeometry()
		{
			workerMesh.Clear(false);
			if (rectTransform == null || rectTransform.rect.width < 0 || rectTransform.rect.height < 0)
				return;

			var components = GetComponents<IMeshModifier>();
			bool hasComponents = components.Length > 0;

			if (hasComponents)
			{
				using (VertexHelper vertexHelper = new VertexHelper())
				{
					OnPopulateMesh(vertexHelper);
					foreach (var component in components)
						component.ModifyMesh(vertexHelper);
					vertexHelper.FillMesh(workerMesh);
				}
				
				canvasRenderer.SetMesh(workerMesh);
				return;
			}

			OnPopulateMesh(workerMesh);
			canvasRenderer.SetMesh(workerMesh);
		}

		private void ApplyToMesh( Mesh _mesh )
		{
			var vertexCount = s_vertices.Count;

			var vertices = new Vector3[vertexCount];
			var colors = new Color[vertexCount];
			var uv = new Vector2[vertexCount];

			for (int i = 0; i < vertexCount; i++)
			{
				var vertex = s_vertices[i];
				vertices[i] = vertex.Position;
				colors[i] = vertex.Color;
				uv[i] = vertex.Uv;
			}

			var triangleCount = s_triangles.Count;
			var triangles = new int[triangleCount * 3];
			int it = 0;
			for (int i = 0; i < triangleCount; i++)
			{
				var triangle = s_triangles[i];
				triangles[it++] = triangle[0];
				triangles[it++] = triangle[1];
				triangles[it++] = triangle[2];
			}

			_mesh.vertices = vertices;
			_mesh.colors = colors;
			_mesh.uv = uv;
			_mesh.triangles = triangles;

			s_vertices.Clear();
			s_triangles.Clear();
		}

		private static void ApplyToVertexHelper( VertexHelper _vh )
		{
			_vh.Clear();

			foreach (var vertex in s_vertices)
				_vh.AddVert(vertex.Position, vertex.Color, vertex.Uv);

			for (int i = 0; i < s_triangles.Count; i++)
			{
				var tri = s_triangles[i];
				_vh.AddTriangle(tri[0], tri[1], tri[2]);
			}

			s_vertices.Clear();
			s_triangles.Clear();
		}

		private void GenerateFrame()
		{
			if (Mathf.Approximately(0, m_radius))
			{
				GenerateFrameRect();
				return;
			}

			GenerateFrameRounded();
		}

		private void GenerateFilled()
		{
			if (Mathf.Approximately(0, m_radius))
			{
				GenerateFilledRect();
				return;
			}

			GenerateFilledRounded();
		}

		private void GenerateFrameRect() => GenerateFrameRect(rectTransform.rect, m_frameSize);

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

		private void GenerateFrameRounded() => GenerateFrameRounded(rectTransform.rect, m_frameSize, Fade.None);
		
		private void GenerateFrameRounded(ref Rect _rect, float _frameSize, Fade _fade)
		{
			GenerateFrameRounded(_rect, _frameSize, _fade);
			_rect.x += _frameSize;
			_rect.y += _frameSize;
			_rect.width -= _frameSize * 2;
			_rect.height -= _frameSize * 2;
		}
		
		private void GenerateFrameRounded(Rect _rect, float _frameSize, Fade _fade)
		{
			var x = _rect.x;
			var y = _rect.y;
			var w = _rect.width;
			var h = _rect.height;

			Rect l = new Rect(x, y + m_radius, _frameSize, h - m_radius * 2);
			Rect r = new Rect(w + x - _frameSize, y + m_radius, _frameSize, h - m_radius * 2);
			Rect t = new Rect(x + m_radius, h + y - _frameSize, w - m_radius * 2, _frameSize);
			Rect b = new Rect(x + m_radius, y, w - m_radius * 2, _frameSize);

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
			

			AddFrameSegment(Corner.TopLeft, _frameSize, _fade);
			AddFrameSegment(Corner.TopRight, _frameSize, _fade);
			AddFrameSegment(Corner.BottomLeft, _frameSize, _fade);
			AddFrameSegment(Corner.BottomRight, _frameSize, _fade);
		}

		private void GenerateFilledRect()
		{
			var rect = rectTransform.rect;
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
			
			var fadeColor = color;
			fadeColor.a = 0;
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
					vertex.Color = fadeColor;
				}
			}
		}

		private void GenerateFilledRounded()
		{
			Rect rect = rectTransform.rect;
			float radius = m_radius;
			if (!Mathf.Approximately(0, m_fadeSize))
			{
				GenerateFrameRounded(ref rect, m_fadeSize, Fade.Outer);
				radius -= m_fadeSize;
			}
			
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

		private void AddFrameSegment( Corner _corner ) => AddFrameSegment(_corner, m_cornerSegments, m_frameSize, Fade.None);
		private void AddFrameSegment( Corner _corner, float _frameSize, Fade _fade ) => AddFrameSegment(_corner, m_cornerSegments, _frameSize, _fade);

		private void AddFrameSegment( Corner _corner, int _cornerSegments, float _frameSize, Fade _fade )
		{
			Rect rect = rectTransform.rect;
			var x = rect.x;
			var y = rect.y;
			var w = rect.width;
			var h = rect.height;

			float angle = ((int)_corner + 3) * 90 * Mathf.Deg2Rad;
			float angleIncrement = 90f / _cornerSegments * Mathf.Deg2Rad;

			float ox, oy;
			switch (_corner)
			{
				case Corner.TopLeft:
					ox = x + m_radius;
					oy = y + h - m_radius;
					break;
				case Corner.TopRight:
					ox = x + w - m_radius;
					oy = y + h - m_radius;
					break;
				case Corner.BottomRight:
					ox = x + w - m_radius;
					oy = y + m_radius;
					break;
				case Corner.BottomLeft:
					ox = x + m_radius;
					oy = y + m_radius;
					break;
				default:
					throw new ArgumentOutOfRangeException(nameof(_corner), _corner, null);
			}

			float radiusInner = m_radius - _frameSize;
			for (int i = 0; i < _cornerSegments; i++)
			{
				float x1 = Mathf.Sin(angle) * m_radius + ox;
				float y1 = Mathf.Cos(angle) * m_radius + oy;
				float x3 = Mathf.Sin(angle) * radiusInner + ox;
				float y3 = Mathf.Cos(angle) * radiusInner + oy;
				angle += angleIncrement;
				float x0 = Mathf.Sin(angle) * m_radius + ox;
				float y0 = Mathf.Cos(angle) * m_radius + oy;
				float x2 = Mathf.Sin(angle) * radiusInner + ox;
				float y2 = Mathf.Cos(angle) * radiusInner + oy;
				AddIrregularQuad(x0, y0, x1, y1, x2, y2, x3, y3, _fade);
			}

		}

		private void AddSector( Rect _rect, Corner _corner, float _radius ) => AddSector(_rect, _corner, m_cornerSegments, _radius);
		
		private void AddSector( Rect _rect, Corner _corner ) => AddSector(_rect, _corner, m_cornerSegments, m_radius);

		private void AddSector( Rect _rect, Corner _corner, int _cornerSegments, float _radius )
		{
			var x = _rect.x;
			var y = _rect.y;
			var w = _rect.width;
			var h = _rect.height;
			var cex = _rect.center.x;
			var cey = _rect.center.y;

			float angle = ((int)_corner + 3) * 90 * Mathf.Deg2Rad;
			float angleIncrement = 90f / _cornerSegments * Mathf.Deg2Rad;

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

			for (int i = 0; i < _cornerSegments; i++)
			{
				float x1 = Mathf.Sin(angle) * _radius + ox;
				float y1 = Mathf.Cos(angle) * _radius + oy;
				angle += angleIncrement;
				float x0 = Mathf.Sin(angle) * _radius + ox;
				float y0 = Mathf.Cos(angle) * _radius + oy;
				AddTriangle(x0, y0, cex, cey, x1, y1);
			}
		}

		private void AddTriangle( float _ax, float _ay, float _bx, float _by, float _cx, float _cy )
		{
			int startIndex = s_vertices.Count;

			AddVert(_ax, _ay, color);
			AddVert(_bx, _by, color);
			AddVert(_cx, _cy, color);

			s_triangles.Add(new[] { startIndex, startIndex + 1, startIndex + 2 });
		}

		private void AddQuad( Rect _rect, QuadFade _fade = QuadFade.None, bool _left = false ) => AddQuad(_rect.min, _rect.max, _fade, _left);

		private void AddQuad( Vector2 _posMin, Vector2 _posMax, QuadFade _fade = QuadFade.None, bool _left = false )
		{
			int startIndex = s_vertices.Count;
			var fadeColor = color;
			fadeColor.a = 0;

			switch (_fade)
			{
				case QuadFade.None:
					AddVert(_posMin.x, _posMin.y, color);
					AddVert(_posMin.x, _posMax.y, color);
					AddVert(_posMax.x, _posMax.y, color);
					AddVert(_posMax.x, _posMin.y, color);
					break;
				case QuadFade.Left:
					AddVert(_posMin.x, _posMin.y, fadeColor);
					AddVert(_posMin.x, _posMax.y, fadeColor);
					AddVert(_posMax.x, _posMax.y, color);
					AddVert(_posMax.x, _posMin.y, color);
					break;
				case QuadFade.Right:
					AddVert(_posMin.x, _posMin.y, color);
					AddVert(_posMin.x, _posMax.y, color);
					AddVert(_posMax.x, _posMax.y, fadeColor);
					AddVert(_posMax.x, _posMin.y, fadeColor);
					break;
				case QuadFade.Top:
					AddVert(_posMin.x, _posMin.y, color);
					AddVert(_posMin.x, _posMax.y, fadeColor);
					AddVert(_posMax.x, _posMax.y, fadeColor);
					AddVert(_posMax.x, _posMin.y, color);
					break;
				case QuadFade.Bottom:
					AddVert(_posMin.x, _posMin.y, fadeColor);
					AddVert(_posMin.x, _posMax.y, color);
					AddVert(_posMax.x, _posMax.y, color);
					AddVert(_posMax.x, _posMin.y, fadeColor);
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
			var fadeColor = color;
			fadeColor.a = 0;
			
			var effectiveColor = _fade == Fade.Outer ? fadeColor : color;
			
			AddVert(_ax, _ay, effectiveColor);
			AddVert(_bx, _by, effectiveColor);
			
			effectiveColor = _fade == Fade.Inner ? fadeColor : color;
			
			AddVert(_cx, _cy, effectiveColor);
			AddVert(_dx, _dy, effectiveColor);

			s_triangles.Add(new[] { startIndex + 3, startIndex + 1, startIndex });
			s_triangles.Add(new[] { startIndex + 2, startIndex + 3, startIndex });
		}

		private void AddVert( float _x, float _y, Color32 _color )
		{
			s_vertices.Add(new Vertex() { Position = new Vector2(_x, _y), Uv = GetUv(_x, _y), Color = _color });
		}

		private Vector2 GetUv( float _x, float _y )
		{
			Rect r = rectTransform.rect;

			var normalizedX = _x / r.width + .5f;
			var normalizedY = _y / r.height + .5f;

			return new Vector2(normalizedX, normalizedY);
		}

	}
}