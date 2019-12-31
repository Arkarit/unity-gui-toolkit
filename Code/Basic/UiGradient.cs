using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit
{
	[ExecuteAlways]
	public class UiGradient : BaseMeshEffect
	{
		public Color m_colorLeftOrBottom;
		public Color m_colorRightOrTop;
		public bool m_isHorizontal;

		private static UIVertex s_vertex;

		public override void ModifyMesh( VertexHelper _vh )
		{
			for (int i = 0; i < _vh.currentVertCount; ++i)
			{
				_vh.PopulateUIVertex(ref s_vertex, i);

				Vector2 uv = s_vertex.uv0;
				float lerpVal = m_isHorizontal ? uv.x : uv.y;
				Color color = Color.Lerp(m_colorLeftOrBottom, m_colorRightOrTop, lerpVal);
				s_vertex.color = color;

				_vh.SetUIVertex(s_vertex, i);
			}
		}

	}
}