using System;
using System.Collections.Generic;
using GuiToolkit.Editor;
using UnityEditor;
using UnityEngine;

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
		/// <summary>
		/// The style this applier currently resolves, drawn inline.
		///
		/// An inherited style is shown read-only here, for the same reason as in the config inspector: the
		/// instance belongs to another asset, so editing it would change that config - and if that is the
		/// copy inside the package, the save is dropped without a word. The button copies it into this
		/// applier's own config first, which is what makes it editable.
		/// </summary>
		public static void DrawStyle( UiAbstractApplyStyleBase _applier, UiAbstractStyleBase _style )
		{
			_applier.SetSkinListeners(true);

			var config = _applier.StyleConfig;
			var ownSkin = _applier.OwnSkin;
			var resolvingSkin = _applier.ResolvingSkin;
			string requestedSkinName = _applier.SkinIsFixed ? _applier.FixedSkinName : resolvingSkin?.Name;

			// A skin the config does not declare itself resolves through an ancestor as a whole, so there is
			// no own skin to copy into and no override to offer - only something to say.
			bool skinIsForeign = ownSkin == null && resolvingSkin != null;

			// The row inside cannot tell whose style this is from its own property: the inline display wraps
			// it in a throwaway helper object, so the property names that instead of a config.
			// originShownAbove: the header below names where the style comes from, so the row itself must
			// not repeat it - in an applier there is exactly one style, and one statement is enough.
			using (UiStyleRowContext.Use(config, ownSkin, true))
			{
				bool isInherited = UiStyleRowContext.IsInherited(_style);
				bool isOverride = UiStyleRowContext.IsOverride(_style);

				// The asset AND the skin inside it. Naming only the config hides the case where two skins of
				// this config inherit from the same parent skin - a reasonable setup in which switching
				// between them changes nothing, and which without the skin name reads as a broken switch.
				//
				// A sibling skin of this very config is named by its skin alone; repeating the config there
				// would say nothing. That distinction lives in UiStyleRowContext, so this line and the row
				// header cannot drift apart.
				EditorGUILayout.LabelField
				(
					isInherited ? $"Currently used Style (inherited from {UiStyleRowContext.SourceNameLong(UiStyleRowContext.SkinOwnerOf(_style))}, read-only):"
					: isOverride ? $"Currently used Style (overrides {UiStyleRowContext.OverriddenSourceNameLong(_style)}):"
					: skinIsForeign && _style != null ? $"Currently used Style (from {UiStyleRowContext.SourceNameLong(resolvingSkin)}, read-only):"
					: "Currently used Style:"
				);

				if (_style == null)
				{
					// A style that does not resolve is not the same as none being assigned, and the generic
					// "nothing here" text made the two look alike - the state that cost the most time to
					// understand of all of them.
					var explanation = UiStyleDiagnostics.ExplainMissingStyle
					(
						config,
						resolvingSkin,
						requestedSkinName,
						_applier.Name,
						_applier.SupportedComponentType?.Name
					);

					if (string.IsNullOrEmpty(explanation))
						EditorDisplayHelper.Draw(null, "No Style assigned yet");
					else
						EditorGUILayout.HelpBox(explanation, MessageType.Warning);
				}
				else
				{
					// Not disabled from out here when the style is merely inherited: the row greys out its
					// own VALUES and keeps its buttons live, and an outer scope would grey out the override
					// button along with them. A foreign skin is the one case with nothing live to keep.
					using (new EditorGUI.DisabledScope(skinIsForeign))
					{
						EditorDisplayHelper.Draw(_style, "No Style assigned yet");
					}

					if (skinIsForeign)
					{
						EditorGUILayout.HelpBox
						(
							UiStyleDiagnostics.ExplainForeignSkin(config, requestedSkinName, resolvingSkin),
							MessageType.Warning
						);
					}
				}
			}

			_applier.SetSkinListeners(!_applier.SkinIsFixed);
		}

		/// <summary>
		/// What the Style popup shows as its current value.
		///
		/// The resolved style's alias when there is one. Otherwise the alias belonging to the STORED name,
		/// because a style the current skin cannot resolve is not unassigned - the name is untouched and
		/// comes back the moment the skin can resolve it again. Reading the display value off the resolved
		/// style left the popup blank in exactly that situation, which reads as "nothing assigned".
		///
		/// A name the list does not have is added as its own entry, to both lists at the same index so that
		/// index i keeps meaning the same style in both. Picking it is a no-op: it IS the current value, so
		/// the popup reports no change. It is marked as missing only when nothing resolved - the list is
		/// built from the FIRST skin, so a style that exists only in another skin is present, not missing,
		/// and would otherwise be slandered for being unusual.
		/// </summary>
		public static string ResolveDisplayAlias
		(
			string _storedName,
			UiAbstractStyleBase _resolvedStyle,
			List<string> _styleNames,
			List<string> _styleAliases
		)
		{
			string name = _resolvedStyle != null ? _resolvedStyle.Name : _storedName;
			if (string.IsNullOrEmpty(name))
				return string.Empty;

			int index = _styleNames.IndexOf(name);
			if (index >= 0)
				return _styleAliases[index];

			string entry = _resolvedStyle != null ? _resolvedStyle.Alias : $"{name}   (missing)";
			_styleNames.Insert(0, name);
			_styleAliases.Insert(0, entry);
			return entry;
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

		/// <summary>
		/// Whether this config can be written to at all, with a reason fit for a disabled menu entry.
		///
		/// Only real package installs are refused. The path test is deliberately just "Packages/": the
		/// toolkit's own dev app has the package symlinked into Assets/, and there it IS the thing being
		/// edited - a check that went by the toolkit root would lock the library out of its own config.
		/// </summary>
		public static bool IsWritable( UiStyleConfig _config, out string _reason )
		{
			if (_config == null)
			{
				_reason = "there is no config behind this row";
				return false;
			}

			string path = AssetDatabase.GetAssetPath(_config);
			if (!string.IsNullOrEmpty(path) && path.StartsWith("Packages/", StringComparison.Ordinal))
			{
				_reason = $"'{_config.name}' belongs to a read-only package";
				return false;
			}

			_reason = null;
			return true;
		}

		/// <summary>
		/// Creates the style values that a config on disk does not have yet, and says whether it had to.
		///
		/// Values are [SerializeReference] fields. When a style type gains one, Unity does NOT run the new
		/// field's initialiser for assets it loads - every config serialised before that moment keeps a null
		/// there. Nothing else notices: the runtime getters quietly create what they need, so only the
		/// inspector, which walks the serialised data rather than the object, is left looking at a managed
		/// reference that points nowhere.
		///
		/// The cost of leaving it: a null value cannot be switched on, cannot be styled, and used to take
		/// the whole skin inspector with it (see ApplicableValueBaseDrawer.IsMissingValue). So this runs once
		/// when a config is opened, and marks it dirty only when it actually filled something in - the
		/// alternative, dirtying every config anybody looks at, is worse than the bug.
		/// </summary>
		public static bool RepairMissingStyleValues( UiStyleConfig _config )
		{
			if (_config == null)
				return false;

			var serializedObject = new SerializedObject(_config);
			if (!HasMissingValue(serializedObject))
				return false;

			foreach (var skin in _config.Skins)
			{
				foreach (var style in skin.Styles)
				{
					if (style != null)
						style.RebuildValues();
				}
			}

			UiStyleConfig.SetDirty(_config);
			UiLog.Log($"'{_config.name}': filled in style values that did not exist yet. This happens once "
				+ "after a style type gained a value; save the config to keep it.");

			return true;
		}

		/// <summary>
		/// Whether any style holds a value that was never created. Asked through the serialised data, not
		/// through the object: the object's getters repair themselves on access, so it would always answer no.
		/// </summary>
		private static bool HasMissingValue( SerializedObject _serializedObject )
		{
			var skinsProp = _serializedObject.FindProperty("m_skins");
			if (skinsProp == null)
				return false;

			for (int i = 0; i < skinsProp.arraySize; i++)
			{
				var stylesProp = skinsProp.GetArrayElementAtIndex(i).FindPropertyRelative("m_styles");
				if (stylesProp == null)
					continue;

				for (int j = 0; j < stylesProp.arraySize; j++)
				{
					var styleProp = stylesProp.GetArrayElementAtIndex(j);
					if (styleProp.managedReferenceValue == null)
						continue;

					var iterator = styleProp.Copy();
					var end = styleProp.GetEndProperty();
					int depth = styleProp.depth;

					while (iterator.NextVisible(true) && !SerializedProperty.EqualContents(iterator, end))
					{
						// Only the style's own fields; a value's insides are none of this check's business.
						if (iterator.depth != depth + 1)
							continue;

						if (iterator.propertyType == SerializedPropertyType.ManagedReference
						 && iterator.managedReferenceValue == null)
						{
							return true;
						}
					}
				}
			}

			return false;
		}
	}
}
