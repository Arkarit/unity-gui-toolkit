using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Style.Editor
{
	[CustomPropertyDrawer(typeof(UiSkin), true)]
	public class UiSkinDrawer : AbstractPropertyDrawer
	{
		SerializedProperty 	m_nameProp;
		SerializedProperty m_stylesProp;

		protected override void OnEnable(SerializedProperty _baseProp)
		{
			m_nameProp = _baseProp.FindPropertyRelative("m_name");
			m_stylesProp = _baseProp.FindPropertyRelative("m_styles");
		}

		protected override void OnInspectorGUI()
		{
			LabelField($"Style: {m_nameProp.stringValue}", EditorStyles.boldLabel);
			PropertyField(m_nameProp);
			for (int i = 0; i < m_stylesProp.arraySize; i++)
			{
				SerializedProperty styleProp = m_stylesProp.GetArrayElementAtIndex(i);
				PropertyField(styleProp);
			}
		}
	}
}