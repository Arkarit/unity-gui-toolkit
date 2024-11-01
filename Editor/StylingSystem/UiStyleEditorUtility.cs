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
		
		private class DisplayStyleHelperObject : ScriptableObject
		{
			[SerializeReference]
			public UiAbstractStyleBase Style;
		}
		
		private class DisplayStyleHelperObjectEditor : UnityEditor.Editor
		{
			public override void OnInspectorGUI() => DrawInspectorExcept(serializedObject, "m_Script");
		}
		
		private static DisplayStyleHelperObject m_styleHelper;
		private static DisplayStyleHelperObjectEditor m_styleHelperEditor;
		
		private static DisplayStyleHelperObject StyleHelper
		{
			get
			{
				if (m_styleHelper == null)
					m_styleHelper = ScriptableObject.CreateInstance<DisplayStyleHelperObject>();
				
				return m_styleHelper;
			}
		}
		
		private static UnityEditor.Editor StyleHelperEditor
		{
			get
			{
				if (m_styleHelperEditor == null)
					m_styleHelperEditor = (DisplayStyleHelperObjectEditor) UnityEditor.Editor.CreateEditor(StyleHelper, typeof(DisplayStyleHelperObjectEditor));
				
				return m_styleHelperEditor;
			}
		}
		
		public static string GetSelectSkinPopup(UiStyleConfig _config, string _currentAlias, out bool _hasChanged, bool _isFixedSkin = false)
		{
			_hasChanged = false;
			var skinNames = _config.SkinNames;
			var skinAliases = new List<string>(_config.SkinAliases);
			int numSkins = skinAliases.Count;
			string copyFromAlias = skinAliases.Count > 0 ? skinAliases[0] : string.Empty;
			string copyFromName = skinAliases.Count > 0 ? skinNames[0] : string.Empty;

			Action<EditorInputDialog> additionalContent = _ =>
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
				    null, false, "Add Skin", "Adds a new skin", additionalContent); 

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
		
		public static void SelectSkinByPopup(UiStyleConfig _config)
		{
			var currentSkinAlias = _config.CurrentSkinAlias;
			_config.CurrentSkinAlias = GetSelectSkinPopup(_config, currentSkinAlias, out bool _);
		}
		
		// Draw a style in the inspector without the need to actually [SerializeReference] it (which totally bloats stuff)
		public static void DrawStyle(UiAbstractApplyStyleBase _applier, UiAbstractStyleBase _style)
		{
			EditorGUILayout.LabelField("Currently used Style:");
			if (_style == null)
			{
				EditorGUILayout.HelpBox("No Style assigned yet", MessageType.Warning);
				EditorGUILayout.Space(10);
				return;
			}
			
			_applier.SetSkinListeners(true);
			var styleHelper = StyleHelper;
			styleHelper.Style = _style;
			StyleHelperEditor.OnInspectorGUI();
			_applier.SetSkinListeners(!_applier.SkinIsFixed);
		}
		
        private static void DrawInspectorExcept(SerializedObject _serializedObject, string _fieldToSkip)
        {
            DrawInspectorExcept(_serializedObject, new [] { _fieldToSkip });
        }

        private static void DrawInspectorExcept(SerializedObject _serializedObject, string[] _fieldsToSkip)
        {
	        if (_serializedObject == null || _serializedObject.targetObject == null)
		        return;
	        
            _serializedObject.Update();
            SerializedProperty prop = _serializedObject.GetIterator();
            if (prop.NextVisible(true))
            {
                do
                {
                    if (_fieldsToSkip.Any(prop.name.Contains))
                        continue;

                    EditorGUILayout.PropertyField(_serializedObject.FindProperty(prop.name), true);
                }
                while (prop.NextVisible(false));
            }
            _serializedObject.ApplyModifiedProperties();
        }

		// both _name and _copyFromName have to be the actual names and not aliases
		private static string AddSkin(UiStyleConfig config, string _name, string _copyFromName)
		{
			if (config.SkinNames.Contains(_name))
				return string.Empty;

			var newSkin = new UiSkin(config, _name);

			UiSkin copyFrom = null;
			if (!string.IsNullOrEmpty(_copyFromName))
			{
				foreach (var skin in config.Skins)
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

			UiEventDefinitions.EvAddSkin.InvokeAlways(config, newSkin);
			UiStyleConfig.SetDirty(config);
			return _name;
		}
	}
}
