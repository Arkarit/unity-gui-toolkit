using System;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Style.Editor
{
	[CustomPropertyDrawer(typeof(UiSkin), true)]
	public class UiSkinDrawer : AbstractPropertyDrawer<UiSkin>
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
			BackgroundBox
			(
				new Color(0,0,0,.2f),
				new Color(1,1,1,.2f),
				0,
				0,
				0,
				SingleLineHeight + 5
			);

			LabelFieldAdditional($"Skin: {m_nameProp.stringValue}", 5,-2, 0, SingleLineHeight + 10, EditorStyles.boldLabel);

			Foldout(EditedClass.Name, $"", () =>
			{
				Space(10);
				Line(5);
				PropertyField(m_nameProp);

				Space(GapBeforeStyles);

				try
				{
					for (int i = 0; i < m_stylesProp.arraySize; i++)
					{
						SerializedProperty styleProp = m_stylesProp.GetArrayElementAtIndex(i);
						PropertyField(styleProp);
					}
				}
				catch {}
			});
		}
	}
}