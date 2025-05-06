using System;
using UnityEngine;
using UnityEngine.UI;
using static UnityEditor.PlayerSettings;

namespace GuiToolkit
{
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
		
		[Range(2, 50)]
		[SerializeField] protected int m_cornerSegments = 5;
		[Range(0,200)]
		[SerializeField] protected float m_radius = 10;
		[Range(0,200)]
		[SerializeField] protected float m_frameSize = 0;
		[Range(0,10)]
		[SerializeField] protected float m_blurWidth = 0;
		
		private static VertexHelper s_vertexHelper;

		protected override void OnPopulateMesh( VertexHelper _vh )
		{
			s_vertexHelper = _vh;
			s_vertexHelper.Clear();
			

			if (m_frameSize > 0)
			{
				GenerateFrame();
				return;
			}

			GenerateFilled();
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

		private void GenerateFrameRounded()
		{
			Rect rect = rectTransform.rect;
			var x = rect.x;
			var y = rect.y;
			var w = rect.width;
			var h = rect.height;
			
			Rect l = new Rect(x, y + m_radius, m_frameSize, h - m_radius * 2);
			Rect r = new Rect(w + x - m_frameSize, y + m_radius, m_frameSize, h - m_radius * 2);
			Rect t = new Rect(x + m_radius, h + y - m_frameSize, w - m_radius * 2, m_frameSize);
			Rect b = new Rect(x + m_radius, y, w - m_radius * 2, m_frameSize);

			AddQuad(l);
			AddQuad(r);
			AddQuad(t);
			AddQuad(b);

			AddFrameSegment(Corner.TopLeft);
			AddFrameSegment(Corner.TopRight);
			AddFrameSegment(Corner.BottomLeft);
			AddFrameSegment(Corner.BottomRight);
		}

		private void AddFrameSegment(Corner _corner) => AddFrameSegment(_corner, m_cornerSegments);
		
		private void AddFrameSegment(Corner _corner, int _cornerSegments)
		{
			Rect rect = rectTransform.rect;
			var x = rect.x;
			var y = rect.y;
			var w = rect.width;
			var h = rect.height;
			
			float angle = ((int) _corner + 3) * 90 * Mathf.Deg2Rad;
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
			
			float radiusInner = m_radius - m_frameSize;
			for (int i=0; i<_cornerSegments; i++)
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
				AddIrregularQuad(x0, y0, x1, y1, x2, y2, x3, y3);
			}
		}
		
		
		private void GenerateFrameRect()
		{
			Rect rect = rectTransform.rect;
			var x = rect.x;
			var y = rect.y;
			var w = rect.width;
			var h = rect.height;

			Rect bl = new Rect(x, y, m_frameSize, m_frameSize);
			Rect br = new Rect(w + x - m_frameSize, y, m_frameSize, m_frameSize);
			Rect tl = new Rect(x, h + y - m_frameSize, m_frameSize, m_frameSize);
			Rect tr = new Rect(w + x - m_frameSize, h + y - m_frameSize, m_frameSize, m_frameSize);

			AddQuad(bl);
			AddQuad(br);
			AddQuad(tl);
			AddQuad(tr);

			Rect l = new Rect(x, y + m_frameSize, m_frameSize, h - m_frameSize * 2);
			Rect r = new Rect(w + x - m_frameSize, y + m_frameSize, m_frameSize, h - m_frameSize * 2);
			Rect t = new Rect(x + m_frameSize, h + y - m_frameSize, w - m_frameSize * 2, m_frameSize);
			Rect b = new Rect(x + m_frameSize, y, w - m_frameSize * 2, m_frameSize);

			AddQuad(l);
			AddQuad(r);
			AddQuad(t);
			AddQuad(b);
		}

		private void GenerateFilled()
		{
			if (Mathf.Approximately(0, m_radius))
			{
				GenerateQuad();
				return;
			}
			
			GenerateFilledRounded();
		}

