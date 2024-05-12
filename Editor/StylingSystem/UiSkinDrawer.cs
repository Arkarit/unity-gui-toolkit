using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Style.Editor
{
	[CustomPropertyDrawer(typeof(UiSkin), true)]
	public class UiSkinDrawer : AbstractPropertyDrawer<UiSkin>
	{
		SerializedProperty 	m_nameProp;
		SerializedProperty m_stylesProp;

		protected override void OnEnable()
		{
			m_nameProp = Property.FindPropertyRelative("m_name");
			m_stylesProp = Property.FindPropertyRelative("m_styles");
		}

		protected override void OnInspectorGUI()
		{
			BackgroundAbsHeight
			(
				new Color(0,0,0,.15f),
				new Color(1,1,1,.15f),
				0,
				0,
				0,
				SingleLineHeight + 5
			);

			var foldoutTitleRect = CurrentRect;
			foldoutTitleRect.height = SingleLineHeight;

			Foldout(EditedClass.Name, $"", () =>
			{
				Space(10);
				Line(5);

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

			if (CollectHeightMode)
				return;

			var skinName = m_nameProp.stringValue;
			var foldoutLabelRect = foldoutTitleRect;
			foldoutLabelRect.x += 5;
			foldoutLabelRect.y += 3;
			EditorGUI.LabelField(foldoutLabelRect, $"Skin: {m_nameProp.stringValue}", EditorStyles.boldLabel);
			var deleteButtonRect = foldoutTitleRect;
			deleteButtonRect.x = deleteButtonRect.width + deleteButtonRect.x - 60;
			deleteButtonRect.y += 2;
			deleteButtonRect.width = 50;
			if (GUI.Button(deleteButtonRect, "Delete"))
			{

				if (EditorUtility.DisplayDialog
			    (
				    "Are you sure?",
				    $"The skin '{skinName}' will be removed from UiMainStyleConfig" 
				    + " and all Ui Apply Style instances which use it. This can not be undone.",
				    "OK",
				    "Cancel"
			    ))
				{
					UiEvents.EvDeleteSkin.InvokeAlways(skinName);
				}
			}
		}
	}
}