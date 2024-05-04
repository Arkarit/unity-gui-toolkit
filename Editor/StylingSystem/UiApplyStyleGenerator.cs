#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit
{
	public class UiApplyStyleGenerator : EditorWindow
	{
		private MonoBehaviour m_MonoBehaviour;
		private readonly List<PropertyRecord> m_PropertyRecords = new();
		private Vector2 m_ScrollPos;

		[Serializable]
		private class PropertyRecord
		{
			public bool Used;
			public string Name;
			public Type Type;
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
			var propertyInfos = m_MonoBehaviour.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);
			foreach (var propertyInfo in propertyInfos)
			{
				if (!propertyInfo.CanRead || !propertyInfo.CanWrite)
					continue;

				m_PropertyRecords.Add(new PropertyRecord()
				{
					Used = true,
					Name = propertyInfo.Name,
					Type = propertyInfo.PropertyType
				});
			}
		}

		private void DrawProperties()
		{
			m_ScrollPos = EditorGUILayout.BeginScrollView(m_ScrollPos);
			foreach (var propertyRecord in m_PropertyRecords)
				DrawProperty(propertyRecord);
			EditorGUILayout.EndScrollView();
		}

		private void DrawProperty(PropertyRecord propertyRecord)
		{
			EditorGUILayout.BeginHorizontal();
			propertyRecord.Used = GUILayout.Toggle(propertyRecord.Used, "");
			EditorGUILayout.LabelField($"{propertyRecord.Name} ({propertyRecord.Type.Name})");
			EditorGUILayout.EndHorizontal();
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