using System.Collections.Generic;
using GuiToolkit.Editor;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Style.Editor
{
	[CustomPropertyDrawer(typeof(UiSkin), true)]
	public class UiSkinDrawer : AbstractPropertyDrawer<UiSkin>
	{
		protected SerializedProperty 	m_nameProp;
		protected SerializedProperty m_stylesProp;

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
			var displayFilter = UiStyleConfigEditor.DisplayFilter;

			Foldout(EditedClassInstance.Name, $"", () =>
			{
				Space(10);
				Line(5);

				try
				{
					var styles = GetSortedStylesList();
					foreach (var styleProp in styles)
					{
						if (!string.IsNullOrEmpty(displayFilter))
						{
							var style = styleProp.boxedValue as UiAbstractStyleBase;
							var searchName = UiStyleUtility.GetName(style.SupportedMonoBehaviourType, style.Name).ToLower();
							if (style != null && !searchName.Contains(displayFilter.ToLower()))
								continue;
						}

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

		private List<SerializedProperty> GetSortedStylesList()
		{
			List<SerializedProperty> result = new();

			for (int i = 0; i < m_stylesProp.arraySize; i++)
				result.Add(m_stylesProp.GetArrayElementAtIndex(i));

			result.Sort((a, b) =>
			{
				var styleA = a.boxedValue as UiAbstractStyleBase;
				var styleB = b.boxedValue as UiAbstractStyleBase;

				int nameComp = styleA.Name.CompareTo(styleB.Name);
				int typeComp = styleA.SupportedMonoBehaviourType.Name.CompareTo(styleB.SupportedMonoBehaviourType.Name);

				if (UiStyleConfigEditor.SortType == UiStyleConfigEditor.ESortType.NameDescending || UiStyleConfigEditor.SortType == UiStyleConfigEditor.ESortType.TypeDescending)
				{
					nameComp = -nameComp;
					typeComp = -typeComp;
				}

				if (UiStyleConfigEditor.SortType <= UiStyleConfigEditor.ESortType.NameDescending)
				{
					if (nameComp != 0)
						return nameComp;
					return typeComp;
				}

				if (typeComp != 0)
					return typeComp;

				return nameComp;
			});

			return result;
		}
	}
}