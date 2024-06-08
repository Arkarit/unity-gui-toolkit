using GuiToolkit.Style;
using UnityEditor;

namespace GuiToolkit.Editor
{
	[CustomEditor(typeof(UiStyleConfig), true)]
	public class UiStyleConfigEditor : UnityEditor.Editor
	{
		private SerializedProperty m_stylesProp;
		private UiStyleConfig m_thisUiStyleConfig;

		protected virtual void OnEnable()
		{
			m_thisUiStyleConfig = (UiStyleConfig)target;
			m_stylesProp = serializedObject.FindProperty("m_styles");
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			int idx = EditorUiUtility.StringPopup
			(
				"Type", 
				m_thisUiStyleConfig.Styles, 
				m_thisUiStyleConfig.CurrentStyle, 
				out string newSelection,
				null,
				true,
				"Add new style",
				"Add new style"
			);

			serializedObject.ApplyModifiedProperties();
		}
	}
}
