using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit
{
	[ExecuteAlways]
	public class UiGradient : UiGradientBase
	{
		[SerializeField]
		protected Gradient m_gradient = new Gradient();
		[SerializeField]
		protected EAxis2D m_axis = EAxis2D.Vertical;
		[SerializeField]
		protected bool m_splitAtKeys = true;

		public Gradient Gradient
		{
			get => m_gradient;
			set
			{
				m_gradient = value;
				SetDirty();
			}
		}

		public EAxis2D Axis
		{
			get => m_axis;
			set
			{
				m_axis = value;
				SetDirty();
			}
		}

		public bool SplitAtKeys
		{
			get => m_splitAtKeys;
			set
			{
				m_splitAtKeys = value;
				SetDirty();
			}
		}

		protected override bool ChangesTopology { get {return true;} }

		protected override Color GetColor( Vector2 _normVal )
		{
			return m_gradient.Evaluate( m_axis == EAxis2D.Horizontal ? _normVal.x : 1.0f - _normVal.y );
		}

		protected override void Prepare( VertexHelper _vh )
		{
			if (!m_splitAtKeys)
				return;

			GradientColorKey[] colorKeys = m_gradient.colorKeys;
			GradientAlphaKey[] alphaKeys = m_gradient.alphaKeys;

			SortedSet<float> keyTimes = new SortedSet<float>();
			foreach( var key in colorKeys )
				keyTimes.Add( m_axis == EAxis2D.Horizontal ? key.time : 1.0f - key.time );
			foreach( var key in alphaKeys )
				keyTimes.Add( m_axis == EAxis2D.Horizontal ? key.time : 1.0f - key.time );

			if (keyTimes.Count <= 2)
				return;

			if (m_axis == EAxis2D.Horizontal)
				UiModifierUtility.Subdivide( _vh, keyTimes.ToList(), null);
			else
				UiModifierUtility.Subdivide( _vh, null, keyTimes.ToList());
		}

	}
}