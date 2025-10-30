using UnityEditor;

namespace GuiToolkit.Editor
{
	[CustomEditor(typeof(UiSimpleChildrenAnimation))]
	public class UiSimpleChildrenAnimationEditor : UiSimpleAnimationBaseEditor
	{
		private SerializedProperty m_delayPerChildProp;
		private SerializedProperty m_baseDurationPerChildProp;
		private SerializedProperty m_durationPerChildProp;
		private SerializedProperty m_autoCollectChildrenProp;
		private SerializedProperty m_childAnimationsProp;
		
		public override bool DisplayDurationProp => false;
		public override bool DisplaySlaveAnimations => false;

		public override void OnEnable()
		{
			base.OnEnable();
			m_delayPerChildProp = serializedObject.FindProperty("m_delayPerChild");
			m_baseDurationPerChildProp = serializedObject.FindProperty("m_baseDurationPerChild");
			m_durationPerChildProp = serializedObject.FindProperty("m_durationPerChild");
			m_autoCollectChildrenProp = serializedObject.FindProperty("m_autoCollectChildren");
			m_childAnimationsProp = serializedObject.FindProperty("m_childAnimations");
		}

		public override void EditSubClass()
		{
			base.EditSubClass();
			EditorGUILayout.PropertyField(m_delayPerChildProp);
			EditorGUILayout.PropertyField(m_baseDurationPerChildProp);
			EditorGUILayout.PropertyField(m_durationPerChildProp);
			EditorGUILayout.PropertyField(m_autoCollectChildrenProp);
			if (m_autoCollectChildrenProp.boolValue)
				m_childAnimationsProp.arraySize = 0;
			else
				EditorGUILayout.PropertyField(m_childAnimationsProp);
		}
	}
}
