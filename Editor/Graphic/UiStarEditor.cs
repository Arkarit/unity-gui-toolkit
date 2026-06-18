using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Editor
{
	[CustomEditor(typeof(UiStar))]
	public class UiStarEditor : UiShapeImageEditor
	{
		protected SerializedProperty m_spikeCountProp;
		protected SerializedProperty m_innerRadiusRatioProp;
		protected SerializedProperty m_rotationProp;
		protected SerializedProperty m_insetStrategyProp;
		protected SerializedProperty m_miterLimitProp;

		protected override void OnEnable()
		{
			base.OnEnable();
			m_spikeCountProp = serializedObject.FindProperty("m_spikeCount");
			m_innerRadiusRatioProp = serializedObject.FindProperty("m_innerRadiusRatio");
			m_rotationProp = serializedObject.FindProperty("m_rotation");
			m_insetStrategyProp = serializedObject.FindProperty("m_insetStrategy");
			m_miterLimitProp = serializedObject.FindProperty("m_miterLimit");
		}

		public override void OnInspectorGUI()
		{
			var thisUiStar = (UiStar) target;

			DrawImageProperties();
			GUILayout.Space(10);

			GUILayout.Label("Star Properties", EditorStyles.boldLabel);
			EditorGUILayout.PropertyField(m_spikeCountProp);
			EditorGUILayout.PropertyField(m_innerRadiusRatioProp);
			EditorGUILayout.PropertyField(m_rotationProp);
			EditorGUILayout.PropertyField(m_insetStrategyProp);

			using (new EditorGUI.DisabledScope(m_insetStrategyProp.enumValueIndex != (int)UiStar.InsetStrategy.Miter))
				EditorGUILayout.PropertyField(m_miterLimitProp);

			DrawSharedShapeProperties(thisUiStar);

			GUILayout.Space(10);
			DrawSizeAndEnabledness();

			DrawSimpleGradientInline(thisUiStar);

			serializedObject.ApplyModifiedProperties();
		}
	}
}
