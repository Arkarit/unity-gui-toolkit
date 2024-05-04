#if UNITY_EDITOR
using Codice.Client.BaseCommands.Config;
using System;
using System.Collections.Generic;
using System.IO;
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

		[Serializable]
		private class PropertyRecordsJson
		{
			public Type Type;
			public PropertyRecord[] Records;
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

			EditorGUILayout.BeginHorizontal();
			if (GUILayout.Button("Write JSON"))
				WriteJson();
			if (GUILayout.Button("Apply"))
				Apply();
			EditorGUILayout.EndHorizontal();
		}

		private void WriteJson()
		{
			var jsonClass = new PropertyRecordsJson();
			jsonClass.Type = m_MonoBehaviour.GetType();
			jsonClass.Records = m_PropertyRecords.ToArray();
			string path = UiToolkitConfiguration.Instance.GeneratedAssetsDir + $".{m_MonoBehaviour.GetType().FullName}.json";

			try
			{
				string json = JsonUtility.ToJson(jsonClass, true);
				File.WriteAllText(path, json);
			}
			catch (Exception e)
			{
				Debug.LogError($"Could not write Json, reason:'{e.Message}'");
			}
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