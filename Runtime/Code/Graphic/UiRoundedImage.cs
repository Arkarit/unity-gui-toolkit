using System;
using UnityEngine;
using UnityEngine.UI;
using static UnityEditor.PlayerSettings;

namespace GuiToolkit
{
	[RequireComponent(typeof(CanvasRenderer))]
	public class UiRoundedImage : Image
	{
		[Range(2, 50)]
		[SerializeField] protected int m_cornerSegments = 5;
		[Range(0,200)]
		[SerializeField] protected float m_radius = 10;
		[Range(0,200)]
		[SerializeField] protected float m_frameSize = 0;
		[Range(0,10)]
		[SerializeField] protected float m_blurWidth = 0;

		protected override void OnPopulateMesh( VertexHelper _vh )
		{
			_vh.Clear();

			if (m_frameSize > 0)
			{
				GenerateFrame(_vh);
				return;
			}

			GenerateFilled(_vh);
		}

		private void GenerateFrame( VertexHelper _vh )
		{
			if (Mathf.Approximately(0, m_radius))
			{
				GenerateRectFrame(_vh);
				return;
			}
		}

		private void GenerateRectFrame( VertexHelper _vh )
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

			AddQuad(_vh, bl);
			AddQuad(_vh, br);
			AddQuad(_vh, tl);
			AddQuad(_vh, tr);

			Rect l = new Rect(x, y + m_frameSize, m_frameSize, h - m_frameSize * 2);
			Rect r = new Rect(w + x - m_frameSize, y + m_frameSize, m_frameSize, h - m_frameSize * 2);
			Rect t = new Rect(x + m_frameSize, h + y - m_frameSize, w - m_frameSize * 2, m_frameSize);
			Rect b = new Rect(x + m_frameSize, y, w - m_frameSize * 2, m_frameSize);

			AddQuad(_vh, l);
			AddQuad(_vh, r);
			AddQuad(_vh, t);
			AddQuad(_vh, b);
		}

		private void GenerateFilled( VertexHelper _vh )
		{
			if (Mathf.Approximately(0, m_radius))
			{
				GenerateQuad(_vh);
				return;
			}
			
			GenerateFilledRounded(_vh);
		}

		private void GenerateFilledRounded(VertexHelper _vh)
		{
			Rect rect = rectTransform.rect;
			var x = rect.x;
			var y = rect.y;
			var w = rect.width;
			var h = rect.height;
			var cex = rect.center.x;
			var cey = rect.center.y;
			
			Vector2 a, b, c;
			
			AddTriangle
			(
				_vh, 
				x, y + m_radius, 
				cex, cey, 
				x, y + h - m_radius
			);
		}

		private void GenerateQuad( VertexHelper _vh ) => AddQuad(_vh, GetPixelAdjustedRect());


		private void AddTriangle(VertexHelper _vh, Vector2 _a, Vector2 _b, Vector2 _c)
		{
			int startIndex = _vh.currentVertCount;

			AddVert(_vh, _a, color);
			AddVert(_vh, _b, color);
			AddVert(_vh, _c, color);

			_vh.AddTriangle(startIndex, startIndex + 1, startIndex + 2);
		}
		
		private void AddTriangle(VertexHelper _vh, float _ax, float _ay, float _bx, float _by, float _cx, float _cy)
		{
			int startIndex = _vh.currentVertCount;

			AddVert(_vh, _ax, _ay, color);
			AddVert(_vh, _bx, _by, color);
			AddVert(_vh, _cx, _cy, color);

			_vh.AddTriangle(startIndex, startIndex + 1, startIndex + 2);
		}
		
		private void AddQuad( VertexHelper _vh, Rect _rect ) => AddQuad(_vh, _rect.min, _rect.max);

		private void AddQuad( VertexHelper _vh, Vector2 _posMin, Vector2 _posMax )
		{
			int startIndex = _vh.currentVertCount;

			AddVert(_vh, _posMin.x, _posMin.y, color);
			AddVert(_vh, _posMin.x, _posMax.y, color);
			AddVert(_vh, _posMax.x, _posMax.y, color);
			AddVert(_vh, _posMax.x, _posMin.y, color);

			_vh.AddTriangle(startIndex, startIndex + 1, startIndex + 2);
			_vh.AddTriangle(startIndex + 2, startIndex + 3, startIndex);
		}

		private void AddVert( VertexHelper _vh, float _x, float _y, Color32 _color )
		{
			_vh.AddVert(new Vector3(_x, _y, 0), _color, GetUv(_x, _y));
		}

		private void AddVert( VertexHelper _vh, Vector2 _p, Color32 _color ) => AddVert(_vh, _p.x, _p.y, _color);

		private Vector2 GetUv( float _x, float _y )
		{
			Rect r = rectTransform.rect;

			var normalizedX = _x / r.width + .5f;
			var normalizedY = _y / r.height + .5f;

			return new Vector2(normalizedX, normalizedY);
		}

	}
}