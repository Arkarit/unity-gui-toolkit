using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GuiToolkit
{
	[ExecuteAlways]
	public class UiDistort : BaseMeshEffect
	{
		public Vector2 m_topLeft = Vector2.up;
		public Vector2 m_topRight = Vector2.one;
		public Vector2 m_bottomLeft = Vector2.zero;
		public Vector2 m_bottomRight = Vector2.right;

		public override void ModifyMesh( VertexHelper _vh )
		{
			if (!IsActive())
				return;

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
