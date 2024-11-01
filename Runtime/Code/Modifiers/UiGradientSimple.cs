using UnityEngine;
using UnityEngine.Serialization;

namespace GuiToolkit
{
	[ExecuteAlways]
	public class UiGradientSimple : UiGradientBase
	{
		public enum EOrientation
		{
			Horizontal,
			Vertical,
		}

		[FormerlySerializedAs("m_ColorLeftOrTop")] 
		[SerializeField] protected Color m_colorLeftOrTop;
		[FormerlySerializedAs("m_ColorRightOrBottom")] 
		[SerializeField] protected Color m_colorRightOrBottom;
		[FormerlySerializedAs("m_Orientation")]
		[FormerlySerializedAs("m_type")] 
		[SerializeField] protected EOrientation m_orientation;

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
		
		public EOrientation Orientation
		{
			get => m_orientation;
			set => m_orientation = value;
		}

		public void SetColors(Color _leftOrTop, Color _rightOrBottom)
		{
			m_colorLeftOrTop = _leftOrTop;
			m_colorRightOrBottom = _rightOrBottom;
			SetDirty();
		}

		public (Color _leftOrTop, Color _rightOrBottom) GetColors()
		{
			return (_leftOrTop:m_colorLeftOrTop, _rightOrBottom:m_colorRightOrBottom);
		}

		protected override Color GetColor(Vector2 _normVal)
		{
			return Color.Lerp( m_colorLeftOrTop, m_colorRightOrBottom, m_orientation == EOrientation.Horizontal ? _normVal.x : 1.0f - _normVal.y );
		}
	}
}
