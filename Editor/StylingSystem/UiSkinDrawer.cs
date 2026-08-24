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
		protected SerializedProperty m_aspectRatioGreaterEqualProp;
		protected UiSkin m_thisUiSkin;

		/// <summary>
		/// Sorted style lists, per skin. Static and keyed by property path on purpose: Unity keeps ONE
		/// PropertyDrawer instance for a whole array (its handler cache ignores the array index), so this
		/// same drawer object serves every skin in turn. Held per instance, the cache missed on every
		/// switch between two open skins - and since a miss also drops the remembered row heights, having
		/// two skins open meant nothing was ever cached at all. That is measurable: the layout pass went
		/// from 3.2 ms with one skin open back up to 35 ms with two.
		/// </summary>
		private static readonly Dictionary<string, SortedStyles> s_sortedStylesByPath = new();

		private class SortedStyles
		{
			public string Key;
			public List<SerializedProperty> List;
			public SerializedObject Owner;
		}

		[Serializable]
		private class JsonHelper
		{
			public UiSkin Skin;
		}
		
		public string skinName => m_thisUiSkin != null ? m_thisUiSkin.Name : null;
		public string skinAlias => m_thisUiSkin != null ? m_thisUiSkin.Alias : null;

		protected override void OnEnable()
		{
			// A style row's height is a recursive walk over all its values, and it is asked for twice per
			// repaint (once to lay out, once to draw). Caching it turns the second walk - and every
			// unchanged repaint after it - into a dictionary lookup. Everything that can change a row's
			// height clears the cache: a foldout toggling, a value becoming applicable, the filter.
			HeightCacheEnabled = true;

			m_thisUiSkin = FindRealSkin();
			m_stylesProp = Property.FindPropertyRelative("m_styles");
			m_aspectRatioGreaterEqualProp = Property.FindPropertyRelative("m_aspectRatioGreaterEqual");
		}

		/// <summary>
		/// Lets a skin say which skin of the parent config it builds on.
		///
		/// Only shown when there is a parent, and only offering skins that parent actually has. The default
		/// entry is "same name", which is right whenever the two configs agree on their skin names - and
		/// wrong for a project's own skin, which is exactly the case this popup exists for: a client config
		/// with Default and BOTW inheriting from a package config with Default and Light would leave BOTW
		/// with nothing to inherit.
		/// </summary>
		/// <summary>What one entry of the "inherits skin from" popup stands for.</summary>
		private readonly struct InheritCandidate
		{
			public readonly string Label;
			public readonly string SkinName;
			public readonly bool SameConfig;

			public InheritCandidate( string _label, string _skinName, bool _sameConfig )
			{
				Label = _label;
				SkinName = _skinName;
				SameConfig = _sameConfig;
			}
		}

		private void DrawInheritFromSkinPopup()
		{
			if (m_thisUiSkin == null)
				return;

			var config = m_thisUiSkin.StyleConfig;
			if (config == null)
				return;

			var parentConfig = config.Parent;
			var candidates = BuildInheritCandidates(config, parentConfig);

			// Nothing to build on and nothing that could be: no field, rather than an empty one.
			if (candidates.Count <= 1)
				return;

			var labels = new List<string>();
			foreach (var candidate in candidates)
				labels.Add(candidate.Label);

			string current = CurrentInheritLabel(candidates, parentConfig);
			if (!labels.Contains(current))
				labels.Add(current);

			// The popup draws its own label and shifts the field by labelWidth itself, so a label of ours in
			// front of it would push the field a second time - which left it narrow and hugging the right
			// edge. Handing the label over instead gives it the whole remaining width, and there is no
			// reason to save space here.
			Space(10);
			Horizontal(SingleLineHeight, () =>
			{
				IncreaseX(14);

				if (!StringPopupField("Inherits skin from", labels, current, out string chosen))
					return;

				int index = labels.IndexOf(chosen);
				if (index < 0 || index >= candidates.Count)
					return;                       // the "still stored" entry - nothing chosen

				var candidate = candidates[index];
				m_thisUiSkin.InheritFromSameConfig = candidate.SameConfig;
				m_thisUiSkin.InheritFromSkinName = candidate.SkinName;
				PropertyDrawerView.ClearHeightCache();
				EditorApplication.delayCall += () => UiEventDefinitions.EvSkinChanged.InvokeAlways(0);
			});

			if (m_thisUiSkin.ParentSkin == null)
			{
				Space(4);
				Horizontal(SingleLineHeight, () =>
				{
					IncreaseX(14);
					LabelField(NothingInheritedText(parentConfig), 0, EditorStyles.miniLabel);
				});
			}

			Space(8);
		}

		/// <summary>
		/// Everything this skin could build on: the parent's skins, and the other skins of this config.
		///
		/// Siblings are offered because that is often what a skin actually is - a variant of the skin next to
		/// it rather than of anything in the parent. Excluded are this skin itself and any sibling that
		/// already builds on it, since choosing one of those closes a circle.
		/// </summary>
		private List<InheritCandidate> BuildInheritCandidates( UiStyleConfig _config, UiStyleConfig _parentConfig )
		{
			var result = new List<InheritCandidate>
			{
				new InheritCandidate
				(
					_parentConfig != null ? $"<same name ('{m_thisUiSkin.Name}')>" : "<nothing>",
					null,
					false
				)
			};

			foreach (var skin in _config.Skins)
			{
				if (skin == m_thisUiSkin || m_thisUiSkin.WouldInheritingFromCreateACycle(skin))
					continue;

				result.Add(new InheritCandidate($"{skin.Name}   (this config)", skin.Name, true));
			}

			if (_parentConfig != null)
			{
				foreach (var skinName in _parentConfig.SkinNames)
					result.Add(new InheritCandidate($"{skinName}   ('{_parentConfig.name}')", skinName, false));
			}

			return result;
		}

		/// <summary>
		/// The entry that stands for what is stored right now - or a label of its own when what is stored
		/// exists nowhere any more, because a stored name that the popup cannot show would silently read as
		/// something else.
		/// </summary>
		private string CurrentInheritLabel( List<InheritCandidate> _candidates, UiStyleConfig _parentConfig )
		{
			string storedName = m_thisUiSkin.InheritFromSkinName;
			bool sameConfig = m_thisUiSkin.InheritFromSameConfig;

			if (string.IsNullOrEmpty(storedName))
				return _candidates[0].Label;

			foreach (var candidate in _candidates)
			{
				if (candidate.SkinName == storedName && candidate.SameConfig == sameConfig)
					return candidate.Label;
			}

			return sameConfig
				? $"{storedName}  (missing in this config)"
				: $"{storedName}  (missing in '{(_parentConfig != null ? _parentConfig.name : "<no parent>")}')";
		}

		private string NothingInheritedText( UiStyleConfig _parentConfig )
		{
			if (m_thisUiSkin.InheritFromSameConfig)
			{
				return $"inherits nothing - this config has no skin '{m_thisUiSkin.InheritFromSkinName}'";
			}

			if (_parentConfig == null)
				return "inherits nothing - this config has no parent, and no skin of this one is chosen";

			return $"inherits nothing - '{_parentConfig.name}' has no skin "
				+ $"'{m_thisUiSkin.EffectiveInheritFromSkinName}'";
		}

		/// <summary>
		/// The skin instance that actually lives in the config, NOT Property.boxedValue.
		///
		/// UiSkin is a plain [Serializable] class in List&lt;UiSkin&gt;, so its property type is
		/// Generic, and for Generic properties boxedValue builds a fresh managed copy on every
		/// single access. Only SerializeReference (ManagedReference) properties hand back the
		/// real object - which is why everything this drawer does to *styles* works (m_styles is
		/// [SerializeReference], so those are the real instances) while anything written to a
		/// skin-level field of the boxed copy is dropped without a trace.
		///
		/// Resolving the skin by array index keeps identity intact, and saves a deep copy per
		/// repaint on top (OnEnable runs from both OnGUI and GetPropertyHeight).
		/// </summary>
		private UiSkin FindRealSkin()
		{
			var config = Property.serializedObject.targetObject as UiStyleConfig;
			var idx = Property.GetArrayIndex();
			if (config != null && idx >= 0 && idx < config.NumSkins)
				return config.Skins[idx];

			// Not an element of a config's skin list (nested/standalone use) - the copy is all there is.
			return Property.boxedValue as UiSkin;
		}

		protected override void OnInspectorGUI()
		{
			if (m_thisUiSkin == null)
			{
				UiLog.LogError("Skin is null");
				return;
			}
			
			var styleConfig = m_thisUiSkin.StyleConfig;
			var currentSkin = styleConfig.CurrentSkin;

			// Everything drawn from here on belongs to this skin of this config, and the style rows inside
			// need to know that - they cannot tell from their own property. Scoped, so it cannot leak into
			// whatever is drawn after this drawer is done.
			using var rowContext = UiStyleRowContext.Use(styleConfig, m_thisUiSkin);
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

				if (m_thisUiSkin.IsAspectRatioDependent)
				{
					IncreaseX(-490);
					LabelField("Aspect Ratio >= ");
					IncreaseX(100);

					float before = m_aspectRatioGreaterEqualProp.floatValue;
					float after = Float(before, 80);
					if (!Mathf.Approximately(before, after))
					{
						m_aspectRatioGreaterEqualProp.floatValue = after;
						EditorGeneralUtility.SetDirty(styleConfig);
					}

					IncreaseX(80);
				}
				else
				{
					IncreaseX(-310);
				}

				if (Button("HSV", 55))
				{
					var hsv = UiSkinHSVDialog.GetWindow();
					hsv.Skin = m_thisUiSkin;
					hsv.StyleConfig = styleConfig;
				}
				IncreaseX(60);
				
				if (Button("Copy", 55))
				{
					var jsonHelper = new JsonHelper()
					{
						Skin = m_thisUiSkin
					};
					
					var jsonStr = UnityEngine.JsonUtility.ToJson(jsonHelper, true);
					GUIUtility.systemCopyBuffer = jsonStr;
				}
				IncreaseX(60);
				
				if (Button("Paste", 55))
				{
					var jsonStr = GUIUtility.systemCopyBuffer;
					var jsonHelper = UnityEngine.JsonUtility.FromJson<JsonHelper>(jsonStr);
					for (int i=0; i < jsonHelper.Skin.Styles.Count && i < m_thisUiSkin.Styles.Count; i++)
					{
						var fromStyle = jsonHelper.Skin.Styles[i];
						var toStyle = m_thisUiSkin.Styles[i];
						
						for (int j=0; j < fromStyle.Values.Length && j < toStyle.Values.Length; j++)
						{
							toStyle.Values[j].RawValueObj = fromStyle.Values[j].RawValueObj;
						}
					}
					EditorGeneralUtility.SetDirty(styleConfig);
					EditorApplication.delayCall += () => UiEventDefinitions.EvSkinChanged.InvokeAlways(0);
				}
				IncreaseX(60);
				
				if (Button("Rename", 55))
				{
					// Create copies due to shitty c# not able to define capture copy in lambda
					var skinAliasCopy = skinAlias;
					var thisUiSkinCaptured = m_thisUiSkin;
					
					EditorApplication.delayCall += () =>
					{
						Action<AbstractEditorInputDialog> additionalContent = dialog =>
						{
							if (GUILayout.Button("Reset Name"))
							{
								UiEventDefinitions.EvSetSkinAlias.InvokeAlways(thisUiSkinCaptured.StyleConfig, thisUiSkinCaptured, null);
								dialog.Cancel();
							}
							
							EditorGUILayout.Space(20);
						};
					
						var newName = EditorInputDialog.Show("Rename", "Please enter new name", skinAliasCopy, additionalContent);
						if (!string.IsNullOrEmpty(newName))
						{
							UiEventDefinitions.EvSetSkinAlias.InvokeAlways(thisUiSkinCaptured.StyleConfig, thisUiSkinCaptured, newName);
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
			
			DrawInheritFromSkinPopup();

			var foldoutTitleRect = CurrentRect;
			foldoutTitleRect.height = SingleLineHeight;
			var displayFilter = UiStyleConfigEditor.DisplayFilter;

			if (UiStyleConfigEditor.SortType < UiStyleConfigEditor.ESortType.FlatPathAscending)
			{
				try
				{
					Space(-17);
					
					var foldoutOpen = Foldout(m_thisUiSkin.Name, $"", () =>
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
					
				var foldoutOpen = Foldout(m_thisUiSkin.Name, $"", () =>
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
				UiLog.LogInternal($"{tabStr}{Name}");
				foreach (var kv in Children)
				{
					var current = kv.Value;
					current.Dump(tabStr + "\t");
					foreach (var property in current.Properties)
					{
						UiLog.LogInternal($"{tabStr}\t\t->{property.boxedValue.GetType()}");
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

		/// <summary>
		/// The sorted style list, cached.
		///
		/// Building it means one SerializedProperty copy per style (~4 us each) plus a sort whose
		/// comparator unboxes both operands - measured at 1.7 ms for 80 styles. That ran on every pass,
		/// and there are two passes per repaint (height, then draw) per skin, so a two-skin config spent
		/// ~7 ms per repaint on sorting a list that had not changed.
		///
		/// The key covers everything the result depends on: which skin, the sort order and filter (both
		/// static, set in the config editor), and the number of styles. The SerializedObject is part of it
		/// too, because cached property copies belong to the object they came from - a new one (another
		/// inspector, a re-created editor) has to start over.
		/// </summary>
		private List<SerializedProperty> GetFlatSortedStylesList()
		{
			var path = m_stylesProp.propertyPath;
			// The effective count and the parent are part of the key, because the list now holds inherited
			// rows too: assigning a parent, or a style appearing in one, changes what is shown here without
			// changing anything in this config.
			var parentId = m_thisUiSkin?.StyleConfig?.Parent != null
				? m_thisUiSkin.StyleConfig.Parent.GetInstanceID()
				: 0;
			var effectiveCount = m_thisUiSkin?.EffectiveStyles.Count ?? 0;
			var key = $"{skinName}|{UiStyleConfigEditor.SortType}|{UiStyleConfigEditor.DisplayFilter}"
			        + $"|{m_stylesProp.arraySize}|{effectiveCount}|{parentId}";

			if (s_sortedStylesByPath.TryGetValue(path, out var cached)
			    && cached.Key == key
			    && ReferenceEquals(cached.Owner, Property.serializedObject))
			{
				return cached.List;
			}

			// This skin's set of rows or their order changed. Row heights are remembered by property path,
			// and a deletion shifts every path after it onto a different style - so the remembered heights
			// have to go with the list they belonged to.
			PropertyDrawerView.ClearHeightCache();

			var sorted = BuildFlatSortedStylesList();
			s_sortedStylesByPath[path] = new SortedStyles
			{
				Key = key,
				List = sorted,
				Owner = Property.serializedObject
			};

			return sorted;
		}

		private List<SerializedProperty> BuildFlatSortedStylesList()
		{
			List<SerializedProperty> result = new();

			for (int i = 0; i < m_stylesProp.arraySize; i++)
				result.Add(m_stylesProp.GetArrayElementAtIndex(i));

			result.AddRange(UiStyleEditorUtility.InheritedStyleProperties(m_thisUiSkin));

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
