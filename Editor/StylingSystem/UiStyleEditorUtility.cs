using System;
using System.Collections.Generic;
using System.Linq;
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
		public static void DrawStyle( UiAbstractApplyStyleBase _applier, UiAbstractStyleBase _style )
		{
			_applier.SetSkinListeners(true);
			EditorGUILayout.LabelField("Currently used Style:");
			EditorDisplayHelper.Draw(_style, "No Style assigned yet");
			_applier.SetSkinListeners(!_applier.SkinIsFixed);
		}

		// both _name and _copyFromName have to be the actual names and not aliases
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
