using System;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace GuiToolkit
{
	[ExecuteAlways]
	public class UiGradientSimple : UiGradientBase
	{
		public Color m_colorLeftOrTop;
		public Color m_colorRightOrBottom;
		public bool m_isHorizontal;

		private Color m_colorA;
		private Color m_colorB;

		protected override void Prepare( VertexHelper _vh )
		{
			_vh.PopulateUIVertex(ref s_vertex, 0);
			m_colorA = m_colorLeftOrTop * s_vertex.color;
			m_colorB = m_colorRightOrBottom * s_vertex.color;
		}

		protected override Color GetColor( Vector2 _normVal )
		{
			return Color.Lerp( m_colorA, m_colorB, m_isHorizontal ? _normVal.x : 1.0f - _normVal.y );
		}
	}
}