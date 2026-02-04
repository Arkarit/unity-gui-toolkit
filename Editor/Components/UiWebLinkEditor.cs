using UnityEditor;

namespace GuiToolkit.Editor
{
	[CustomEditor(typeof(UiWebLink), true)]
	public class UiWebLinkEditor : UnityEditor.Editor
	{
		private SerializedProperty m_linksProp;
		private SerializedProperty m_isLocalizedProp;
		private SerializedProperty m_linkProp;

		private void OnEnable()
		{
			m_linksProp = serializedObject.FindProperty("m_links");
			m_isLocalizedProp = serializedObject.FindProperty("m_isLocalized");
			m_linkProp = serializedObject.FindProperty("m_link");
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();
			DrawPropertiesExcluding(serializedObject, "m_links", "m_isLocalized", "m_link");
			EditorGUILayout.PropertyField(m_isLocalizedProp);

			if (m_isLocalizedProp.boolValue)
				EditorGUILayout.PropertyField(m_linksProp, true);
			else
				EditorGUILayout.PropertyField(m_linkProp);

			serializedObject.ApplyModifiedProperties();
		}
	}
}
