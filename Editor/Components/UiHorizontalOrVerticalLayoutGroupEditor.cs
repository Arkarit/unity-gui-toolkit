using UnityEditor;
using UnityEditor.UI;

namespace GuiToolkit.Editor
{
	[CustomEditor(typeof(UiHorizontalOrVerticalLayoutGroup), true)]
	[CanEditMultipleObjects]
	public class UiHorizontalOrVerticalLayoutGroupEditor : HorizontalOrVerticalLayoutGroupEditor
	{
		SerializedProperty m_verticalProp;
		SerializedProperty m_reverseOrderProp;

		protected override void OnEnable()
		{
			base.OnEnable();
			m_verticalProp = serializedObject.FindProperty("m_vertical");
			m_reverseOrderProp = serializedObject.FindProperty("m_reverseOrder");
		}

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();
			serializedObject.Update();
			EditorGUILayout.PropertyField(m_verticalProp);
			EditorGUILayout.PropertyField(m_reverseOrderProp);
			serializedObject.ApplyModifiedProperties();
		}
	}
}