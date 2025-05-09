using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Editor
{
	[CustomEditor(typeof(UiRoundedImage))]
	public class UiRoundedImageEditor : UnityEditor.Editor
	{
		protected SerializedProperty m_SpriteProp;
		protected SerializedProperty m_MaterialProp;
		protected SerializedProperty m_ColorProp;
		protected SerializedProperty m_RaycastTargetProp;
		protected SerializedProperty m_RaycastPaddingProp;
		protected SerializedProperty m_MaskableProp;
		
		protected SerializedProperty m_cornerSegmentsProp;
		protected SerializedProperty m_radiusProp;
		protected SerializedProperty m_frameSizeProp;
		protected SerializedProperty m_fadeSizeProp;
		protected SerializedProperty m_invertMaskProp;
		protected SerializedProperty m_paddingProp;
		protected SerializedProperty m_paddingFixedHorizontalProp;
		protected SerializedProperty m_paddingFixedVerticalProp;

		protected virtual void OnEnable()
		{
			m_SpriteProp = serializedObject.FindProperty("m_Sprite");
			m_MaterialProp = serializedObject.FindProperty("m_Material");
			m_ColorProp = serializedObject.FindProperty("m_Color");
			m_RaycastTargetProp = serializedObject.FindProperty("m_RaycastTarget");
			m_RaycastPaddingProp = serializedObject.FindProperty("m_RaycastPadding");
			m_MaskableProp = serializedObject.FindProperty("m_Maskable");
			
			m_cornerSegmentsProp = serializedObject.FindProperty("m_cornerSegments");
			m_radiusProp = serializedObject.FindProperty("m_radius");
			m_frameSizeProp = serializedObject.FindProperty("m_frameSize");
			m_fadeSizeProp = serializedObject.FindProperty("m_fadeSize");
			m_invertMaskProp = serializedObject.FindProperty("m_invertMask");
			m_paddingProp = serializedObject.FindProperty("m_padding");
			m_paddingFixedHorizontalProp = serializedObject.FindProperty("m_paddingFixedHorizontal");
			m_paddingFixedVerticalProp = serializedObject.FindProperty("m_paddingFixedVertical");
		}

		public override void OnInspectorGUI()
		{
			GUILayout.Label("Image Properties", EditorStyles.boldLabel);
			EditorGUILayout.PropertyField(m_SpriteProp);
			EditorGUILayout.PropertyField(m_MaterialProp);
			EditorGUILayout.PropertyField(m_ColorProp);
			EditorGUILayout.PropertyField(m_RaycastTargetProp);
			EditorGUILayout.PropertyField(m_RaycastPaddingProp);
			EditorGUILayout.PropertyField(m_MaskableProp);
			GUILayout.Space(10);

			GUILayout.Label("Rounded Image Properties", EditorStyles.boldLabel);
			EditorGUILayout.PropertyField(m_cornerSegmentsProp);
			EditorGUILayout.PropertyField(m_radiusProp);
			EditorGUILayout.PropertyField(m_frameSizeProp);
			EditorGUILayout.PropertyField(m_fadeSizeProp);
			EditorGUILayout.PropertyField(m_invertMaskProp);
			EditorGUILayout.PropertyField(m_paddingProp);
			EditorGUILayout.PropertyField(m_paddingFixedHorizontalProp);
			EditorGUILayout.PropertyField(m_paddingFixedVerticalProp);
			
			
			serializedObject.ApplyModifiedProperties();

//			GUILayout.Space(100);
//			DrawDefaultInspector();
		}
	}
}