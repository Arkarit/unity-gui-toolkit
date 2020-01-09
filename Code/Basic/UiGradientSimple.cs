using System;
using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit
{
	[ExecuteAlways]
	public class UiGradientSimple : UiGradientBase
	{
		public Color m_colorRightOrTop;
		public Color m_colorLeftOrBottom;
		public bool m_isHorizontal;

		private Color m_colorA;
		private Color m_colorB;

		protected override void Prepare( VertexHelper _vh )
		{
			_vh.PopulateUIVertex(ref s_vertex, 0);
			m_colorA = m_colorRightOrTop * s_vertex.color;
			m_colorB = m_colorLeftOrBottom * s_vertex.color;
		}

		protected override Color GetColor( Vector2 _normVal )
		{
			return Color.Lerp( m_colorA, m_colorB, m_isHorizontal ? _normVal.x : _normVal.y );
		}
	}
}