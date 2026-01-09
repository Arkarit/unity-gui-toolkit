#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Editor
{
	[CustomEditor(typeof(GuiToolkit.UiAutoLayoutElement))]
	public class UiAutoLayoutElementEditor : UnityEditor.Editor
	{
		public override void OnInspectorGUI()
		{
			EditorGUILayout.HelpBox(
				"Work in progress." +
				"Generic auto layout element. Currently only supports TMP vertical preferred height." +
				"Extended functionality is added only when backed by real use cases.", 
				MessageType.Info);
			DrawDefaultInspector();
		}
	}
}
#endif
