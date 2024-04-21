using UnityEditor;

namespace GuiToolkit.Editor
{
    [CustomEditor(typeof(UiGridLayoutGroup), true)]
    [CanEditMultipleObjects]
    /// <summary>
    ///   Custom Editor for the UiGridLayout Component.
    ///   Extend this class to write a custom editor for an GridLayout-derived component.
    /// </summary>
    public class UiGridLayoutGroupEditor : UnityEditor.Editor
    {
        SerializedProperty m_paddingProp;
        SerializedProperty m_cellSizeProp;
        SerializedProperty m_spacingProp;
        SerializedProperty m_startCornerProp;
        SerializedProperty m_startAxisProp;
        SerializedProperty m_childAlignmentProp;
        SerializedProperty m_constraintProp;
        SerializedProperty m_constraintCountProp;
		SerializedProperty m_centerPartialFilledProp;

        protected virtual void OnEnable()
        {
            m_paddingProp = serializedObject.FindProperty("m_Padding");
            m_cellSizeProp = serializedObject.FindProperty("m_cellSize");
            m_spacingProp = serializedObject.FindProperty("m_spacing");
            m_startCornerProp = serializedObject.FindProperty("m_startCorner");
            m_startAxisProp = serializedObject.FindProperty("m_startAxis");
            m_childAlignmentProp = serializedObject.FindProperty("m_ChildAlignment");
            m_constraintProp = serializedObject.FindProperty("m_constraint");
            m_constraintCountProp = serializedObject.FindProperty("m_constraintCount");
			m_centerPartialFilledProp = serializedObject.FindProperty("m_centerPartialFilled");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            EditorGUILayout.PropertyField(m_paddingProp, true);
            EditorGUILayout.PropertyField(m_cellSizeProp, true);
            EditorGUILayout.PropertyField(m_spacingProp, true);
            EditorGUILayout.PropertyField(m_startCornerProp, true);
            EditorGUILayout.PropertyField(m_startAxisProp, true);
            EditorGUILayout.PropertyField(m_childAlignmentProp, true);
            EditorGUILayout.PropertyField(m_constraintProp, true);
            EditorGUILayout.PropertyField(m_centerPartialFilledProp, true);
            if (m_constraintProp.enumValueIndex > 0)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_constraintCountProp, true);
                EditorGUI.indentLevel--;
            }
            serializedObject.ApplyModifiedProperties();
        }
    }
}
