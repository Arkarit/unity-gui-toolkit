using System;
using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit
{
	[ExecuteAlways]
	public abstract class UiGradientBase : BaseMeshEffect
	{
		public enum Mode {
			Replace,
			Multiply,
			Add,
		}

		[Tooltip("Switch on for sliced bitmaps, off for standard bitmaps.")]
		[SerializeField]
		protected bool m_sliced = false;

		[SerializeField]
		protected Mode m_mode = Mode.Multiply;

		[SerializeField]
		protected bool m_useTesselation;

		[SerializeField]
		[Range(15,1000)]
		protected float m_tesselationSizeHorizontal = 50.0f;

		[SerializeField]
		[Range(15,1000)]
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

			if (m_useTesselation && m_tesselationSizeHorizontal >= 10.0f)
			{
				UiTesselationUtil.Tesselate(_vh, m_tesselationSizeHorizontal, m_tesselationSizeVertical);
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

				switch( m_mode )
				{
					case Mode.Replace:
					default:
						break;
					case Mode.Multiply:
						c *= s_vertex.color;
						break;
					case Mode.Add:
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