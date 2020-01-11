using System;
using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit
{
	[ExecuteAlways]
	public abstract class UiGradientBase : BaseMeshEffect
	{
		private const float LARGE_TESSELATION = 1000000;

		public enum VertexColorMode {
			Replace,
			Multiply,
			Add,
		}

		public enum TesselationMode
		{
			None,
			Horizontal,
			Vertical,
			Both,
		}

		[Tooltip("Switch on for sliced bitmaps, off for standard bitmaps.")]
		[SerializeField]
		protected bool m_sliced = false;

		[SerializeField]
		protected VertexColorMode m_vertexColorMode = VertexColorMode.Multiply;

		[SerializeField]
		protected TesselationMode m_tesselationMode = TesselationMode.None;

		[SerializeField]
		[Range(5,2000)]
		protected float m_tesselationSizeHorizontal = 50.0f;

		[SerializeField]
		[Range(5,2000)]
		protected float m_tesselationSizeVertical = 50.0f;

		// This is filled with the current vertex while calling GetColor()
		protected static UIVertex s_vertex;

		private Vector2 m_min;
		private Vector2 m_max;

		protected abstract Color GetColor( Vector2 _normVal );

		public override void ModifyMesh( VertexHelper _vh )
		{
			if (m_sliced)
				CalcMinMax( _vh );

			switch( m_tesselationMode )
			{
				default:
				case TesselationMode.None:
					break;
				case TesselationMode.Horizontal:
					UiTesselationUtil.Tesselate(_vh, m_tesselationSizeHorizontal, LARGE_TESSELATION);
					break;
				case TesselationMode.Vertical:
					UiTesselationUtil.Tesselate(_vh, LARGE_TESSELATION, m_tesselationSizeVertical);
					break;
				case TesselationMode.Both:
					UiTesselationUtil.Tesselate(_vh, m_tesselationSizeHorizontal, m_tesselationSizeVertical);
					break;
			}

			Vector2 dist = m_max - m_min;

			for (int i = 0; i < _vh.currentVertCount; ++i)
			{
				_vh.PopulateUIVertex(ref s_vertex, i);

				Vector2 lerpVal;
				if (m_sliced)
				{
					Vector2 pos = new Vector2( s_vertex.position.x, s_vertex.position.y );
					lerpVal = ( pos-m_min) / dist;
				}
				else
				{
					lerpVal	= s_vertex.uv0;
				}

				Color c = GetColor(lerpVal);

				switch( m_vertexColorMode )
				{
					case VertexColorMode.Replace:
					default:
						break;
					case VertexColorMode.Multiply:
						c *= s_vertex.color;
						break;
					case VertexColorMode.Add:
						c += s_vertex.color;
						break;
				}

				s_vertex.color = c;

				_vh.SetUIVertex(s_vertex, i);
			}
		}

		private void CalcMinMax( VertexHelper _vh )
		{
			m_min = new Vector2( float.MaxValue, float.MaxValue );
			m_max = new Vector2( -float.MaxValue, -float.MaxValue );

			for (int i = 0; i < _vh.currentVertCount; ++i)
			{
				_vh.PopulateUIVertex(ref s_vertex, i);

				m_min.x = Mathf.Min(m_min.x, s_vertex.position.x);
				m_min.y = Mathf.Min(m_min.y, s_vertex.position.y);
				m_max.x = Mathf.Max(m_max.x, s_vertex.position.x);
				m_max.y = Mathf.Max(m_max.y, s_vertex.position.y);
			}
		}

#if UNITY_EDITOR
		protected override void OnValidate()
		{
			this.SetDirty();
		}
#endif
	}
}