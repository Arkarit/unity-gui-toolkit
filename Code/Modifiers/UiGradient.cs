using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace GuiToolkit
{
	[ExecuteAlways]
	/// \brief Colorize Unity graphic by an Unity gradient
	/// 
	/// This component can apply the colors of a Unity gradient to a Unity graphic by changing vertex colors
	/// (and introducing new vertices, if necessary) <BR>
	/// It supports vertical or horizontal gradients, color and alpha.
	/// It should only be used for more complex gradients; for simpler gradients of only two colors use UiGradientSimple.
	public class UiGradient : UiGradientBase
	{
		protected enum Type
		{
			Horizontal,
			Vertical,
		}

		[SerializeField]
		protected Gradient m_gradient = new Gradient();
		[SerializeField]
		protected Type m_type = Type.Vertical;
		[SerializeField]
		protected bool m_splitAtKeys = true;

		protected override bool ChangesTopology { get {return true;} }

		protected override Color GetColor( Vector2 _normVal )
		{
			return m_gradient.Evaluate( m_type == Type.Horizontal ? _normVal.x : 1.0f - _normVal.y );
		}

		protected override void Prepare( VertexHelper _vh )
		{
			if (!m_splitAtKeys)
				return;

			GradientColorKey[] colorKeys = m_gradient.colorKeys;
			GradientAlphaKey[] alphaKeys = m_gradient.alphaKeys;

			SortedSet<float> keyTimes = new SortedSet<float>();
			foreach( var key in colorKeys )
				keyTimes.Add( m_type == Type.Horizontal ? key.time : 1.0f - key.time );
			foreach( var key in alphaKeys )
				keyTimes.Add( m_type == Type.Horizontal ? key.time : 1.0f - key.time );

			if (keyTimes.Count <= 2)
				return;

			if (m_type == Type.Horizontal)
				UiModifierUtility.Subdivide( _vh, keyTimes.ToList(), null);
			else
				UiModifierUtility.Subdivide( _vh, null, keyTimes.ToList());
		}

	}
}