using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Style.Editor
{
	[CustomPropertyDrawer(typeof(UiSkin), true)]
	public class UiSkinDrawer : AbstractPropertyDrawer
	{
		private const float GapBeforeStyles = 10;

		SerializedProperty 	m_nameProp;
		SerializedProperty m_stylesProp;

		protected override void OnEnable()
		{
			m_nameProp = Property.FindPropertyRelative("m_name");
			m_stylesProp = Property.FindPropertyRelative("m_styles");
		}

		protected override void OnInspectorGUI()
		{
			LabelField($"Style: {m_nameProp.stringValue}", 0, EditorStyles.boldLabel);
			PropertyField(m_nameProp);

			Space(GapBeforeStyles);
			for (int i = 0; i < m_stylesProp.arraySize; i++)
			{
				SerializedProperty styleProp = m_stylesProp.GetArrayElementAtIndex(i);
				PropertyField(styleProp);
			}
		}
	}
}