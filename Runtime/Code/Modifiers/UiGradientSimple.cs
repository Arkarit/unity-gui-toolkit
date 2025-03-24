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
		[SerializeField] protected bool m_swapped;

		public bool Swapped
		{
			get => m_swapped;
			set
			{
				m_swapped = value;
				SetDirty();
			}
		}

		public Color ColorLeftOrTop
		{
			get => m_swapped ? m_colorRightOrBottom : m_colorLeftOrTop;
			set
			{
				if (m_swapped)
					m_colorRightOrBottom = value;
				else
					m_colorLeftOrTop = value;

				SetDirty();
			}
		}

		public Color ColorRightOrBottom
		{
			get => m_swapped ? m_colorLeftOrTop : m_colorRightOrBottom;
			set
			{
				if (m_swapped)
					m_colorLeftOrTop = value;
				else
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
			ColorLeftOrTop = _leftOrTop;
			ColorRightOrBottom = _rightOrBottom;
			SetDirty();
		}

		public (Color leftOrTop, Color rightOrBottom) GetColors()
		{
			return (leftOrTop:ColorLeftOrTop, rightOrBottom:ColorRightOrBottom);
		}

		protected override Color GetColor(Vector2 _normVal)
		{
			return Color.Lerp( ColorLeftOrTop, ColorRightOrBottom, m_orientation == EOrientation.Horizontal ? _normVal.x : 1.0f - _normVal.y );
		}
	}
}
