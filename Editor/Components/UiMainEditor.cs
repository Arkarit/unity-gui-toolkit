using UnityEngine;
using UnityEditor;

namespace GuiToolkit.Editor
{
	[CustomEditor(typeof(UiMain))]
	public class UiMainEditor : UnityEditor.Editor
	{
		public override void OnInspectorGUI()
		{
			DrawDefaultInspector();
			var thisUiMain = (UiMain)target;

			GUILayout.BeginHorizontal();
			GUILayout.FlexibleSpace();
			if (GUILayout.Button("Clone Default Prefabs"))
			{

			}
			GUILayout.FlexibleSpace();
			GUILayout.EndHorizontal();
		}
	}
}
