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

			// A mesh with no extent along one axis (or none at all) would divide by zero below and
			// write NaN into every vertex color. Map that axis to 0 instead.
			if (dist.x == 0f)
				dist.x = 1f;
			if (dist.y == 0f)
				dist.y = 1f;

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

		// Mesh bounds could simply be the mesh bounds. If there only would be a mesh in the Vertex"Helper".
		// Well done as usual, Unity. UiMeshModifierUtility.GetBounds does the walking, and - unlike the
		// straight per-vertex loop that used to live here - it skips the surplus quads TextMeshPro parks
		// on (0,0,0). Counting those pulled the box to the origin and shifted the whole gradient with it.
		private void CalcMeshBounds( VertexHelper _vh )
		{
			Rect bounds = UiMeshModifierUtility.GetBounds(_vh);
			m_min = bounds.min;
			m_max = bounds.max;
		}
	}
}
