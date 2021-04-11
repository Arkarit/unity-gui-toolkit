using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.UI;
using UnityEngine;

namespace GuiToolkit
{
    [CustomEditor(typeof(UiHorizontalOrVerticalLayoutGroup), true)]
    [CanEditMultipleObjects]
    /// <summary>
    ///   Custom Editor for the HorizontalOrVerticalLayoutGroupEditor Component.
    ///   Extend this class to write a custom editor for an HorizontalOrVerticalLayoutGroupEditor-derived component.
    /// </summary>
    public class UiHorizontalOrVerticalLayoutGroupEditor : HorizontalOrVerticalLayoutGroupEditor
    {
        SerializedProperty m_vertical;

        protected override void OnEnable()
        {
			base.OnEnable();
            m_vertical = serializedObject.FindProperty("m_vertical");
        }

        public override void OnInspectorGUI()
        {
			base.OnInspectorGUI();
			serializedObject.Update();
			EditorGUILayout.PropertyField(m_vertical);
			serializedObject.ApplyModifiedProperties();
		}
	}
}