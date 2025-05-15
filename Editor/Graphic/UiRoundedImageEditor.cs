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
		protected SerializedProperty m_usePaddingProp;
		protected SerializedProperty m_paddingProp;
		protected SerializedProperty m_useFixedSizeProp;
		protected SerializedProperty m_fixedSizeProp;
		protected SerializedProperty m_disabledMaterialProp;
		protected SerializedProperty m_enabledInHierarchyProp;
		protected SerializedProperty m_gradientSimpleProp;

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
			m_usePaddingProp =serializedObject.FindProperty("m_usePadding");
			m_paddingProp = serializedObject.FindProperty("m_padding");
			m_useFixedSizeProp = serializedObject.FindProperty("m_useFixedSize");
			m_fixedSizeProp = serializedObject.FindProperty("m_fixedSize");
			m_disabledMaterialProp = serializedObject.FindProperty("m_disabledMaterial");
			m_enabledInHierarchyProp = serializedObject.FindProperty("m_enabledInHierarchy");
			m_gradientSimpleProp = serializedObject.FindProperty("m_gradientSimple");
		}

		public override void OnInspectorGUI()
		{
			var thisUiRoundedImage = (UiRoundedImage) target;
			
			GUILayout.Label("Image Properties", EditorStyles.boldLabel);
			EditorGUILayout.PropertyField(m_SpriteProp);
			
			EditorGUILayout.PropertyField(m_MaterialProp);
			EditorGUILayout.PropertyField(m_disabledMaterialProp); // This is actually not an Image member, but it does not make sense to display it elsewhere
			
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
			EditorGUILayout.PropertyField(m_gradientSimpleProp);
			
			using (new EditorGUI.DisabledScope(!thisUiRoundedImage.maskable))
				EditorGUILayout.PropertyField(m_invertMaskProp);
			
			GUILayout.Space(10);

			GUILayout.Label("Size", EditorStyles.boldLabel);
			EditorUiUtility.DisplayPropertyConditionally(m_useFixedSizeProp, m_fixedSizeProp);
			EditorUiUtility.DisplayPropertyConditionally(m_usePaddingProp, m_paddingProp);
			GUILayout.Space(10);
			
			GUILayout.Label("Visual Enabledness", EditorStyles.boldLabel);
			EditorGUILayout.PropertyField(m_enabledInHierarchyProp);
			
			if (m_gradientSimpleProp.objectReferenceValue != null)
			{
				UiGradientSimple gradientSimple = (UiGradientSimple) m_gradientSimpleProp.objectReferenceValue;
				var colors = gradientSimple.GetColors();
				Color newColorLeftOrTop = EditorGUILayout.ColorField("Color left or top:", colors.leftOrTop);
				Color newColorRightOrBottom = EditorGUILayout.ColorField("Color right or bottom:", colors.rightOrBottom);
				if (newColorLeftOrTop != colors.leftOrTop || newColorRightOrBottom != colors.rightOrBottom)
				{
					Undo.RecordObject(gradientSimple, "Simple gradient colors change");
					thisUiRoundedImage.SetSimpleGradientColors(newColorLeftOrTop, newColorRightOrBottom);
				}
			}
			
			serializedObject.ApplyModifiedProperties();

//			GUILayout.Space(100);
//			DrawDefaultInspector();
		}
	}
}