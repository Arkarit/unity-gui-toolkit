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
				GenerateRectFrame();
				return;
			}
		}

		private void GenerateRectFrame()
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
#if true			
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
#endif			
			
			AddSegment(cex, cey, Corner.TopLeft);
		}
		
		

		private void GenerateQuad() => AddQuad(GetPixelAdjustedRect());

		private void AddSegment(float _cex, float _cey, Corner _corner)
		{
			Rect rect = rectTransform.rect;
			var x = rect.x;
			var y = rect.y;
			var w = rect.width;
			var h = rect.height;
			
			float angle = ((int) _corner + 2) * 90 * Mathf.Deg2Rad;
			float angleIncrement = 90f / m_cornerSegments * Mathf.Deg2Rad;
			
			for (int i=0; i<m_cornerSegments; i++)
			{
				float x1 = Mathf.Sin(angle) * m_radius + x + m_radius;
				float y1 = Mathf.Cos(angle) * m_radius + y + m_radius;
				angle += angleIncrement;
				float x0 = Mathf.Sin(angle) * m_radius + x + m_radius;
				float y0 = Mathf.Cos(angle) * m_radius + y + m_radius;
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