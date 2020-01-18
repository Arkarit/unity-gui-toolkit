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

		private void CalculatePerspectiveValues( Rect _bounding, ref Vector2 _fixedPointA, ref Vector2 _fixedPointB, ref Vector2 _movingPointA, ref Vector2 _movingPointB, EDirection _direction )
		{
			_fixedPointA = _fixedPointB = Vector2.zero;

			Vector2 vanishingPoint = m_vanishingPoint.anchoredPosition;

			switch( _direction )
			{
				case EDirection.Vertical:
				case EDirection.Horizontal:
					_movingPointA = CalculatePerspectiveValueV( _bounding, vanishingPoint, _bounding.TopLeft());
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
// 			float a = _vanishingPoint.y - _bounding.height;
// 			float c = (_vanishingPoint - )

			float gamma = (90.0f * Mathf.Deg2Rad);
			float a0 = _vanishingPoint.x - _fixedPoint.x;
			float b0 = _vanishingPoint.y - _fixedPoint.y;
			float c0 = (_vanishingPoint - _fixedPoint).magnitude;
			float alpha = Mathf.Acos(b0/c0);
			float beta = (180.0f * Mathf.Deg2Rad - gamma - alpha);
			float b1 = _vanishingPoint.y - _bounding.yMax;
			float a1 = b1 / Mathf.Tan(beta);
			result.x = a1 - _fixedPoint.x;

			// cos (alpha) = b / c;
			// alpha = acos(b/c);
			// sin(alpha) = a / c;
			// tan(beta) = b / a
			// tan(beta) * a = b;
			// a = b / tan(beta);
			return result / 10.0f;
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
