using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Editor
{
	[CustomEditor(typeof(UiRoundedImage))]
	public class UiRoundedImageEditor : UiShapeImageEditor
	{
		protected SerializedProperty m_cornerSegmentsProp;
		protected SerializedProperty m_radiusProp;

		protected override void OnEnable()
		{
			base.OnEnable();
			m_cornerSegmentsProp = serializedObject.FindProperty("m_cornerSegments");
			m_radiusProp = serializedObject.FindProperty("m_radius");
		}

		public override void OnInspectorGUI()
		{
			var thisUiRoundedImage = (UiRoundedImage) target;

			DrawImageProperties();
			GUILayout.Space(10);

			GUILayout.Label("Rounded Image Properties", EditorStyles.boldLabel);
			EditorGUILayout.PropertyField(m_cornerSegmentsProp);
			EditorGUILayout.PropertyField(m_radiusProp);
			DrawSharedShapeProperties(thisUiRoundedImage);

			GUILayout.Space(10);
			DrawSizeAndEnabledness();

			DrawSimpleGradientInline(thisUiRoundedImage);

			serializedObject.ApplyModifiedProperties();
		}
	}
}
