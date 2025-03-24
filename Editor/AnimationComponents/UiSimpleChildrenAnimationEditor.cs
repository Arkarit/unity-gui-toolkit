using UnityEditor;

namespace GuiToolkit.Editor
{
	[CustomEditor(typeof(UiSimpleChildrenAnimation))]
	public class UiSimpleChildrenAnimationEditor : UiSimpleAnimationBaseEditor
	{
		private SerializedProperty m_delayPerChildProp;
		private SerializedProperty m_autoCollectChildrenProp;
		
		public override bool DisplayDurationProp => false;
		public override bool DisplaySlaveAnimations => false;

		public override void OnEnable()
		{
			base.OnEnable();
			m_delayPerChildProp = serializedObject.FindProperty("m_delayPerChild");
			m_autoCollectChildrenProp = serializedObject.FindProperty("m_autoCollectChildren");
		}

		public override void EditSubClass()
		{
			base.EditSubClass();
			EditorGUILayout.PropertyField(m_delayPerChildProp);
			EditorGUILayout.PropertyField(m_autoCollectChildrenProp);
		}
	}
}
