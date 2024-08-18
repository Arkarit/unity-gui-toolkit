using UnityEngine;

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
		protected EAxis2D m_axis;

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

		public EAxis2D Axis
		{
			get => m_axis;
			set
			{
				m_axis = value;
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
			return Color.Lerp( m_colorLeftOrTop, m_colorRightOrBottom, m_axis == EAxis2D.Horizontal ? _normVal.x : 1.0f - _normVal.y );
		}
	}
}