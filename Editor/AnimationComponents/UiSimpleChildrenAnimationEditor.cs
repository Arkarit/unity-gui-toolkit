using UnityEditor;

namespace GuiToolkit.Editor
{
	[CustomEditor(typeof(UiSimpleChildrenAnimation))]
	public class UiSimpleChildrenAnimationEditor : UiSimpleAnimationBaseEditor
	{
		private SerializedProperty m_delayPerChildProp;
		public override bool DisplayDurationProp => false;
		public override bool DisplaySlaveAnimations => false;

		public override void OnEnable()
		{
			base.OnEnable();
			m_delayPerChildProp = serializedObject.FindProperty("m_delayPerChild");
		}

		public override void EditSubClass()
		{
			base.EditSubClass();
			EditorGUILayout.PropertyField(m_delayPerChildProp);
		}
	}
}
