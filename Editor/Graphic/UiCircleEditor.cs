using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Editor
{
	[CustomEditor(typeof(UiCircle))]
	public class UiCircleEditor : UiShapeImageEditor
	{
		protected SerializedProperty m_segmentsProp;

		protected override void OnEnable()
		{
			base.OnEnable();
			m_segmentsProp = serializedObject.FindProperty("m_segments");
		}

		public override void OnInspectorGUI()
		{
			var thisUiCircle = (UiCircle) target;

			DrawImageProperties();
			GUILayout.Space(10);

			GUILayout.Label("Circle Properties", EditorStyles.boldLabel);
			EditorGUILayout.PropertyField(m_segmentsProp);
			DrawSharedShapeProperties(thisUiCircle);

			GUILayout.Space(10);
			DrawSizeAndEnabledness();

			DrawSimpleGradientInline(thisUiCircle);

			serializedObject.ApplyModifiedProperties();
		}
	}
}
