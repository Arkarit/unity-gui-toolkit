using System;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace GuiToolkit
{
	[ExecuteAlways]
	public class UiGradientSimple : UiGradientBase
	{
		protected enum Type
		{
			Horizontal,
			Vertical,
		}

		[SerializeField]
		protected Color m_colorLeftOrTop;
		[SerializeField]
		protected Color m_colorRightOrBottom;
		[SerializeField]
		protected Type m_type;

		public Color ColorLeftOrTop
		{
			get => m_colorLeftOrTop;
			set
			{
				m_colorLeftOrTop = value;
				SetDirty();
			}
		}

		public Color ColorRightOrBottom
		{
			get => m_colorRightOrBottom;
			set
			{
				m_colorRightOrBottom = value;
				SetDirty();
			}
		}

		public void SetColors(Color _leftOrTop, Color _rightOrBottom)
		{
			m_colorLeftOrTop = _leftOrTop;
			m_colorRightOrBottom = _rightOrBottom;
			SetDirty();
		}

		public (Color leftOrTop, Color rightOrBottom) GetColors()
		{
			return (leftOrTop:m_colorLeftOrTop, rightOrBottom:m_colorRightOrBottom);
		}

		protected override Color GetColor( Vector2 _normVal )
		{
			return Color.Lerp( m_colorLeftOrTop, m_colorRightOrBottom, m_type == Type.Horizontal ? _normVal.x : 1.0f - _normVal.y );
		}
	}
}