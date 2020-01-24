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
		protected ESide m_lockedSide;

		private Vector2 m_lastPosition;
		private Vector2 m_lastPositionVanishingPoint;

		protected void Update()
		{
			if (m_vanishingPoint == null || m_rectTransform == null)
				return;

			if (m_lastPositionVanishingPoint != m_vanishingPoint.anchoredPosition
			||  m_lastPosition != m_rectTransform.anchoredPosition)
				SetDirty();

			m_lastPosition = m_rectTransform.anchoredPosition;
			m_lastPositionVanishingPoint = m_vanishingPoint.anchoredPosition;
		}

		protected override void Prepare( Rect _bounding )
		{
			if (m_vanishingPoint == null)
				return;

			switch( m_lockedSide )
			{
				case ESide.Top:
					CalculatePerspectiveValues( _bounding, ref m_topLeft, ref m_topRight, ref m_bottomLeft, ref m_bottomRight, m_lockedSide );
					break;
				case ESide.Bottom:
					CalculatePerspectiveValues( _bounding, ref m_bottomLeft, ref m_bottomRight, ref m_topLeft, ref m_topRight, m_lockedSide );
					break;
				case ESide.Left:
					CalculatePerspectiveValues( _bounding, ref m_topLeft, ref m_bottomLeft, ref m_topRight, ref m_bottomRight, m_lockedSide );
					break;
				case ESide.Right:
					CalculatePerspectiveValues( _bounding, ref m_topRight, ref m_bottomRight, ref m_topLeft, ref m_bottomLeft, m_lockedSide );
					break;
				default:
					Debug.LogError("None or multiple flags not allowed here");
					break;
			}
		}

		protected override bool IsAbsolute() { return true; }
		protected override bool NeedsWorldBoundingBox() { return true; }

		private void CalculatePerspectiveValues( Rect _bounding, ref Vector2 _fixedPointA, ref Vector2 _fixedPointB, ref Vector2 _movingPointA, ref Vector2 _movingPointB, ESide _side )
		{
			_fixedPointA = _fixedPointB = Vector2.zero;

			Vector2 vanishingPoint = m_vanishingPoint.GetWorldCenter2D( m_canvas );

			switch( _side )
			{
				case ESide.Top:
					_movingPointA = CalculatePerspectiveValue( _bounding.yMin, _bounding.yMax, _bounding.xMin, vanishingPoint, false);
					_movingPointB = CalculatePerspectiveValue( _bounding.yMin, _bounding.yMax, _bounding.xMax, vanishingPoint, false);
					break;
				case ESide.Bottom:
					_movingPointA = CalculatePerspectiveValue( _bounding.yMax, _bounding.yMin, _bounding.xMin, vanishingPoint, false);
					_movingPointB = CalculatePerspectiveValue( _bounding.yMax, _bounding.yMin, _bounding.xMax, vanishingPoint, false);
					break;
				case ESide.Left:
					_movingPointA = CalculatePerspectiveValue( _bounding.xMin, _bounding.xMax, _bounding.yMin, vanishingPoint, true);
					_movingPointB = CalculatePerspectiveValue( _bounding.xMin, _bounding.xMax, _bounding.yMax, vanishingPoint, true);
					break;
				case ESide.Right:
					_movingPointA = CalculatePerspectiveValue( _bounding.xMax, _bounding.xMin, _bounding.yMin, vanishingPoint, true);
					_movingPointB = CalculatePerspectiveValue( _bounding.xMax, _bounding.xMin, _bounding.yMax, vanishingPoint, true);
					break;
				default:
					Debug.LogError("None or multiple flags not allowed here");
					break;
			}
		}

		private Vector2 CalculatePerspectiveValue( float _byMin, float _byMax, float _bxMax, Vector2 _vanishingPoint, bool _horizontal )
		{
			Vector2 result = new Vector2();
			result.y = 0;

			if (_horizontal)
				_vanishingPoint = _vanishingPoint.Swap();

			float b0 = _byMin - _vanishingPoint.y;
			float b1 = _byMax - _vanishingPoint.y;
			float ratio = b1 / b0;
			float a0 = _bxMax - _vanishingPoint.x;
			float a1 = a0 * ratio;
			result.x = a1 - a0;

			if (_horizontal)
				result = result.Swap();

			return result;
		}
	}

#if UNITY_EDITOR
	[CustomEditor(typeof(UiPerspectiveSemi))]
	public class UiSemiPerspectiveEditor : UiDistortEditorBase
	{
		protected SerializedProperty m_vanishingPointProp;
		protected SerializedProperty m_lockedSideProp;

		public override void OnEnable()
		{
			base.OnEnable();
			m_vanishingPointProp = serializedObject.FindProperty("m_vanishingPoint");
			m_lockedSideProp = serializedObject.FindProperty("m_lockedSide");
		}

		protected override void Edit( UiDistortBase thisUiDistort )
		{
			EditorGUILayout.PropertyField(m_vanishingPointProp);
			EditorGUILayout.PropertyField(m_lockedSideProp);
		}
	}
#endif

}
