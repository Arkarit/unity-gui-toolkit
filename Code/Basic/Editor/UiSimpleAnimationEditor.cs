using Unity;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit
{
	[CustomEditor(typeof(UiSimpleAnimation))]
	public class UiSimpleAnimationEditor : UiSimpleAnimationBaseEditor
	{
		protected SerializedProperty m_supportProp;
		protected SerializedProperty m_targetProp;
		protected SerializedProperty m_posXStartProp;
		protected SerializedProperty m_posXEndProp;
		protected SerializedProperty m_posXCurveProp;
		protected SerializedProperty m_posYStartProp;
		protected SerializedProperty m_posYEndProp;
		protected SerializedProperty m_posYCurveProp;

		public override void OnEnable()
		{
			base.OnEnable();

			m_supportProp = serializedObject.FindProperty("m_support");
			m_targetProp = serializedObject.FindProperty("m_target");
			m_posXStartProp = serializedObject.FindProperty("m_posXStart");
			m_posXEndProp = serializedObject.FindProperty("m_posXEnd");
			m_posXCurveProp = serializedObject.FindProperty("m_posXCurve");
			m_posYStartProp = serializedObject.FindProperty("m_posYStart");
			m_posYEndProp = serializedObject.FindProperty("m_posYEnd");
			m_posYCurveProp = serializedObject.FindProperty("m_posYCurve");
		}

		public override void EditSubClass()
		{
			UiSimpleAnimation thisUiSimpleAnimation = (UiSimpleAnimation)target;

			GUILayout.Label("UiSimpleAnimation Target:", EditorStyles.boldLabel);
			EditorGUILayout.PropertyField(m_targetProp);
			if (m_targetProp.objectReferenceValue == null)
			{
				m_targetProp.objectReferenceValue = (RectTransform) thisUiSimpleAnimation.transform;
			}
			EditorGUILayout.Space();

			GUILayout.Label("Properties support:", EditorStyles.boldLabel);
			UiSimpleAnimation.ESupport support = thisUiSimpleAnimation.Support;
			UiEditorUtility.BoolBar(ref support);
			m_supportProp.intValue = (int)(object)support;
			EditorGUILayout.Space();

			if( support.HasFlags(UiSimpleAnimation.ESupport.PositionX))
			{
				GUILayout.Label("Position X:", EditorStyles.boldLabel);
				EditorGUILayout.PropertyField(m_posXStartProp, new GUIContent("Start"));
				EditorGUILayout.PropertyField(m_posXEndProp, new GUIContent("End"));
				EditorGUILayout.PropertyField(m_posXCurveProp, new GUIContent("Norm. Curve"));
				EditorGUILayout.Space();
			}

			if( support.HasFlags(UiSimpleAnimation.ESupport.PositionY))
			{
				GUILayout.Label("Position Y:", EditorStyles.boldLabel);
				EditorGUILayout.PropertyField(m_posYStartProp, new GUIContent("Start"));
				EditorGUILayout.PropertyField(m_posYEndProp, new GUIContent("End"));
				EditorGUILayout.PropertyField(m_posYCurveProp, new GUIContent("Norm. Curve"));
				EditorGUILayout.Space();
			}
		}
	}
}