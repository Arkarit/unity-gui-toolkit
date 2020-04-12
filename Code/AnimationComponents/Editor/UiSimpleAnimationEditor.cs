using Unity;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit
{
	[CustomEditor(typeof(UiSimpleAnimation))]
	public class UiSimpleAnimationEditor : UiSimpleAnimationBaseEditor
	{
		protected SerializedProperty m_supportProp;
		protected SerializedProperty m_targetProp;
		protected SerializedProperty m_canvasScalerProp;
		protected SerializedProperty m_canvasRectTransformProp;
		protected SerializedProperty m_scaleByCanvasScalerProp;
		protected SerializedProperty m_posXStartProp;
		protected SerializedProperty m_posXEndProp;
		protected SerializedProperty m_posXCurveProp;
		protected SerializedProperty m_posYStartProp;
		protected SerializedProperty m_posYEndProp;
		protected SerializedProperty m_posYCurveProp;
		protected SerializedProperty m_rotZStartProp;
		protected SerializedProperty m_rotZEndProp;
		protected SerializedProperty m_rotZCurveProp;
		protected SerializedProperty m_scaleXStartProp;
		protected SerializedProperty m_scaleXEndProp;
		protected SerializedProperty m_scaleXCurveProp;
		protected SerializedProperty m_scaleYStartProp;
		protected SerializedProperty m_scaleYEndProp;
		protected SerializedProperty m_scaleYCurveProp;
		protected SerializedProperty m_scaleLockedProp;
		protected SerializedProperty m_alphaCurveProp;
		protected SerializedProperty m_alphaImageProp;
		protected SerializedProperty m_alphaCanvasGroupProp;

		public override void OnEnable()
		{
			base.OnEnable();

			m_supportProp = serializedObject.FindProperty("m_support");
			m_targetProp = serializedObject.FindProperty("m_target");
			m_canvasScalerProp = serializedObject.FindProperty("m_canvasScaler");
			m_canvasRectTransformProp = serializedObject.FindProperty("m_canvasRectTransform");
			m_scaleByCanvasScalerProp = serializedObject.FindProperty("m_scaleByCanvasScaler");
			m_posXStartProp = serializedObject.FindProperty("m_posXStart");
			m_posXEndProp = serializedObject.FindProperty("m_posXEnd");
			m_posXCurveProp = serializedObject.FindProperty("m_posXCurve");
			m_posYStartProp = serializedObject.FindProperty("m_posYStart");
			m_posYEndProp = serializedObject.FindProperty("m_posYEnd");
			m_posYCurveProp = serializedObject.FindProperty("m_posYCurve");
			m_rotZStartProp = serializedObject.FindProperty("m_rotZStart");
			m_rotZEndProp = serializedObject.FindProperty("m_rotZEnd");
			m_rotZCurveProp = serializedObject.FindProperty("m_rotZCurve");
			m_scaleXStartProp = serializedObject.FindProperty("m_scaleXStart");
			m_scaleXEndProp = serializedObject.FindProperty("m_scaleXEnd");
			m_scaleXCurveProp = serializedObject.FindProperty("m_scaleXCurve");
			m_scaleYStartProp = serializedObject.FindProperty("m_scaleYStart");
			m_scaleYEndProp = serializedObject.FindProperty("m_scaleYEnd");
			m_scaleYCurveProp = serializedObject.FindProperty("m_scaleYCurve");
			m_scaleLockedProp = serializedObject.FindProperty("m_scaleLocked");
			m_alphaCurveProp = serializedObject.FindProperty("m_alphaCurve");
			m_alphaImageProp = serializedObject.FindProperty("m_alphaImage");
			m_alphaCanvasGroupProp = serializedObject.FindProperty("m_alphaCanvasGroup");
		}

		public override void EditSubClass()
		{
			UiSimpleAnimation thisUiSimpleAnimation = (UiSimpleAnimation)target;

			GUILayout.Label("UiSimpleAnimation Basic settings:", EditorStyles.boldLabel);
			EditorGUILayout.PropertyField(m_targetProp);
			if (m_targetProp.objectReferenceValue == null)
			{
				m_targetProp.objectReferenceValue = (RectTransform) thisUiSimpleAnimation.transform;
			}
			EditorGUILayout.PropertyField(m_scaleByCanvasScalerProp);
			if (m_scaleByCanvasScalerProp.boolValue)
			{
				EditorGUILayout.PropertyField(m_canvasScalerProp);
				if (m_canvasScalerProp.objectReferenceValue == null)
					m_canvasScalerProp.objectReferenceValue = thisUiSimpleAnimation.GetComponentInParent<CanvasScaler>();

				CanvasScaler scaler = m_canvasScalerProp.objectReferenceValue as CanvasScaler;
	
				if (scaler != null)
					m_canvasRectTransformProp.objectReferenceValue = scaler.transform as RectTransform;
			}
			EditorGUILayout.Space();

			GUILayout.Label("Properties support:", EditorStyles.boldLabel);
			UiSimpleAnimation.ESupport support = thisUiSimpleAnimation.Support;
			UiEditorUtility.BoolBar(ref support, null, 3);
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
			else
			{
				m_posXCurveProp.animationCurveValue.keys = new Keyframe[0];
			}

			if( support.HasFlags(UiSimpleAnimation.ESupport.PositionY))
			{
				GUILayout.Label("Position Y:", EditorStyles.boldLabel);
				EditorGUILayout.PropertyField(m_posYStartProp, new GUIContent("Start"));
				EditorGUILayout.PropertyField(m_posYEndProp, new GUIContent("End"));
				EditorGUILayout.PropertyField(m_posYCurveProp, new GUIContent("Norm. Curve"));
				EditorGUILayout.Space();
			}
			else
			{
				m_posYCurveProp.animationCurveValue = new AnimationCurve();
			}

			if( support.HasFlags(UiSimpleAnimation.ESupport.RotationZ))
			{
				GUILayout.Label("Rotation Z:", EditorStyles.boldLabel);
				EditorGUILayout.PropertyField(m_rotZStartProp, new GUIContent("Start"));
				EditorGUILayout.PropertyField(m_rotZEndProp, new GUIContent("End"));
				EditorGUILayout.PropertyField(m_rotZCurveProp, new GUIContent("Norm. Curve"));
				EditorGUILayout.Space();
			}
			else
			{
				m_rotZCurveProp.animationCurveValue = new AnimationCurve();
			}

			if( support.HasFlags(UiSimpleAnimation.ESupport.ScaleX | UiSimpleAnimation.ESupport.ScaleY))
			{
				GUILayout.Label("Scale locked:", EditorStyles.boldLabel);
				EditorGUILayout.PropertyField(m_scaleLockedProp);
				EditorGUILayout.Space();
			}

			if( support.HasFlags(UiSimpleAnimation.ESupport.ScaleX) || (support.HasFlags(UiSimpleAnimation.ESupport.ScaleY) && m_scaleLockedProp.boolValue))
			{
				if (m_scaleLockedProp.boolValue)
					GUILayout.Label("Scale:", EditorStyles.boldLabel);
				else
					GUILayout.Label("Scale X:", EditorStyles.boldLabel);

				EditorGUILayout.PropertyField(m_scaleXStartProp, new GUIContent("Start"));
				EditorGUILayout.PropertyField(m_scaleXEndProp, new GUIContent("End"));
				EditorGUILayout.PropertyField(m_scaleXCurveProp, new GUIContent("Norm. Curve"));
				EditorGUILayout.Space();
			}
			else
			{
				m_scaleXCurveProp.animationCurveValue = new AnimationCurve();
			}

			if( support.HasFlags(UiSimpleAnimation.ESupport.ScaleY) && !m_scaleLockedProp.boolValue)
			{
				GUILayout.Label("Scale Y:", EditorStyles.boldLabel);

				EditorGUILayout.PropertyField(m_scaleYStartProp, new GUIContent("Start"));
				EditorGUILayout.PropertyField(m_scaleYEndProp, new GUIContent("End"));
				EditorGUILayout.PropertyField(m_scaleYCurveProp, new GUIContent("Norm. Curve"));
				EditorGUILayout.Space();
			}
			else
			{
				m_scaleYCurveProp.animationCurveValue = new AnimationCurve();
			}

			if( support.HasFlags(UiSimpleAnimation.ESupport.Alpha))
			{
				GUILayout.Label("Alpha:", EditorStyles.boldLabel);
				EditorGUILayout.PropertyField(m_alphaImageProp);
				EditorGUILayout.PropertyField(m_alphaCanvasGroupProp);
				EditorGUILayout.PropertyField(m_alphaCurveProp, new GUIContent("Norm. Curve"));
				EditorGUILayout.Space();
			}
			else
			{
				m_alphaCurveProp.animationCurveValue = new AnimationCurve();
			}

		}
	}
}