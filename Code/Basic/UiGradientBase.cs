using System;
using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit
{
	[ExecuteAlways]
	public abstract class UiGradientBase : BaseMeshEffect
	{
		[Tooltip("Switch on for sliced bitmaps, off for standard bitmaps.")]
		[SerializeField]
		private bool m_sliced = false;

		protected static UIVertex s_vertex;

		protected Vector2 m_min;
		protected Vector2 m_max;

		protected virtual void Prepare( VertexHelper _vh ) {}

		protected abstract Color GetColor( Vector2 _normVal );

		public override void ModifyMesh( VertexHelper _vh )
		{
			if (m_sliced)
				CalcMinMax( _vh );

			Vector2 dist = m_max - m_min;
			_vh.PopulateUIVertex(ref s_vertex, 0);

			Prepare( _vh );


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

				s_vertex.color = GetColor(lerpVal);

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