using System;
using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit
{
	[ExecuteAlways]
	public abstract class UiGradientBase : BaseMeshEffectTMP
	{
		public enum EVertexColorMode {
			Multiply,
			Replace,
			Add,
		}

		[SerializeField]
		protected EVertexColorMode m_vertexColorMode = EVertexColorMode.Multiply;

		// This is filled with the current vertex while calling GetColor()
		protected static UIVertex s_vertex;

		protected Vector2 m_min;
		protected Vector2 m_max;

		protected abstract Color GetColor( Vector2 _normVal );
		protected virtual void Prepare( VertexHelper _vh ) {}

		public override void ModifyMesh( VertexHelper _vh )
		{
			if (!IsActive())
				return;

			CalcMinMax( _vh );

			Prepare( _vh );

			Vector2 dist = m_max - m_min;

			for (int i = 0; i < _vh.currentVertCount; ++i)
			{
				_vh.PopulateUIVertex(ref s_vertex, i);

				Vector2 lerpVal;
				Vector2 pos = new Vector2( s_vertex.position.x, s_vertex.position.y );
				lerpVal = ( pos-m_min) / dist;

				Color c = GetColor(lerpVal);

				switch( m_vertexColorMode )
				{
					case EVertexColorMode.Replace:
					default:
						break;
					case EVertexColorMode.Multiply:
						c *= s_vertex.color;
						break;
					case EVertexColorMode.Add:
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
	}
}