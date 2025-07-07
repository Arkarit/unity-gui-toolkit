using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Editor
{
	[CustomEditor(typeof(VisibilityByQuality), true)]
	public class VisibilityByQualityEditor : UnityEditor.Editor
	{
		SerializedProperty m_qualitiesVisibleProp;

		protected void OnEnable()
		{
			m_qualitiesVisibleProp = serializedObject.FindProperty("m_qualitiesVisible");
		}

		public override void OnInspectorGUI()
		{
			List<string> names = new();
			List<bool> values = new();
			int val = m_qualitiesVisibleProp.intValue;
			
			for (int i = 0;i < QualitySettings.names.Length; i++)
			{
				names.Add(QualitySettings.names[i]);
				values.Add((val & (1 << i)) != 0);
			}
			
			if (EditorUiUtility.BoolBar(names, values))
			{
				val = 0;
				for (int i = 0; i < values.Count; i++)
				{
					if (values[i])
						val |= 1 << i;
				}
				
				m_qualitiesVisibleProp.intValue = val;
				serializedObject.ApplyModifiedProperties();
			}
		}
	}
}