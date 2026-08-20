using System;
using System.Collections.Generic;
using GuiToolkit.Editor;
using UnityEditor;

namespace GuiToolkit.Style.Editor
{
	public static class UiStyleEditorUtility
	{
		public const string NoFixedSkinEntry = "<Use Global Skin>";

		public static string GetSelectSkinPopup( UiStyleConfig _config, string _currentAlias, out bool _hasChanged, bool _isFixedSkin = false )
		{
			_hasChanged = false;
			var skinNames = _config.SkinNames;
			var skinAliases = new List<string>(_config.SkinAliases);
			int numSkins = skinAliases.Count;
			string copyFromAlias = skinAliases.Count > 0 ? skinAliases[0] : string.Empty;
			string copyFromName = skinAliases.Count > 0 ? skinNames[0] : string.Empty;

			Action<AbstractEditorInputDialog> additionalContent = _ =>
			{
				if (string.IsNullOrEmpty(copyFromAlias))
					return;

				var copyFromIdx = EditorUiUtility.StringPopup("Copy skin from ", skinAliases, copyFromAlias, out string _);
				if (copyFromIdx != -1)
				{
					copyFromName = skinNames[copyFromIdx];
				}

				EditorGUILayout.Space(20);
			};

			string currentAlias = _currentAlias;
			if (_isFixedSkin)
			{
				skinAliases.Insert(0, NoFixedSkinEntry);
				if (string.IsNullOrEmpty(currentAlias))
					currentAlias = NoFixedSkinEntry;
			}

			var skinIdx = EditorUiUtility.StringPopup("Skin", skinAliases, currentAlias, out string selectedEntry,
					null, false, "Add Skin", "Adds a new skin", null, additionalContent);

			if (_isFixedSkin)
			{
				if (selectedEntry == NoFixedSkinEntry)
				{
					if (skinIdx <= 0)
					{
						_hasChanged = skinIdx == 0;
						return null;
					}
				}

				skinIdx--;
			}

			if (skinIdx >= 0)
			{
				bool userSelectedNewEntry = skinIdx >= numSkins;
				if (userSelectedNewEntry)
				{
					AddSkin(_config, selectedEntry, copyFromName);
				}

				_hasChanged = true;
				return selectedEntry;
			}

			return _currentAlias;
		}

		public static void SelectSkinByPopup( UiStyleConfig _config )
		{
			var currentSkinAlias = _config.CurrentSkinAlias;
			_config.CurrentSkinAlias = GetSelectSkinPopup(_config, currentSkinAlias, out bool _);
		}

		// Draw a style in the inspector without the need to actually [SerializeReference] it (which totally bloats stuff)
		public static void DrawStyle( UiAbstractApplyStyleBase _applier, UiAbstractStyleBase _style )
		{
			_applier.SetSkinListeners(true);
			EditorGUILayout.LabelField("Currently used Style:");
			EditorDisplayHelper.Draw(_style, "No Style assigned yet");
			_applier.SetSkinListeners(!_applier.SkinIsFixed);
		}

		// both _name and _copyFromName have to be the actual names and not aliases
		/// <summary>
		/// A SerializedProperty for every style the skin inherits rather than owns, so the inspector can
		/// draw them with the same drawers as the own ones instead of with a second, poorer implementation.
		///
		/// An inherited style lives in another asset, so its property comes from that asset's
		/// SerializedObject - which is why those are cached here and refreshed on use. Drawing a property of
		/// a foreign object is fine; writing to it is what must not happen, and that is what the style
		/// drawer disables.
		/// </summary>
		public static List<SerializedProperty> InheritedStyleProperties( UiSkin _skin )
		{
			var result = new List<SerializedProperty>();
			if (_skin?.StyleConfig?.Parent == null)
				return result;

			// Walked skin by skin, not config by config: a skin may inherit from a DIFFERENTLY NAMED skin of
			// the parent, so which skin a style lives in is only knowable one hop at a time.
			var chain = _skin.SelfAndInheritedSkins();
			var seen = new HashSet<int>();
			foreach (var own in _skin.Styles)
				seen.Add(own.Key);

			for (int i = 1; i < chain.Count; i++)
			{
				var inheritedSkin = chain[i];
				var map = StylePropertiesByKey(inheritedSkin.StyleConfig, inheritedSkin.Name);

				foreach (var style in inheritedSkin.Styles)
				{
					// Nearest wins, so a style already seen further down the chain is not added again.
					if (!seen.Add(style.Key))
						continue;

					if (map.TryGetValue(style.Key, out var styleProp))
						result.Add(styleProp);
				}
			}

			return result;
		}

		/// <summary>
		/// key -> SerializedProperty for every style of the same-named skin in that config.
		/// </summary>
		public static Dictionary<int, SerializedProperty> StylePropertiesByKey( UiStyleConfig _config, string _skinName )
		{
			var result = new Dictionary<int, SerializedProperty>();
			if (_config == null)
				return result;

			var skinsProp = SerializedConfig(_config).FindProperty("m_skins");
			if (skinsProp == null)
				return result;

			for (int i = 0; i < skinsProp.arraySize; i++)
			{
				var skinProp = skinsProp.GetArrayElementAtIndex(i);
				if (skinProp.FindPropertyRelative("m_name")?.stringValue != _skinName)
					continue;

				var stylesProp = skinProp.FindPropertyRelative("m_styles");
				for (int j = 0; j < stylesProp.arraySize; j++)
				{
					var styleProp = stylesProp.GetArrayElementAtIndex(j);
					if (styleProp.boxedValue is UiAbstractStyleBase style)
						result[style.Key] = styleProp;
				}

				break;
			}

			return result;
		}

		private static readonly Dictionary<UiStyleConfig, SerializedObject> s_serializedConfigs = new();

		/// <summary>
		/// A SerializedObject per config, kept across repaints and refreshed on use - building one per repaint
		/// for every ancestor would be wasteful, and they are only ever read here.
		/// </summary>
		private static SerializedObject SerializedConfig( UiStyleConfig _config )
		{
			if (s_serializedConfigs.TryGetValue(_config, out var existing) && existing?.targetObject != null)
			{
				existing.Update();
				return existing;
			}

			var created = new SerializedObject(_config);
			s_serializedConfigs[_config] = created;
			return created;
		}

		public static string AddSkin( UiStyleConfig _config, string _name, string _copyFromName = null )
		{
			if (_config.SkinNames.Contains(_name))
				return string.Empty;

			var newSkin = new UiSkin(_config, _name);

			UiSkin copyFrom = null;
			if (!string.IsNullOrEmpty(_copyFromName))
			{
				foreach (var skin in _config.Skins)
				{
					if (skin.Name == _copyFromName)
					{
						copyFrom = skin;
						break;
					}
				}
			}

			if (copyFrom != null)
			{
				foreach (var style in copyFrom.Styles)
				{
					var newStyle = style.DeepClone();
					newSkin.Styles.Add(newStyle);
				}
			}

			UiEventDefinitions.EvAddSkin.InvokeAlways(_config, newSkin);
			UiStyleConfig.SetDirty(_config);
			return _name;
		}
	}
}
