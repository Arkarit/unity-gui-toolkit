using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Editor
{
	[CustomEditor(typeof(UiAbstractPerCameraSettings), true)]
	[CanEditMultipleObjects]
	public class UiAbstractPerCameraSettingsEditor : UnityEditor.Editor
	{
		public override void OnInspectorGUI()
		{
			DrawDefaultInspector();

			EditorGUILayout.Space();
			if (GUILayout.Button(new GUIContent("Capture Current Values",
				    "Copy the current global values (RenderSettings, QualitySettings, ...) into all entries, enabled or not")))
			{
				foreach (var t in targets)
				{
					var settings = (UiAbstractPerCameraSettings)t;
					Undo.RecordObject(settings, "Capture Current Values");
					settings.CaptureCurrentValues();
					EditorUtility.SetDirty(settings);
				}
			}
		}
	}
}
