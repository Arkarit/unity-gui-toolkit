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

		protected static UIVertex s_vertex;

		protected Vector2 m_min;
		protected Vector2 m_max;

		protected abstract Color GetColor( Vector2 _normVal );
		protected virtual void Prepare( VertexHelper _vh ) {}
		protected virtual bool NeedsMeshBounds => true;

		public override void ModifyMesh( VertexHelper _vh )
		{
			if (!IsActive())
				return;

			if (NeedsMeshBounds)
				CalcMeshBounds( _vh );

			Prepare( _vh );

			Vector2 dist = m_max - m_min;

			for (int i = 0; i < _vh.currentVertCount; ++i)
			{
				_vh.PopulateUIVertex(ref s_vertex, i);

				Vector2 pos = new Vector2( s_vertex.position.x, s_vertex.position.y );
				Vector2 lerpVal = (pos-m_min) / dist;
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

		// Well mesh bounds could be simply done by getting the mesh bounds. 
		// If there only would be a mesh in the Vertex"Helper".
		// So we have to iterate it manually. And let "PopulateUIVertex" fill each UiVertex with uv0, uv1, tangents and whatnot shit we don't need. Extremely inefficient.
		// Well done as usual, Unity.
		private void CalcMeshBounds( VertexHelper _vh )
		{
			m_min = new Vector2( float.MaxValue, float.MaxValue );
			m_max = new Vector2( float.MinValue, float.MinValue );

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
