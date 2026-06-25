using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Editor
{
	[CustomEditor(typeof(UiInitialActiveState))]
	public class UiInitialActiveStateEditor : UnityEditor.Editor
	{
		private SerializedProperty m_stateProp;
		private SerializedProperty m_targetProp;
		private SerializedProperty m_objectsProp;
		private SerializedProperty m_addToStartupOverlayProp;

		private void OnEnable()
		{
			m_stateProp                = serializedObject.FindProperty("m_state");
			m_targetProp               = serializedObject.FindProperty("m_target");
			m_objectsProp              = serializedObject.FindProperty("m_objects");
			m_addToStartupOverlayProp  = serializedObject.FindProperty("m_addToStartupOverlay");
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			EditorGUILayout.PropertyField(m_stateProp);
			EditorGUILayout.PropertyField(m_targetProp);

			// Only show the object list when Target is List.
			if (m_targetProp.enumValueIndex == (int)EInitialActiveTarget.List)
				EditorGUILayout.PropertyField(m_objectsProp, true);

			// The queue-add flag only makes sense when activating targets — show it greyed
			// out otherwise so it's still discoverable.
			bool isActive = m_stateProp.enumValueIndex == (int)EInitialActiveState.Active;
			using (new EditorGUI.DisabledScope(!isActive))
			{
				EditorGUILayout.PropertyField(m_addToStartupOverlayProp);
			}

			serializedObject.ApplyModifiedProperties();
		}
	}
}
