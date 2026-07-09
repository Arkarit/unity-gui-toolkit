using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace GuiToolkit.Editor
{
	/// <summary>
	/// Inspector for <see cref="UiSoundConfig"/>. Draws the entries as a
	/// <see cref="ReorderableList"/> whose add button seeds a fresh
	/// <see cref="UiSoundConfig.Entry"/> from the class defaults — Unity otherwise
	/// zero-initializes a newly added serialized array element (ignoring the C# field
	/// initializers), which would make every new entry silent (Volume/Pitch 0) and
	/// unpickable (Weight 0).
	/// </summary>
	[CustomEditor(typeof(UiSoundConfig))]
	public class UiSoundConfigEditor : UnityEditor.Editor
	{
		private SerializedProperty m_masterVolumeProp;
		private SerializedProperty m_entriesProp;
		private SerializedProperty m_debugLogProp;
		private ReorderableList m_list;

		private void OnEnable()
		{
			m_masterVolumeProp = serializedObject.FindProperty("m_masterVolume");
			m_entriesProp      = serializedObject.FindProperty("m_entries");
			m_debugLogProp     = serializedObject.FindProperty("m_debugLog");

			m_list = new ReorderableList(serializedObject, m_entriesProp, true, true, true, true);
			m_list.drawHeaderCallback = _rect => EditorGUI.LabelField(_rect, "Entries");

			m_list.elementHeightCallback = _index =>
				EditorGUI.GetPropertyHeight(m_entriesProp.GetArrayElementAtIndex(_index), true) + 4f;

			m_list.drawElementCallback = ( _rect, _index, _active, _focused ) =>
			{
				var element = m_entriesProp.GetArrayElementAtIndex(_index);
				_rect.y += 2f;
				_rect.height = EditorGUI.GetPropertyHeight(element, true);
				EditorGUI.PropertyField(_rect, element, true);
			};

			// Seed new entries from the class defaults (Volume/Pitch/Weight = 1) instead
			// of Unity's zero-initialized element. boxedValue keeps this DRY — the field
			// initializers in Entry/SoundDef remain the single source of truth.
			m_list.onAddCallback = _list =>
			{
				int index = m_entriesProp.arraySize;
				m_entriesProp.arraySize++;
				m_entriesProp.GetArrayElementAtIndex(index).boxedValue = new UiSoundConfig.Entry();
			};
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			EditorGUILayout.PropertyField(m_masterVolumeProp);
			EditorGUILayout.Space();
			m_list.DoLayoutList();
			EditorGUILayout.Space();
			EditorGUILayout.PropertyField(m_debugLogProp);

			serializedObject.ApplyModifiedProperties();
		}
	}
}
