using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GuiToolkit
{
	[ExecuteAlways]
	public class UiPerspectiveSemi: UiDistortBase
	{
		[SerializeField]
		protected RectTransform m_vanishingPoint;

		[SerializeField]
		protected ESide m_lockedVertexSide;

		private Vector2 m_lastPosition;

		protected void Update()
		{
			if (m_vanishingPoint == null)
				return;
			if (m_lastPosition != m_vanishingPoint.anchoredPosition)
				SetDirty();
			m_lastPosition = m_vanishingPoint.anchoredPosition;
		}

		protected override void Prepare( Rect _bounding )
		{
			if (m_vanishingPoint == null)
				return;

			switch( m_lockedVertexSide )
			{
				case ESide.Top:
					CalculatePerspectiveValues( _bounding, ref m_topLeft, ref m_topRight, ref m_bottomLeft, ref m_bottomRight, EDirection.Vertical );
					break;
				case ESide.Bottom:
					CalculatePerspectiveValues( _bounding, ref m_bottomLeft, ref m_bottomRight, ref m_topLeft, ref m_topRight, EDirection.Vertical );
					break;
				case ESide.Left:
					CalculatePerspectiveValues( _bounding, ref m_topLeft, ref m_bottomLeft, ref m_topRight, ref m_bottomRight, EDirection.Horizontal );
					break;
				case ESide.Right:
					CalculatePerspectiveValues( _bounding, ref m_topRight, ref m_bottomRight, ref m_topLeft, ref m_bottomLeft, EDirection.Horizontal );
					break;
				default:
					Debug.LogError("None or multiple flags not allowed here");
					break;
			}
		}

		protected override bool IsAbsolute() { return true; }
		protected override bool NeedsWorldBoundingBox() { return true; }

		private void CalculatePerspectiveValues( Rect _bounding, ref Vector2 _fixedPointA, ref Vector2 _fixedPointB, ref Vector2 _movingPointA, ref Vector2 _movingPointB, EDirection _direction )
		{
			_fixedPointA = _fixedPointB = Vector2.zero;

			Vector2 vanishingPoint = m_vanishingPoint.GetWorldCenter2D( m_canvas );

			switch( _direction )
			{
				case EDirection.Vertical:
				case EDirection.Horizontal:
					_movingPointA = Vector2.zero;
					_movingPointB = CalculatePerspectiveValueV( _bounding, vanishingPoint, _bounding.TopRight());
					break;
				default:
					Debug.LogError("None or multiple flags not allowed here");
					break;
			}
		}

		private Vector2 CalculatePerspectiveValueV(  Rect _bounding, Vector2 _vanishingPoint, Vector2 _fixedPoint )
		{
			Vector2 result = new Vector2();
			result.y = 0;

			float b0 = _bounding.yMin - _vanishingPoint.y;
			float b1 = _bounding.yMax - _vanishingPoint.y;
			float ratio = b1 / b0;
			float a0 = _bounding.xMax - _vanishingPoint.x;
			float a1 = a0 * ratio;
			result.x = a1 - a0;

			return result;
		}
	}

#if UNITY_EDITOR
	[CustomEditor(typeof(UiPerspectiveSemi))]
	public class UiSemiPerspectiveEditor : UiDistortEditorBase
	{
		protected SerializedProperty m_vanishingPointProp;
		protected SerializedProperty m_lockedVertexSideProp;

		public override void OnEnable()
		{
			base.OnEnable();
			m_vanishingPointProp = serializedObject.FindProperty("m_vanishingPoint");
			m_lockedVertexSideProp = serializedObject.FindProperty("m_lockedVertexSide");
		}

		protected override void Edit( UiDistortBase thisUiDistort )
		{
			EditorGUILayout.PropertyField(m_vanishingPointProp);
			EditorGUILayout.PropertyField(m_lockedVertexSideProp);
		}

	}
#endif

}
