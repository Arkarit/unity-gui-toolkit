#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit
{
	public class UiApplyStyleGenerator : EditorWindow
	{
		private MonoBehaviour m_MonoBehaviour;
		private readonly List<PropertyRecord> m_PropertyRecords = new();

		private class PropertyRecord
		{

		}

		private void OnGUI()
		{
			if (m_MonoBehaviour == null)
			{
				EditorGUILayout.HelpBox("Drag a mono behaviour into this field to generate", MessageType.Info);
			}

			m_MonoBehaviour = EditorGUILayout.ObjectField(m_MonoBehaviour, typeof(MonoBehaviour), true) as MonoBehaviour;

			if (!m_MonoBehaviour)
				return;

			CollectProperties();
			DrawProperties();
			if (GUILayout.Button("Apply"))
				Apply();
		}

		private void CollectProperties()
		{
			m_PropertyRecords.Clear();
		}

		private void DrawProperties()
		{
		}

		private void Apply()
		{
		}

		[MenuItem(StringConstants.APPLY_STYLE_GENERATOR_MENU_NAME, priority = Constants.STYLES_HEADER_PRIORITY)]
		public static UiApplyStyleGenerator GetWindow()
		{
			var window = GetWindow<UiApplyStyleGenerator>();
			window.titleContent = new GUIContent("'Ui Apply Style' Generator");
			window.Focus();
			window.Repaint();
			return window;
		}
	}
}
#endif