		private void GenerateFilledRounded()
		{
			Rect rect = rectTransform.rect;
			var x = rect.x;
			var y = rect.y;
			var w = rect.width;
			var h = rect.height;
			var cex = rect.center.x;
			var cey = rect.center.y;

			AddTriangle
			(
				x, y + m_radius, 
				cex, cey, 
				x, y + h - m_radius
			);
			AddTriangle
			(
				x + m_radius, y + h, 
				cex, cey, 
				x + w - m_radius, y + h
			);
			AddTriangle
			(
				x + w, y + m_radius, 
				cex, cey, 
				x + w, y + h - m_radius
			);
			AddTriangle
			(
				x + m_radius, y, 
				cex, cey, 
				x + w - m_radius, y
			);
			
			AddSector(cex, cey, Corner.TopLeft);
			AddSector(cex, cey, Corner.TopRight);
			AddSector(cex, cey, Corner.BottomLeft);
			AddSector(cex, cey, Corner.BottomRight);
		}

		private void GenerateQuad() => AddQuad(GetPixelAdjustedRect());

		private void AddSector(float _cex, float _cey, Corner _corner) => AddSector(_cex, _cey, _corner, m_cornerSegments);
		
		private void AddSector(float _cex, float _cey, Corner _corner, int _cornerSegments)
		{
			Rect rect = rectTransform.rect;
			var x = rect.x;
			var y = rect.y;
			var w = rect.width;
			var h = rect.height;
			
			float angle = ((int) _corner + 3) * 90 * Mathf.Deg2Rad;
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
			
			for (int i=0; i<_cornerSegments; i++)
			{
				float x1 = Mathf.Sin(angle) * m_radius + ox;
				float y1 = Mathf.Cos(angle) * m_radius + oy;
				angle += angleIncrement;
				float x0 = Mathf.Sin(angle) * m_radius + ox;
				float y0 = Mathf.Cos(angle) * m_radius + oy;
				AddTriangle(x0, y0, _cex, _cey, x1, y1);
			}
		}
		
		private void AddTriangle(Vector2 _a, Vector2 _b, Vector2 _c)
		{
			int startIndex = s_vertexHelper.currentVertCount;

			AddVert(_a, color);
			AddVert(_b, color);
			AddVert(_c, color);

			s_vertexHelper.AddTriangle(startIndex, startIndex + 1, startIndex + 2);
		}
		
		private void AddTriangle(float _ax, float _ay, float _bx, float _by, float _cx, float _cy)
		{
			int startIndex = s_vertexHelper.currentVertCount;

			AddVert(_ax, _ay, color);
			AddVert(_bx, _by, color);
			AddVert(_cx, _cy, color);

			s_vertexHelper.AddTriangle(startIndex, startIndex + 1, startIndex + 2);
		}
		
		private void AddQuad(Rect _rect ) => AddQuad(_rect.min, _rect.max);

		private void AddQuad(Vector2 _posMin, Vector2 _posMax )
		{
			int startIndex = s_vertexHelper.currentVertCount;

			AddVert(_posMin.x, _posMin.y, color);
			AddVert(_posMin.x, _posMax.y, color);
			AddVert(_posMax.x, _posMax.y, color);
			AddVert(_posMax.x, _posMin.y, color);

			s_vertexHelper.AddTriangle(startIndex, startIndex + 1, startIndex + 2);
			s_vertexHelper.AddTriangle(startIndex + 2, startIndex + 3, startIndex);
		}

		private void AddIrregularQuad(float _ax, float _ay, float _bx, float _by, float _cx, float _cy, float _dx, float _dy)
		{
			int startIndex = s_vertexHelper.currentVertCount;

			AddVert(_ax, _ay, color);
			AddVert(_bx, _by, color);
			AddVert(_cx, _cy, color);
			AddVert(_dx, _dy, color);

			s_vertexHelper.AddTriangle(startIndex + 3, startIndex + 1, startIndex);
			s_vertexHelper.AddTriangle(startIndex + 2, startIndex + 3, startIndex);
		}

		private void AddVert(float _x, float _y, Color32 _color )
		{
			s_vertexHelper.AddVert(new Vector3(_x, _y, 0), _color, GetUv(_x, _y));
		}

		private void AddVert(Vector2 _p, Color32 _color ) => AddVert(_p.x, _p.y, _color);

		private Vector2 GetUv( float _x, float _y )
		{
			Rect r = rectTransform.rect;

			var normalizedX = _x / r.width + .5f;
			var normalizedY = _y / r.height + .5f;

			return new Vector2(normalizedX, normalizedY);
		}

	}
}