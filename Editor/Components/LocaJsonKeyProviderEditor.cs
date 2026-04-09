using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Editor
{
	/// <summary>
	/// Custom inspector for <see cref="LocaJsonKeyProvider"/> ScriptableObjects.
	/// Renders the default fields and adds a "Preview Keys" button that shows all keys that would
	/// be harvested from the configured JSON files.
	/// </summary>
	[CustomEditor(typeof(LocaJsonKeyProvider), true)]
	public class LocaJsonKeyProviderEditor : UnityEditor.Editor
	{
		private bool m_showPreview;
		private List<string> m_previewKeys;

		public override void OnInspectorGUI()
		{
			DrawDefaultInspector();

			EditorGUILayout.Space();

			if (GUILayout.Button("Preview Harvested Keys"))
			{
				var provider = (LocaJsonKeyProvider) target;
				m_previewKeys = provider.LocaKeys;
				m_showPreview = true;
			}

			if (!m_showPreview || m_previewKeys == null)
				return;

			EditorGUILayout.Space();
			EditorGUILayout.LabelField($"Harvested Keys ({m_previewKeys.Count})", EditorStyles.boldLabel);

			if (m_previewKeys.Count == 0)
			{
				EditorGUILayout.HelpBox("No keys found. Check that the JSON file is assigned and the field names are correct.", MessageType.Warning);
				return;
			}

			foreach (var key in m_previewKeys)
				EditorGUILayout.LabelField(key, EditorStyles.wordWrappedLabel);
		}
	}
}
