using System;
using UnityEngine;
using UnityEngine.UI;
using static UnityEditor.PlayerSettings;

namespace GuiToolkit
{
	[RequireComponent(typeof(CanvasRenderer))]
	public class UiRoundedImage : Image
	{
		public enum Side
		{
			Top,
			Right,
			Bottom,
			Left,
		}

		[SerializeField] protected int m_cornerSegments = 5;
		[SerializeField] protected float m_radius = 10;
		[SerializeField] protected float m_frameSize = 0;
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

			AddQuad(_vh, bl, color);
			AddQuad(_vh, br, color);
			AddQuad(_vh, tl, color);
			AddQuad(_vh, tr, color);

			Rect l = new Rect(x, y + m_frameSize, m_frameSize, h - m_frameSize * 2);
			Rect r = new Rect(w + x - m_frameSize, y + m_frameSize, m_frameSize, h - m_frameSize * 2);
			Rect t = new Rect(x + m_frameSize, h + y - m_frameSize, w - m_frameSize * 2, m_frameSize);
			Rect b = new Rect(x + m_frameSize, y, w - m_frameSize * 2, m_frameSize);

			AddQuad(_vh, l, color);
			AddQuad(_vh, r, color);
			AddQuad(_vh, t, color);
			AddQuad(_vh, b, color);
		}

		private void GenerateFilled( VertexHelper _vh )
		{
			if (Mathf.Approximately(0, m_radius))
			{
				GenerateQuad(_vh);
				return;
			}
			
		}

		private void GenerateQuad( VertexHelper _vh ) => AddQuad(_vh, GetPixelAdjustedRect(), color);

		private void AddQuad( VertexHelper _vh, Rect _rect, Color32 _color ) => AddQuad(_vh, _rect.min, _rect.max, color);


		private void AddQuad( VertexHelper _vh, Vector2 _posMin, Vector2 _posMax, Color32 _color )
		{
			int startIndex = _vh.currentVertCount;

			AddVert(_vh, _posMin.x, _posMin.y, _color);
			AddVert(_vh, _posMin.x, _posMax.y, _color);
			AddVert(_vh, _posMax.x, _posMax.y, _color);
			AddVert(_vh, _posMax.x, _posMin.y, _color);

			_vh.AddTriangle(startIndex, startIndex + 1, startIndex + 2);
			_vh.AddTriangle(startIndex + 2, startIndex + 3, startIndex);
		}

		private void AddVert( VertexHelper _vh, float _x, float _y, Color32 _color )
		{
			_vh.AddVert(new Vector3(_x, _y, 0), _color, GetUv(_x, _y));
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