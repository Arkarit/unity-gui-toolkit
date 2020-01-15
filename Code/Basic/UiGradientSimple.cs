using System;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace GuiToolkit
{
	[ExecuteAlways]
	public class UiGradientSimple : UiGradientBase
	{
		[SerializeField]
		protected Color m_colorLeftOrTop;
		[SerializeField]
		protected Color m_colorRightOrBottom;
		[SerializeField]
		protected EDirection m_direction;

		protected override Color GetColor( Vector2 _normVal )
		{
			return Color.Lerp( m_colorLeftOrTop, m_colorRightOrBottom, m_direction == EDirection.Horizontal ? _normVal.x : 1.0f - _normVal.y );
		}
	}
}