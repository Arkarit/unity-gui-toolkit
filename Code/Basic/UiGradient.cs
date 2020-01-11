using System;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace GuiToolkit
{
	[ExecuteAlways]
	public class UiGradient : UiGradientBase
	{
		public Gradient m_gradient = new Gradient();
		public bool m_isHorizontal;

		protected override Color GetColor( Vector2 _normVal )
		{
			return m_gradient.Evaluate( m_isHorizontal ? _normVal.x : 1.0f - _normVal.y );
		}
	}
}