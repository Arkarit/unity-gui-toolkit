using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Editor
{
	[CustomEditor(typeof(UiTextContainerDisableable))]
	public class UiTextContainerDisableableEditor : UiTextContainerEditor
	{
		protected SerializedProperty m_disabledBrightnessProp;
		protected SerializedProperty m_disabledDesaturationStrengthProp;
		protected SerializedProperty m_disabledAlphaProp;

		private static readonly HashSet<string> m_excludedProperties = new()
		{
			"m_disabledBrightness",
			"m_disabledDesaturationStrength",
			"m_disabledAlpha"
		};

		protected override HashSet<string> excludedProperties => m_excludedProperties;

		protected override void OnEnable()
		{
			base.OnEnable();
			m_disabledBrightnessProp = serializedObject.FindProperty("m_disabledBrightness");
			m_disabledDesaturationStrengthProp = serializedObject.FindProperty("m_disabledDesaturationStrength");
			m_disabledAlphaProp = serializedObject.FindProperty("m_disabledAlpha");
		}

		public override void OnInspectorGUI()
		{
			base.OnInspectorGUI();

			UiTextContainerDisableable thisUiTextContainerDisableable = (UiTextContainerDisableable)target;

			EditorGUI.BeginChangeCheck();

			EditorGUILayout.PropertyField(m_disabledAlphaProp);
			EditorGUILayout.PropertyField(m_disabledDesaturationStrengthProp);
			EditorGUILayout.PropertyField(m_disabledBrightnessProp);

			bool changed = EditorGUI.EndChangeCheck();

			serializedObject.ApplyModifiedProperties();

			thisUiTextContainerDisableable.SetColorMembersIfNecessary(changed);
		}
	}
}