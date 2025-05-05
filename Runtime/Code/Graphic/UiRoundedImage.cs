using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit
{
	[RequireComponent(typeof(CanvasRenderer))]
	public class UiRoundedImage : Image
	{
		public enum SliceType
		{
			Simple,
			Sliced,
		}
		
		[SerializeField] protected int m_cornerSegments = 5;
		[SerializeField] protected float m_radius = 10;
		[SerializeField] protected float m_frameWidth = 0;
		[SerializeField] protected float m_blurWidth = 0;
		[SerializeField] protected SliceType m_sliceType;

		protected override void OnPopulateMesh(VertexHelper _vh)
		{
			if (m_frameWidth > 0)
			{
				GenerateFrame(_vh);
				return;
			}
			
			GenerateFilled(_vh);
		}

		private void GenerateFrame(VertexHelper _vh)
		{
			if (Mathf.Approximately(0, m_radius))
			{
				GenerateRectFrame(_vh);
				return;
			}
		}

		private void GenerateRectFrame(VertexHelper _vh)
		{
			
		}

		private void GenerateFilled(VertexHelper _vh)
		{
			if (Mathf.Approximately(0, m_radius))
			{
				GenerateQuad(_vh);
				return;
			}
		}

		private void GenerateQuad(VertexHelper _vh)
		{
//			_vh.AddUIVertexQuad();
			
		}
		
		private void AddQuad(VertexHelper _vh, Vector2 _posMin, Vector2 _posMax, Color32 _color, Vector2 _uvMin, Vector2 _uvMax)
        {
            int startIndex = _vh.currentVertCount;

            _vh.AddVert(new Vector3(_posMin.x, _posMin.y, 0), _color, new Vector2(_uvMin.x, _uvMin.y));
            _vh.AddVert(new Vector3(_posMin.x, _posMax.y, 0), _color, new Vector2(_uvMin.x, _uvMax.y));
            _vh.AddVert(new Vector3(_posMax.x, _posMax.y, 0), _color, new Vector2(_uvMax.x, _uvMax.y));
            _vh.AddVert(new Vector3(_posMax.x, _posMin.y, 0), _color, new Vector2(_uvMax.x, _uvMin.y));

            _vh.AddTriangle(startIndex, startIndex + 1, startIndex + 2);
            _vh.AddTriangle(startIndex + 2, startIndex + 3, startIndex);
        }
		
		private Vector2 GetUv(float _x, float _y) => new Vector2();

		

	}
}