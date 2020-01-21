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
		[SerializeField]
		protected Gradient m_gradient = new Gradient();
		[SerializeField]
		protected EDirection m_direction;

		protected override bool ChangesTopology { get {return true;} }

		protected override Color GetColor( Vector2 _normVal )
		{
			return m_gradient.Evaluate( m_direction == EDirection.Horizontal ? _normVal.x : 1.0f - _normVal.y );
		}

		protected override void Prepare( VertexHelper _vh )
		{
			GradientColorKey[] colorKeys = m_gradient.colorKeys;
			GradientAlphaKey[] alphaKeys = m_gradient.alphaKeys;

			SortedSet<float> keyTimes = new SortedSet<float>();
			foreach( var key in colorKeys )
				keyTimes.Add( m_direction == EDirection.Horizontal ? key.time : 1.0f - key.time );
			foreach( var key in alphaKeys )
				keyTimes.Add( m_direction == EDirection.Horizontal ? key.time : 1.0f - key.time );

			if (keyTimes.Count <= 2)
				return;

			if (m_direction == EDirection.Horizontal)
				UiModifierUtil.Subdivide( _vh, keyTimes.ToList(), null);
			else
				UiModifierUtil.Subdivide( _vh, null, keyTimes.ToList());
		}

	}
}