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

		protected override bool IsAbsolute { get { return true; } }
		protected override bool NeedsWorldBoundingBox { get { return true; } }

		private void CalculatePerspectiveValues( Rect _bounding, ref Vector2 _fixedPointA, ref Vector2 _fixedPointB, ref Vector2 _movingPointA, ref Vector2 _movingPointB, ESide _side )
		{
			_fixedPointA = _fixedPointB = Vector2.zero;

			Vector2 vanishingPoint = m_vanishingPoint.GetWorldCenter2D( m_canvas );

			(Rect bounding, Vector2 offset) = _bounding.BringToCenter();
			vanishingPoint += offset;

			switch( _side )
			{
				case ESide.Top:
					_movingPointA = CalculatePerspectiveValue( bounding.yMin, bounding.yMax, bounding.xMax, vanishingPoint, false);
					_movingPointB = CalculatePerspectiveValue( bounding.yMin, bounding.yMax, bounding.xMin, vanishingPoint, false);
					break;
				case ESide.Bottom:
					_movingPointA = CalculatePerspectiveValue( bounding.yMax, bounding.yMin, bounding.xMax, vanishingPoint, false);
					_movingPointB = CalculatePerspectiveValue( bounding.yMax, bounding.yMin, bounding.xMin, vanishingPoint, false);
					break;
				case ESide.Left:
					_movingPointA = CalculatePerspectiveValue( bounding.xMin, bounding.xMax, bounding.xMax, vanishingPoint, true);
					_movingPointB = CalculatePerspectiveValue( bounding.xMin, bounding.xMax, bounding.xMin, vanishingPoint, true);
					break;
				case ESide.Right:
					_movingPointA = CalculatePerspectiveValue( bounding.xMax, bounding.xMin, bounding.xMax, vanishingPoint, true);
					_movingPointB = CalculatePerspectiveValue( bounding.xMax, bounding.xMin, bounding.xMin, vanishingPoint, true);
					break;
				default:
					Debug.LogError("None or multiple flags not allowed here");
					break;
			}
		}

		private Vector2 CalculatePerspectiveValue( float _byMin, float _byMax, float _bxMax, Vector2 _vanishingPoint, bool _horizontal )
		{
			Vector2 result = new Vector2();

			if (_horizontal)
				_vanishingPoint = _vanishingPoint.Swap();
			else
				_vanishingPoint.x = -_vanishingPoint.x;

			float b0 = _byMin - _vanishingPoint.y;
			float b1 = _byMax - _vanishingPoint.y;
			float ratio = b1 / b0;
			float a0 = _bxMax - _vanishingPoint.x;
			float a1 = a0 * ratio;
			result.x = a1 - a0;

			result.x *= m_canvas.scaleFactor;
			result = m_rectTransform.InverseTransformVector(result);
			result.y = 0;

			if (_horizontal)
				result = result.Swap();
			else
				result = -result;

			//Debug.Log($"_byMin:{_byMin} _byMax:{_byMax} _bxMax:{_bxMax} _vanishingPoint:{_vanishingPoint} b0:{b0} b1:{b1} ratio:{ratio} a0:{a0} a1:{a1} result:{result}");

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
