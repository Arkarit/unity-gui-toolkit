using UnityEditor;

namespace GuiToolkit.Editor
{
	/// <summary>
	/// Inspector for <see cref="UiSoundOverride"/> that shows only the member relevant
	/// to the selected mode: nothing extra for Suppress, the redirect type for Redirect,
	/// and the custom sound (drawn via <see cref="UiSoundDefDrawer"/>) for Custom.
	/// </summary>
	[CustomEditor(typeof(UiSoundOverride), true)]
	public class UiSoundOverrideEditor : UnityEditor.Editor
	{
		private SerializedProperty m_modeProp;
		private SerializedProperty m_redirectTypeProp;
		private SerializedProperty m_customSoundProp;

		private void OnEnable()
		{
			m_modeProp         = serializedObject.FindProperty("m_mode");
			m_redirectTypeProp = serializedObject.FindProperty("m_redirectType");
			m_customSoundProp  = serializedObject.FindProperty("m_customSound");
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			EditorGUILayout.PropertyField(m_modeProp);

			switch ((UiSoundOverride.EMode) m_modeProp.enumValueIndex)
			{
				case UiSoundOverride.EMode.Redirect:
					EditorGUILayout.PropertyField(m_redirectTypeProp);
					break;
				case UiSoundOverride.EMode.Custom:
					EditorGUILayout.PropertyField(m_customSoundProp, true);
					break;
			}

			serializedObject.ApplyModifiedProperties();
		}
	}
}
