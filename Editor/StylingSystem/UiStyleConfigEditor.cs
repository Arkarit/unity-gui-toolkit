using GuiToolkit.Style;
using UnityEditor;

namespace GuiToolkit.Editor
{
	[CustomEditor(typeof(UiStyleConfig), true)]
	public class UiStyleConfigEditor : UnityEditor.Editor
	{
		protected virtual void OnEnable()
		{
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();
			DrawDefaultInspector();
		}
	}
}
