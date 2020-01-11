using System;
using System.Collections.Generic;
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

		protected override void Prepare( VertexHelper _vh )
		{
			GradientColorKey[] colorKeys = m_gradient.colorKeys;
			GradientAlphaKey[] alphaKeys = m_gradient.alphaKeys;

			SortedSet<float> keyTimes = new SortedSet<float>();
			foreach( var key in colorKeys )
				keyTimes.Add( m_isHorizontal ? key.time : 1.0f - key.time );
			foreach( var key in alphaKeys )
				keyTimes.Add( m_isHorizontal ? key.time : 1.0f - key.time );

			if (keyTimes.Count <= 2)
				return;

			if (m_isHorizontal)
				UiTesselationUtil.Subdivide( _vh, keyTimes.ToList(), null);
			else
				UiTesselationUtil.Subdivide( _vh, null, keyTimes.ToList());
		}

	}
}