using System;
using System.Collections.Generic;
using System.Xml.XPath;
using GuiToolkit.Editor;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Style.Editor
{
	[CustomPropertyDrawer(typeof(UiSkin), true)]
	public class UiSkinDrawer : AbstractPropertyDrawer<UiSkin>
	{
		protected SerializedProperty m_nameProp;
		protected SerializedProperty m_stylesProp;

		public string skinName
		{
			get
			{
				if (m_nameProp == null)
					return string.Empty;

				return m_nameProp.stringValue;
			}
		}

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

			if (UiStyleConfigEditor.SortType < UiStyleConfigEditor.ESortType.FlatPathAscending)
			{
				try
				{
					Foldout(EditedClassInstance.Name, $"", () =>
					{
						var styles = GetFlatSortedStylesList();
						var snp = new StyleNamePart();
						snp.BuildTree(this, styles);
//						snp.Dump();
						Space(10);
						Line(5);
						snp.Display(this);
						Line(5);
					});
				}
				catch
				{
				}
			}
			else
			{
				Foldout(EditedClassInstance.Name, $"", () =>
				{
					Space(10);
					Line(5);

					try
					{
						var styles = GetFlatSortedStylesList();
						foreach (var styleProp in styles)
						{
							if (!string.IsNullOrEmpty(displayFilter))
							{
								var style = styleProp.boxedValue as UiAbstractStyleBase;
								var searchName = UiStyleUtility.GetName(style.SupportedMonoBehaviourType, style.Name)
									.ToLower();
								if (style != null && !searchName.Contains(displayFilter.ToLower()))
									continue;
							}

							PropertyField(styleProp);
						}
					}
					catch
					{
					}
				});
			}

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

		private class StyleNamePart
		{
			public string Name = string.Empty;
			public readonly Dictionary<string, StyleNamePart> Children = new();
			public readonly List<SerializedProperty> Properties = new ();
			public int Id;

			public void BuildTree(UiSkinDrawer drawer, List<SerializedProperty> flatList)
			{
				Id = 0xddfa0 + Animator.StringToHash(drawer.skinName);

				foreach (var property in flatList)
				{
					StyleNamePart current = this;

					string s = property.displayName;
					while (true)
					{
						(string a, string b) = Split(s);
						if (string.IsNullOrEmpty(a))
						{
							if (!current.Children.ContainsKey(b))
							{
								current.Children.Add(b, GetNew());
							}
							current = current.Children[b];
							current.Name = b;
							current.Properties.Add(property);
							break;
						}

						if (!current.Children.ContainsKey(a))
						{
							current.Children.Add(a, GetNew());
						}
						current = current.Children[a];
						current.Name = a;
						s = b;
					}
				}
			}

			private StyleNamePart GetNew()
			{
				var result = new StyleNamePart();
				result.Id = Id++;
				return result;
			}

			public void Dump() => Dump(string.Empty);
			private void Dump(string tabStr)
			{
				Debug.Log($"{tabStr}{Name}");
				foreach (var kv in Children)
				{
					var current = kv.Value;
					current.Dump(tabStr + "\t");
					foreach (var property in current.Properties)
					{
						Debug.Log($"{tabStr}\t\t->{property.boxedValue.GetType()}");
					}
				}
			}

			private int m_id;
			public void Display(UiSkinDrawer drawer)
			{
				m_id = 0x70;
				Display(drawer, this);
			}

			private void Display(UiSkinDrawer drawer, StyleNamePart current)
			{
//				foreach (var property in current.Properties)
//				{
//					drawer.PropertyField(property);
//				}

				if (string.IsNullOrEmpty(current.Name))
				{
					foreach (var kv in current.Children)
						Display(drawer, kv.Value);
					return;
				}

				drawer.Foldout(current.Id, current.Name, false, () =>
				{
					foreach (var kv in current.Children)
						Display(drawer, kv.Value);
				});
			}


			private (string, string) Split(string s)
			{
				var idx = s.IndexOf("/");
				if (idx == -1)
					return (string.Empty, s);

				return (s.Substring(0, idx), s.Substring(idx + 1));
			}
		}

		private List<SerializedProperty> GetFlatSortedStylesList()
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

				if (   UiStyleConfigEditor.SortType == UiStyleConfigEditor.ESortType.PathDescending 
				    || UiStyleConfigEditor.SortType == UiStyleConfigEditor.ESortType.FlatPathDescending
					|| UiStyleConfigEditor.SortType == UiStyleConfigEditor.ESortType.FlatTypeDescending)
				{
					nameComp = -nameComp;
					typeComp = -typeComp;
				}

				if (UiStyleConfigEditor.SortType <= UiStyleConfigEditor.ESortType.FlatPathDescending)
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