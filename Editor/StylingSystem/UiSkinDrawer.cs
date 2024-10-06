using GuiToolkit.Editor;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Style.Editor
{
	[CustomPropertyDrawer(typeof(UiSkin), true)]
	public class UiSkinDrawer : AbstractPropertyDrawer<UiSkin>
	{
		protected SerializedProperty m_stylesProp;
		protected UiSkin m_thisUiSkin;

		public string skinName => m_thisUiSkin != null ? m_thisUiSkin.Name : null;
		public string skinAlias => m_thisUiSkin != null ? m_thisUiSkin.Alias : null;

		protected override void OnEnable()
		{
			m_thisUiSkin = Property.boxedValue as UiSkin;
			m_stylesProp = Property.FindPropertyRelative("m_styles");
		}

		protected override void OnInspectorGUI()
		{
			if (m_thisUiSkin == null)
			{
				Debug.LogError("Skin is null");
				return;
			}
			
			var styleConfig = m_thisUiSkin.StyleConfig;
			var currentSkin = styleConfig.CurrentSkin;
			bool isCurrentSkin = skinName == currentSkin.Name;
			
			BackgroundBox
			(
				isCurrentSkin ? new Color(0,0.5f,0,.15f) : new Color(0,0,0,.15f),
				isCurrentSkin ? new Color(.75f,1,.75f,.15f) : new Color(.75f,.75f,.75f,.15f),
				0,
				-5,
				0,
				SingleLineHeight + 10
			);

			Horizontal(SingleLineHeight, () =>
			{
				IncreaseX(2);
				
				bool newCurrent = Toggle("", isCurrentSkin);
				if (newCurrent && !isCurrentSkin)
				{
					isCurrentSkin = true;
					styleConfig.CurrentSkinName = skinName;
					return;
				}
				
				IncreaseX(10);
				
				LabelField("   " + skinAlias, 0, EditorStyles.boldLabel);
	
				IncreaseX(-130);
	
				if (Button("Rename", 55))
				{
					// Create copies due to shitty c# not able to define capture copy in lambda
					var skinAliasCopy = skinAlias;
					var thisUiSkinCopy = m_thisUiSkin;
					
					EditorApplication.delayCall += () =>
					{
						Action<EditorInputDialog> additionalContent = dialog =>
						{
							if (GUILayout.Button("Reset Name"))
							{
								UiEventDefinitions.EvSetSkinAlias.InvokeAlways(thisUiSkinCopy.StyleConfig, thisUiSkinCopy, null);
								dialog.Cancel();
							}
							
							EditorGUILayout.Space(20);
						};
					
						var newName = EditorInputDialog.Show("Rename", "Please enter new name", skinAliasCopy, additionalContent);
						if (!string.IsNullOrEmpty(newName))
						{
							UiEventDefinitions.EvSetSkinAlias.InvokeAlways(thisUiSkinCopy.StyleConfig, thisUiSkinCopy, newName);
						}
					};
				}
					
				IncreaseX(60);
				
				if (Button("Delete", 50))
				{
					if (EditorUtility.DisplayDialog
				    (
					    "Are you sure?",
					    $"The skin '{skinAlias}' (identifier '{skinName}') will be removed from UiStyleConfig" 
					    + " and all UI Apply Style instances which use it. This can not be undone.",
					    "OK",
					    "Cancel"
				    ))
					{
						UiEventDefinitions.EvDeleteSkin.InvokeAlways(m_thisUiSkin.StyleConfig, skinName);
					}
				}
			});
			
			var foldoutTitleRect = CurrentRect;
			foldoutTitleRect.height = SingleLineHeight;
			var displayFilter = UiStyleConfigEditor.DisplayFilter;

			if (UiStyleConfigEditor.SortType < UiStyleConfigEditor.ESortType.FlatPathAscending)
			{
				try
				{
					Space(-17);
					
					var foldoutOpen = Foldout(EditedClassInstance.Name, $"", () =>
					{
						var styles = GetFlatSortedStylesList();
						var snp = new StyleTree();
						snp.Build(this, styles);
						Space(8);
						Line(5);
						snp.Display(this);
						Line(5);
						Space(1);
					});
					
					if (!foldoutOpen)
						Space(13);
				}
				catch
				{
				}
			}
			else
			{
				Space(-17);
					
				var foldoutOpen = Foldout(EditedClassInstance.Name, $"", () =>
				{
					Space(10);
					Line(5);

					try
					{
						var styles = GetFlatSortedStylesList();
						foreach (var styleProp in styles)
						{
							if (!CheckFilter(displayFilter, styleProp))
								continue;
var scprop = styleProp.FindPropertyRelative("m_styleConfig");
scprop.objectReferenceValue = m_thisUiSkin.StyleConfig;
EditorUtility.SetDirty(m_thisUiSkin.StyleConfig);
							PropertyField(styleProp);
						}
					}
					catch
					{
					}
				});
				
				if (!foldoutOpen)
					Space(13);
			}
		}

		private class StyleTree
		{
			public string Name = string.Empty;
			public readonly Dictionary<string, StyleTree> Children = new();
			public readonly List<SerializedProperty> Properties = new ();
			public int Id;

			public void Build(UiSkinDrawer drawer, List<SerializedProperty> flatList)
			{
				Id = 0xddfa0 + (UiStyleConfigEditor.SynchronizeFoldouts ? 0 : Animator.StringToHash(drawer.skinName));

				var displayFilter = UiStyleConfigEditor.DisplayFilter;
				
				foreach (var property in flatList)
				{
					StyleTree current = this;

					var style = property.boxedValue as UiAbstractStyleBase;
					if (style == null)
						continue;
					
					if (!CheckFilter(displayFilter, property))
						continue;
					
					string s = style.Alias;
					
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

			private StyleTree GetNew()
			{
				var result = new StyleTree();
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

			public void Display(UiSkinDrawer drawer)
			{
				Display(drawer, this);
			}

			private void Display(UiSkinDrawer drawer, StyleTree current)
			{
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

					drawer.Outdent(() =>
					{
						foreach (var property in current.Properties)
						{
							drawer.PropertyField(property);
						}
					});
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

				int nameComp = styleA.Alias.CompareTo(styleB.Alias);
				int typeComp = styleA.SupportedComponentType.Name.CompareTo(styleB.SupportedComponentType.Name);

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
		
		private static bool CheckFilter(UiStyleEditorFilter displayFilter, SerializedProperty styleProp)
		{
			if (!displayFilter.ShowAll)
			{
				var style = styleProp.boxedValue as UiAbstractStyleBase;
				if (!displayFilter.HasName(style.Alias))
					return false;

				if (!displayFilter.HasType(style.SupportedComponentType.Name))
					return false;
			}

			return true;
		}
	}
}
