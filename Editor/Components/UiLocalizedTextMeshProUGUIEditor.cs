using TMPro.EditorUtilities;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Editor
{
	/// <summary>
	/// Custom Inspector for <see cref="UiLocalizedTextMeshProUGUI"/>.
	/// Draws a "Localization" section above the standard TMP UI inspector, exposing
	/// <see cref="UiLocalizedTextMeshProUGUI.Group"/> and <see cref="UiLocalizedTextMeshProUGUI.LocaKey"/>
	/// as editable fields. All TMP-native fields below are rendered by the base
	/// <see cref="TMP_EditorPanelUI"/> editor.
	/// </summary>
	[CustomEditor(typeof(UiLocalizedTextMeshProUGUI), true), CanEditMultipleObjects]
	public class UiLocalizedTextMeshProUGUIEditor : TMP_EditorPanelUI
	{
		private SerializedProperty m_groupProp;
		private SerializedProperty m_locaKeyProp;

		protected override void OnEnable()
		{
			base.OnEnable();
			m_groupProp = serializedObject.FindProperty("m_group");
			m_locaKeyProp = serializedObject.FindProperty("m_locaKey");
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			EditorGUILayout.LabelField("Localization", EditorStyles.boldLabel);

			EditorGUILayout.PropertyField(m_locaKeyProp, new GUIContent("Loca Key",
				"The localization key passed to LocaManager.Translate(). " +
				"Leave empty to initialize from the current TMP text at runtime. " +
				"Use [Text] placeholder form to mark as non-translatable."));

			EditorGUILayout.PropertyField(m_groupProp, new GUIContent("Group",
				"Optional localization group / namespace for the key."));

			serializedObject.ApplyModifiedProperties();

			EditorGUILayout.Space();
			EditorGUILayout.LabelField("Text Mesh Pro", EditorStyles.boldLabel);
			EditorGUILayout.Space(2);

			base.OnInspectorGUI();
		}
	}
}
