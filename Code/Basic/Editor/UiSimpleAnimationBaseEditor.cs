using UnityEditor;
using UnityEngine;

namespace GuiToolkit
{

	[CustomEditor(typeof(UiSimpleAnimationBase))]
	public class UiSimpleAnimationBaseEditor : Editor, IEditorUpdateable
	{
		protected SerializedProperty m_durationProp;
		protected SerializedProperty m_delayProp;
		protected SerializedProperty m_autoStartProp;
		protected SerializedProperty m_setOnStartProp;
		protected SerializedProperty m_numberOfLoopsProp;
		protected SerializedProperty m_slaveAnimationsProp;

		public virtual void OnEnable()
		{
			m_durationProp = serializedObject.FindProperty("m_duration");
			m_delayProp = serializedObject.FindProperty("m_delay");
			m_autoStartProp = serializedObject.FindProperty("m_autoStart");
			m_setOnStartProp = serializedObject.FindProperty("m_setOnStart");
			m_numberOfLoopsProp = serializedObject.FindProperty("m_numberOfLoops");
			m_slaveAnimationsProp = serializedObject.FindProperty("m_slaveAnimations");
		}

		public virtual void EditSubClass() { }

		public override void OnInspectorGUI()
		{
			UiSimpleAnimationBase thisUiSimpleAnimationBase = (UiSimpleAnimationBase)target;

			EditSubClass();

			GUILayout.Label("Timing:", EditorStyles.boldLabel);
			EditorGUILayout.PropertyField(m_durationProp);
			EditorGUILayout.PropertyField(m_delayProp);
			EditorGUILayout.PropertyField(m_numberOfLoopsProp);
			EditorGUILayout.PropertyField(m_autoStartProp);
			EditorGUI.BeginDisabledGroup(m_autoStartProp.boolValue);
			EditorGUILayout.PropertyField(m_setOnStartProp);
			EditorGUI.EndDisabledGroup();
			EditorGUILayout.Space();

			GUILayout.Label("Slave animations:", EditorStyles.boldLabel);
			EditorGUILayout.PropertyField(m_slaveAnimationsProp, true);

			serializedObject.ApplyModifiedProperties();

			#if DEBUG_SIMPLE_ANIMATION
				EditorGUILayout.Space(50);
				GUILayout.Label("Default Inspector:", EditorStyles.boldLabel);
				DrawDefaultInspector();
				serializedObject.ApplyModifiedProperties();
			#endif

		}

		public void UpdateInEditor( float _deltaTime )
		{
		}

		public bool RemoveFromEditorUpdate()
		{
			return true;
		}
	}
}