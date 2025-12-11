using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using static GuiToolkit.LocaExcelBridge;

namespace GuiToolkit.Editor
{
	[CustomEditor(typeof(LocaExcelBridge), true)]
	public class LocaExcelBridgeEditor : UnityEditor.Editor
	{
		private SerializedProperty m_sourceType;
		private SerializedProperty m_excelPath;
		private SerializedProperty m_googleUrl;
		private SerializedProperty m_group;
		private SerializedProperty m_columnDescriptions;
		private SerializedProperty m_startRow;
		private SerializedProperty m_processedLoca;

		protected void OnEnable()
		{
			m_sourceType = serializedObject.FindProperty("m_sourceType");
			m_excelPath = serializedObject.FindProperty("m_excelPath");
			m_googleUrl = serializedObject.FindProperty("m_googleUrl");
			m_group = serializedObject.FindProperty("m_group");
			m_columnDescriptions = serializedObject.FindProperty("m_columnDescriptions");
			m_startRow = serializedObject.FindProperty("m_startRow");
			m_processedLoca = serializedObject.FindProperty("m_processedLoca");
		}

		public override void OnInspectorGUI()
		{
			var thisLocaProvider = (ILocaProvider)target;
			serializedObject.Update();
			EditorGUILayout.PropertyField(m_sourceType);
			var sourceType = (SourceType) m_sourceType.intValue;

			switch(sourceType)
			{
				case SourceType.Local:
					EditorGUILayout.PropertyField(m_excelPath);
					break;
				case SourceType.GoogleDocs:
					EditorGUILayout.PropertyField(m_googleUrl);
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			EditorGUILayout.PropertyField(m_group);
			EditorGUILayout.PropertyField(m_columnDescriptions);
			EditorGUILayout.PropertyField(m_startRow);

			EditorGUILayout.Space(10);
			if (GUILayout.Button("Process"))
				thisLocaProvider.CollectData();

			EditorGUILayout.Space(EditorGUIUtility.singleLineHeight);


			EditorGUILayout.PropertyField(m_processedLoca);

			serializedObject.ApplyModifiedProperties();
		}
	}
}
