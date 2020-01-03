using System;
using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit
{
	[ExecuteAlways]
	public class UiGradient : BaseMeshEffect
	{
		public Color m_colorRightOrTop;
		public Color m_colorLeftOrBottom;
		public bool m_isHorizontal;
		public bool m_byUV = true;

		private static UIVertex s_vertex;

		private float m_min;
		private float m_max;

		public override void ModifyMesh( VertexHelper _vh )
		{
			if (!m_byUV)
				CalcMinMax( _vh );

			float dist = m_max - m_min;

			for (int i = 0; i < _vh.currentVertCount; ++i)
			{
				_vh.PopulateUIVertex(ref s_vertex, i);

				float lerpVal;
				if (m_byUV)
				{
					Vector2 uv = s_vertex.uv0;
					lerpVal	= m_isHorizontal ? uv.x : uv.y;
				}
				else
				{
					lerpVal = m_isHorizontal ?
						(s_vertex.position.x-m_min) / dist:
						(s_vertex.position.y-m_min) / dist;
				}
				Color color = Color.Lerp(m_colorLeftOrBottom, m_colorRightOrTop, lerpVal);
				s_vertex.color = color;

				_vh.SetUIVertex(s_vertex, i);
			}
		}

		private void CalcMinMax( VertexHelper _vh )
		{
			m_min = float.MaxValue;
			m_max = -float.MaxValue;

			for (int i = 0; i < _vh.currentVertCount; ++i)
			{
				_vh.PopulateUIVertex(ref s_vertex, i);

				if (m_isHorizontal)
				{
					m_min = Mathf.Min(m_min, s_vertex.position.x);
					m_max = Mathf.Max(m_max, s_vertex.position.x);
				}
				else
				{
					m_min = Mathf.Min(m_min, s_vertex.position.y);
					m_max = Mathf.Max(m_max, s_vertex.position.y);
				}

				_vh.SetUIVertex(s_vertex, i);
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