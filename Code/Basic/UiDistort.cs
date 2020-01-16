using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GuiToolkit
{
	[ExecuteAlways]
	public class UiDistort : BaseMeshEffect
	{
#if UNITY_EDITOR
		public enum EMode
		{
			Distort,
			Skew,
		}
		[SerializeField]
		private EMode m_mode;
#endif

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

		public void OnEnable()
		{
			m_topLeftProp = serializedObject.FindProperty("m_topLeft");
			m_topRightProp = serializedObject.FindProperty("m_topRight");
			m_bottomLeftProp = serializedObject.FindProperty("m_bottomLeft");
			m_bottomRightProp = serializedObject.FindProperty("m_bottomRight");
			m_mirrorDirectionProp = serializedObject.FindProperty("m_mirrorDirection");
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

		private void SetSkewValue(SerializedProperty _prop, float _x, float _y)
		{
			Vector2 vec = new Vector2(_x, _y);
			_prop.vector2Value = vec;
		}

	}
#endif


}
