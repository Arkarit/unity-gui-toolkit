using UnityEngine;
using UnityEngine.UI;

public class UiUvModifier : BaseMeshEffect
{
	public Vector2 m_min = Vector2.zero;
	public Vector2 m_max = Vector2.one;

	public bool m_swapAxes;

	private static UIVertex s_vertex;

	public Rect UvRect
	{
		set
		{
			m_min = value.min;
			m_max = value.max;
		}
	}

	public override void ModifyMesh( VertexHelper _vh )
	{
		if (_vh.currentVertCount < 1)
			return;

		_vh.PopulateUIVertex(ref s_vertex, 0);

		Vector2 min = s_vertex.uv0;
		Vector2 max = s_vertex.uv0;

		for (int i = 1; i < _vh.currentVertCount; ++i)
		{
			_vh.PopulateUIVertex(ref s_vertex, i);

			min.x = Mathf.Min(min.x, s_vertex.uv0.x);
			max.x = Mathf.Max(max.x, s_vertex.uv0.x);

			min.y = Mathf.Min(min.y, s_vertex.uv0.y);
			max.y = Mathf.Max(max.y, s_vertex.uv0.y);
		}

		Vector2 deltaSrc = max - min;
		Vector2 deltaDst = m_max - m_min;

		for (int i = 0; i < _vh.currentVertCount; ++i)
		{
			_vh.PopulateUIVertex(ref s_vertex, i);

			Vector2 uv = s_vertex.uv0;

			// normalize
			uv -= min;
			uv.x /= deltaSrc.x;
			uv.y /= deltaSrc.y;

			if (m_swapAxes)
				uv = new Vector2(uv.y, uv.x);

			// apply requested transform
			uv.x *= deltaDst.x;
			uv.y *= deltaDst.y;

			uv += m_min;

			// bring back into atlas rect again
			uv.x *= deltaSrc.x;
			uv.y *= deltaSrc.y;
			uv += min;

			s_vertex.uv0 = uv;

			_vh.SetUIVertex(s_vertex, i);
		}
	}

	public void SetDirty()
	{
		if (graphic != null)
			graphic.SetVerticesDirty();
	}
}

