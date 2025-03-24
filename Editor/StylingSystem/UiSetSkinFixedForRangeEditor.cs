using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Style.Editor
{
	[CustomEditor(typeof(UiSetSkinFixedForRange), true)]
	public class UiSetSkinFixedForRangeEditor : UnityEditor.Editor
	{
		private SerializedProperty m_optionalStyleConfigProp;
		private SerializedProperty m_styleAppliersProp;
		private SerializedProperty m_fixedSkinNameProp;
		private SerializedProperty m_applierListFixedProp;
		private SerializedProperty m_handleDynamicallyAddedAppliersProp;
		private static bool m_showDebugData;
		
		protected virtual void OnEnable()
		{
			m_optionalStyleConfigProp = serializedObject.FindProperty("m_optionalStyleConfig");
			m_styleAppliersProp = serializedObject.FindProperty("m_styleAppliers");
			m_fixedSkinNameProp = serializedObject.FindProperty("m_fixedSkinName");
			m_applierListFixedProp = serializedObject.FindProperty("m_applierListFixed");
			m_handleDynamicallyAddedAppliersProp = serializedObject.FindProperty("m_handleDynamicallyAddedAppliers");
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();
			
			var m_thisSetSkinFixedForRange = (UiSetSkinFixedForRange) target;
			
			EditorGUILayout.PropertyField(m_optionalStyleConfigProp);
			
			EditorGUILayout.BeginHorizontal();
			EditorGUILayout.PropertyField(m_applierListFixedProp);
			if (m_applierListFixedProp.boolValue && GUILayout.Button("Collect"))
			{
				var styleAppliers = m_thisSetSkinFixedForRange.GetComponentsInChildren<UiAbstractApplyStyleBase>();
				m_styleAppliersProp.arraySize = styleAppliers.Length;
				for (int i = 0; i<styleAppliers.Length; i++)
					m_styleAppliersProp.GetArrayElementAtIndex(i).objectReferenceValue = styleAppliers[i];
			}
			EditorGUILayout.EndHorizontal();
			
			if (!m_applierListFixedProp.boolValue)
			{
				EditorGUILayout.PropertyField(m_handleDynamicallyAddedAppliersProp);
			}
			else
			{
				m_handleDynamicallyAddedAppliersProp.boolValue = false;
				EditorGUILayout.PropertyField(m_styleAppliersProp);
			}
			
			m_thisSetSkinFixedForRange.FixedSkinName = UiStyleEditorUtility.GetSelectSkinPopup(m_thisSetSkinFixedForRange.StyleConfig, m_thisSetSkinFixedForRange.FixedSkinName, out bool _);

			serializedObject.ApplyModifiedProperties();
			
			m_showDebugData = EditorGUILayout.Toggle("Show debug data", m_showDebugData);
			if (m_showDebugData)
			{
				EditorGUILayout.Space(10);
				DrawDefaultInspector();
			}
		}

	}
}
