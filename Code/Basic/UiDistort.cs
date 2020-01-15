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
		[VectorRange (-1, 1, -1, 1, true)]
		public Vector2 m_topLeft = Vector2.zero;
		[VectorRange (-1, 1, -1, 1, true)]
		public Vector2 m_topRight = Vector2.zero;
		[VectorRange (-1, 1, -1, 1, true)]
		public Vector2 m_bottomLeft = Vector2.zero;
		[VectorRange (-1, 1, -1, 1, true)]
		public Vector2 m_bottomRight = Vector2.zero;

		private static readonly List<UIVertex> s_verts = new List<UIVertex>();
		private static UIVertex s_vertex;


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
					  tl * influenceTL
					+ tr * influenceTR
					+ bl * influenceBL
					+ br * influenceBR;

				s_vertex.position = new Vector3(point.x, point.y, s_vertex.position.z);

				_vertexHelper.SetUIVertex(s_vertex, i);
			}
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
		public override void OnInspectorGUI()
		{
			UiDistort thisUiDistort = (UiDistort)target;
			
			float oldLabelWidth = EditorGUIUtility.labelWidth;
			EditorGUIUtility.labelWidth = 80;

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.PropertyField(serializedObject.FindProperty("m_topLeft"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("m_topRight"));
			EditorGUILayout.EndHorizontal();

			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.PropertyField(serializedObject.FindProperty("m_bottomLeft"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("m_bottomRight"));
			EditorGUILayout.EndHorizontal();

			EditorGUIUtility.labelWidth = oldLabelWidth;

			serializedObject.ApplyModifiedProperties();
		}
	}
#endif


}
