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
		protected ESide2D m_lockedSide;

		[SerializeField]
		protected bool m_absoluteValues;

		private Vector3 m_lastPosition;
		private Vector3 m_lastPositionVanishingPoint;

		protected override void Update()
		{
			base.Update();

			if (m_vanishingPoint == null || m_rectTransform == null)
				return;

			if (m_lastPositionVanishingPoint != m_vanishingPoint.position
			||  m_lastPosition != m_rectTransform.position)
				SetDirty();

			m_lastPosition = m_rectTransform.position;
			m_lastPositionVanishingPoint = m_vanishingPoint.position;
		}

		protected override void Prepare()
		{
			if (m_vanishingPoint == null)
				return;

			switch( m_lockedSide )
			{
				case ESide2D.Top:
					CalculatePerspectiveValues( ref m_topLeft, ref m_topRight, ref m_bottomLeft, ref m_bottomRight, m_lockedSide );
					break;
				case ESide2D.Bottom:
					CalculatePerspectiveValues( ref m_bottomLeft, ref m_bottomRight, ref m_topLeft, ref m_topRight, m_lockedSide );
					break;
				case ESide2D.Left:
					CalculatePerspectiveValues( ref m_topLeft, ref m_bottomLeft, ref m_topRight, ref m_bottomRight, m_lockedSide );
					break;
				case ESide2D.Right:
					CalculatePerspectiveValues( ref m_topRight, ref m_bottomRight, ref m_topLeft, ref m_bottomLeft, m_lockedSide );
					break;
				default:
					Debug.LogError("None or multiple flags not allowed here");
					break;
			}
		}

		public override bool IsAbsolute => true;

		private void CalculatePerspectiveValues( ref Vector2 _fixedPointA, ref Vector2 _fixedPointB, ref Vector2 _movingPointA, ref Vector2 _movingPointB, ESide2D _side )
		{
			_fixedPointA = _fixedPointB = Vector2.zero;

			Vector3 vanishingPoint = m_vanishingPoint.position;
			vanishingPoint = m_rectTransform.InverseTransformPoint(vanishingPoint);

			switch( _side )
			{
				case ESide2D.Top:
					_movingPointA = CalculatePerspectiveValue( Bounding.yMin, Bounding.yMax, Bounding.xMax, vanishingPoint, false);
					_movingPointB = CalculatePerspectiveValue( Bounding.yMin, Bounding.yMax, Bounding.xMin, vanishingPoint, false);
					break;
				case ESide2D.Bottom:
					_movingPointA = CalculatePerspectiveValue( Bounding.yMax, Bounding.yMin, Bounding.xMax, vanishingPoint, false);
					_movingPointB = CalculatePerspectiveValue( Bounding.yMax, Bounding.yMin, Bounding.xMin, vanishingPoint, false);
					break;
				case ESide2D.Left:
					_movingPointA = CalculatePerspectiveValue( Bounding.xMin, Bounding.xMax, Bounding.xMax, vanishingPoint, true);
					_movingPointB = CalculatePerspectiveValue( Bounding.xMin, Bounding.xMax, Bounding.xMin, vanishingPoint, true);
					break;
				case ESide2D.Right:
					_movingPointA = CalculatePerspectiveValue( Bounding.xMax, Bounding.xMin, Bounding.xMax, vanishingPoint, true);
					_movingPointB = CalculatePerspectiveValue( Bounding.xMax, Bounding.xMin, Bounding.xMin, vanishingPoint, true);
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
			{
				_vanishingPoint.x = -_vanishingPoint.x;
				_vanishingPoint.y = _vanishingPoint.y - (_byMax - _byMin);
			}

			float b0 = _byMin - _vanishingPoint.y;
			float b1 = _byMax - _vanishingPoint.y;
			float ratio = b1 / b0;
			float a0 = _bxMax - _vanishingPoint.x;
			float a1 = a0 * ratio;
			result.x = a1 - a0;

			result.x *= m_canvas.scaleFactor;
			result.y = 0;

			if (_horizontal)
				result = result.Swap();

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
		protected SerializedProperty m_topLeftProp;
		protected SerializedProperty m_topRightProp;
		protected SerializedProperty m_bottomLeftProp;
		protected SerializedProperty m_bottomRightProp;

		public override void OnEnable()
		{
			base.OnEnable();
			m_vanishingPointProp = serializedObject.FindProperty("m_vanishingPoint");
			m_lockedSideProp = serializedObject.FindProperty("m_lockedSide");
			m_topLeftProp = serializedObject.FindProperty("m_topLeft");
			m_topRightProp = serializedObject.FindProperty("m_topRight");
			m_bottomLeftProp = serializedObject.FindProperty("m_bottomLeft");
			m_bottomRightProp = serializedObject.FindProperty("m_bottomRight");
		}

		protected override void Edit( UiDistortBase thisUiDistort )
		{
			EditorGUILayout.PropertyField(m_vanishingPointProp);
			EditorGUILayout.PropertyField(m_lockedSideProp);

			float oldLabelWidth = EditorGUIUtility.labelWidth;
			EditorGUIUtility.labelWidth = 100;

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.PropertyField(m_topLeftProp);
			EditorGUILayout.PropertyField(m_topRightProp);
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.PropertyField(m_bottomLeftProp);
			EditorGUILayout.PropertyField(m_bottomRightProp);
			EditorGUILayout.EndHorizontal();

			EditorGUIUtility.labelWidth = oldLabelWidth;
		}
	}
#endif

}
