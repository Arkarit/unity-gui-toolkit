#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Editor
{
	[CustomEditor(typeof(GuiToolkit.UiAutoLayoutElement))]
	public class UiAutoLayoutElementEditor : UnityEditor.Editor
	{
		private SerializedProperty m_layoutPriority;

		private SerializedProperty m_sourceRect;
		private SerializedProperty m_tmpText;

		private SerializedProperty m_widthSource;
		private SerializedProperty m_heightSource;

		private SerializedProperty m_useManualMinWidth;
		private SerializedProperty m_manualMinWidth;
		private SerializedProperty m_useManualPreferredWidth;
		private SerializedProperty m_manualPreferredWidth;
		private SerializedProperty m_useManualFlexibleWidth;
		private SerializedProperty m_manualFlexibleWidth;

		private SerializedProperty m_useManualMinHeight;
		private SerializedProperty m_manualMinHeight;
		private SerializedProperty m_useManualPreferredHeight;
		private SerializedProperty m_manualPreferredHeight;
		private SerializedProperty m_useManualFlexibleHeight;
		private SerializedProperty m_manualFlexibleHeight;

		private SerializedProperty m_paddingLeft;
		private SerializedProperty m_paddingRight;
		private SerializedProperty m_paddingTop;
		private SerializedProperty m_paddingBottom;

		private void OnEnable()
		{
			m_layoutPriority = serializedObject.FindProperty("m_layoutPriority");

			m_sourceRect = serializedObject.FindProperty("m_sourceRect");
			m_tmpText = serializedObject.FindProperty("m_tmpText");

			m_widthSource = serializedObject.FindProperty("m_widthSource");
			m_heightSource = serializedObject.FindProperty("m_heightSource");

			m_useManualMinWidth = serializedObject.FindProperty("m_useManualMinWidth");
			m_manualMinWidth = serializedObject.FindProperty("m_manualMinWidth");
			m_useManualPreferredWidth = serializedObject.FindProperty("m_useManualPreferredWidth");
			m_manualPreferredWidth = serializedObject.FindProperty("m_manualPreferredWidth");
			m_useManualFlexibleWidth = serializedObject.FindProperty("m_useManualFlexibleWidth");
			m_manualFlexibleWidth = serializedObject.FindProperty("m_manualFlexibleWidth");

			m_useManualMinHeight = serializedObject.FindProperty("m_useManualMinHeight");
			m_manualMinHeight = serializedObject.FindProperty("m_manualMinHeight");
			m_useManualPreferredHeight = serializedObject.FindProperty("m_useManualPreferredHeight");
			m_manualPreferredHeight = serializedObject.FindProperty("m_manualPreferredHeight");
			m_useManualFlexibleHeight = serializedObject.FindProperty("m_useManualFlexibleHeight");
			m_manualFlexibleHeight = serializedObject.FindProperty("m_manualFlexibleHeight");

			m_paddingLeft = serializedObject.FindProperty("m_paddingLeft");
			m_paddingRight = serializedObject.FindProperty("m_paddingRight");
			m_paddingTop = serializedObject.FindProperty("m_paddingTop");
			m_paddingBottom = serializedObject.FindProperty("m_paddingBottom");
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			EditorGUILayout.PropertyField(m_layoutPriority);

			EditorGUILayout.Space(6f);
			EditorGUILayout.LabelField("Sources", EditorStyles.boldLabel);
			EditorGUILayout.PropertyField(m_sourceRect, new GUIContent("Source Rect"));
			EditorGUILayout.PropertyField(m_tmpText, new GUIContent("TMP Text"));
			EditorGUILayout.PropertyField(m_widthSource, new GUIContent("Width Source"));
			EditorGUILayout.PropertyField(m_heightSource, new GUIContent("Height Source"));

			EditorGUILayout.Space(6f);
			EditorGUILayout.LabelField("Manual", EditorStyles.boldLabel);

			DrawManualRow("Min Width", m_useManualMinWidth, m_manualMinWidth);
			DrawManualRow("Preferred Width", m_useManualPreferredWidth, m_manualPreferredWidth);
			DrawManualRow("Flexible Width", m_useManualFlexibleWidth, m_manualFlexibleWidth);

			EditorGUILayout.Space(4f);

			DrawManualRow("Min Height", m_useManualMinHeight, m_manualMinHeight);
			DrawManualRow("Preferred Height", m_useManualPreferredHeight, m_manualPreferredHeight);
			DrawManualRow("Flexible Height", m_useManualFlexibleHeight, m_manualFlexibleHeight);

			EditorGUILayout.Space(6f);
			EditorGUILayout.LabelField("Padding", EditorStyles.boldLabel);

			EditorGUILayout.PropertyField(m_paddingLeft, new GUIContent("Left"));
			EditorGUILayout.PropertyField(m_paddingRight, new GUIContent("Right"));
			EditorGUILayout.PropertyField(m_paddingTop, new GUIContent("Top"));
			EditorGUILayout.PropertyField(m_paddingBottom, new GUIContent("Bottom"));

			serializedObject.ApplyModifiedProperties();
		}

		private static void DrawManualRow( string _label, SerializedProperty _toggle, SerializedProperty _value )
		{
			Rect rect = EditorGUILayout.GetControlRect();
			Rect left = rect;
			left.width = 18f;

			Rect middle = rect;
			middle.xMin = left.xMax;
			middle.xMax = rect.xMax - 80f;

			Rect right = rect;
			right.xMin = rect.xMax - 76f;

			_toggle.boolValue = EditorGUI.Toggle(left, _toggle.boolValue);
			EditorGUI.LabelField(middle, _label);

			using (new EditorGUI.DisabledScope(!_toggle.boolValue))
			{
				_value.floatValue = EditorGUI.FloatField(right, _value.floatValue);
			}
		}
	}
}
#endif
