using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GuiToolkit
{
	[ExecuteAlways]
	public class UiDistort : BaseMeshEffect
	{
		public enum EMode
		{
			Distort,
			Skew,
			Perspective,
		}
		[SerializeField]
		private EMode m_mode;

		[SerializeField]
		[VectorRange (-1, 1, -1, 1, true)]
		private Vector2 m_topLeft = Vector2.zero;

		[SerializeField]
		[VectorRange (-1, 1, -1, 1, true)]
		private Vector2 m_topRight = Vector2.zero;

		[SerializeField]
		[VectorRange (-1, 1, -1, 1, true)]
		private Vector2 m_bottomLeft = Vector2.zero;

		[SerializeField]
		[VectorRange (-1, 1, -1, 1, true)]
		private Vector2 m_bottomRight = Vector2.zero;

		[SerializeField]
		private EDirection m_mirrorDirection;

		[SerializeField]
		private RectTransform m_vanishingPoint;

		[SerializeField]
		private ESide m_lockedVertexSide;

		private static readonly List<UIVertex> s_verts = new List<UIVertex>();
		private static UIVertex s_vertex;

		public void SetMirror( EDirection _direction )
		{
			m_mirrorDirection = _direction;
			this.SetDirty();
		}

		public override void ModifyMesh( VertexHelper _vertexHelper )
		{
			if (!IsActive())
				return;

			_vertexHelper.GetUIVertexStream(s_verts);

			Rect bounding = UiModifierUtil.GetMinMaxRect(s_verts);

			if (m_mode == EMode.Perspective)
				CalculatePerspectiveValues( bounding );

			Vector2 size = bounding.size;

			Vector2 tl = m_topLeft * size;
			Vector2 bl = m_bottomLeft * size;
			Vector2 tr = m_topRight * size;
			Vector2 br = m_bottomRight * size;

			bool mirrorHorizontal = m_mirrorDirection.IsFlagSet(EDirection.Horizontal);
			bool mirrorVertical = m_mirrorDirection.IsFlagSet(EDirection.Vertical);

			Vector2 mirrorVec = new Vector2(mirrorHorizontal ? -1 : 1, mirrorVertical ? -1 : 1);

			if (mirrorHorizontal)
			{
				Swap( ref tl, ref tr );
				Swap( ref bl, ref br );
			}

			if (mirrorVertical)
			{
				Swap( ref tl, ref bl );
				Swap( ref tr, ref br );
			}

			for (int i = 0; i < _vertexHelper.currentVertCount; ++i)
			{
				_vertexHelper.PopulateUIVertex(ref s_vertex, i);

				Vector2 pointNormalized = s_vertex.position.GetNormalizedPointInRect(bounding);

				Vector2 pInfluenceTL = new Vector2(1.0f - pointNormalized.x, pointNormalized.y);
				Vector2 pInfluenceTR = pointNormalized;
				Vector2 pInfluenceBL = new Vector2(1.0f - pointNormalized.x, 1.0f - pointNormalized.y);
				Vector2 pInfluenceBR = new Vector2(pointNormalized.x, 1.0f - pointNormalized.y);

				float influenceTL = pInfluenceTL.x * pInfluenceTL.y;
				float influenceTR = pInfluenceTR.x * pInfluenceTR.y;
				float influenceBL = pInfluenceBL.x * pInfluenceBL.y;
				float influenceBR = pInfluenceBR.x * pInfluenceBR.y;

				Vector2 point = s_vertex.position.Xy();
				point += 
					  tl * influenceTL * mirrorVec
					+ tr * influenceTR * mirrorVec
					+ bl * influenceBL * mirrorVec
					+ br * influenceBR * mirrorVec;

				s_vertex.position = new Vector3(point.x, point.y, s_vertex.position.z);

				_vertexHelper.SetUIVertex(s_vertex, i);
			}
		}

		private void CalculatePerspectiveValues( Rect _bounding )
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

		private void Swap(ref Vector2 a, ref Vector2 b)
		{
			Vector2 t = b;
			b = a;
			a = t;
		}

#if UNITY_EDITOR
		protected override void OnValidate()
		{
			this.SetDirty();
		}
#endif
	}

#if UNITY_EDITOR
	[CustomEditor(typeof(UiDistort))]
	public class UiDistortEditor : Editor
	{
		private SerializedProperty m_topLeftProp;
		private SerializedProperty m_topRightProp;
		private SerializedProperty m_bottomLeftProp;
		private SerializedProperty m_bottomRightProp;
		private SerializedProperty m_mirrorDirectionProp;
		private SerializedProperty m_vanishingPointProp;
		private SerializedProperty m_lockedVertexSideProp;

		public void OnEnable()
		{
			m_topLeftProp = serializedObject.FindProperty("m_topLeft");
			m_topRightProp = serializedObject.FindProperty("m_topRight");
			m_bottomLeftProp = serializedObject.FindProperty("m_bottomLeft");
			m_bottomRightProp = serializedObject.FindProperty("m_bottomRight");
			m_mirrorDirectionProp = serializedObject.FindProperty("m_mirrorDirection");
			m_vanishingPointProp = serializedObject.FindProperty("m_vanishingPoint");
			m_lockedVertexSideProp = serializedObject.FindProperty("m_lockedVertexSide");
		}

		public override void OnInspectorGUI()
		{
			UiDistort thisUiDistort = (UiDistort)target;

			SerializedProperty modeProp = serializedObject.FindProperty("m_mode");
			EditorGUILayout.PropertyField(modeProp);
			UiDistort.EMode mode = (UiDistort.EMode) modeProp.intValue;

			switch (mode)
			{
				case UiDistort.EMode.Distort:
					EditDistort(thisUiDistort);
					break;
				case UiDistort.EMode.Skew:
					EditSkew(thisUiDistort);
					break;
				case UiDistort.EMode.Perspective:
					EditPerspective(thisUiDistort);
					break;
				default:
					break;
			}

			serializedObject.ApplyModifiedProperties();
		}

		private void EditDistort( UiDistort thisUiDistort )
		{
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

			if (UiEditorUtility.BoolBar<EDirection>(m_mirrorDirectionProp, "Mirror"))
				thisUiDistort.SetDirty();
		}

		private void EditSkew( UiDistort thisUiDistort )
		{
			float skewHorizontal = m_topLeftProp.vector2Value.x;
			float skewVertical = m_topLeftProp.vector2Value.y;
			skewHorizontal = EditorGUILayout.FloatField("Horizontal", skewHorizontal);
			skewVertical = EditorGUILayout.FloatField("Vertical", skewVertical);

			SetSkewValue(m_topLeftProp, skewHorizontal, skewVertical);
			SetSkewValue(m_topRightProp, skewHorizontal, -skewVertical);
			SetSkewValue(m_bottomLeftProp, -skewHorizontal, skewVertical);
			SetSkewValue(m_bottomRightProp, -skewHorizontal, -skewVertical);

			if (UiEditorUtility.BoolBar<EDirection>(serializedObject.FindProperty("m_mirrorDirection"), "Mirror"))
				thisUiDistort.SetDirty();
		}

		private void EditPerspective( UiDistort thisUiDistort )
		{
			EditorGUILayout.PropertyField(m_vanishingPointProp);
			EditorGUILayout.PropertyField(m_lockedVertexSideProp);
		}

		private void SetSkewValue(SerializedProperty _prop, float _x, float _y)
		{
			Vector2 vec = new Vector2(_x, _y);
			_prop.vector2Value = vec;
		}

	}
#endif


}
