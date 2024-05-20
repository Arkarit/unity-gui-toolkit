using System;
using System.Collections.Generic;
using Codice.Client.GameUI.Checkin;
using GuiToolkit.Style;
using GuiToolkit.Style.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace GuiToolkit.Editor
{
	[CustomEditor(typeof(UiMainStyleConfig), true)]
	public class UiMainStyleConfigEditor : UnityEditor.Editor
	{
		private const float PerSkinGap = 20;

		private SerializedProperty m_skinsProp;
		private SerializedProperty m_currentSkinIdxProp;
		private UiMainStyleConfig m_thisUiMainStyleConfig;

		protected virtual void OnEnable()
		{
			m_skinsProp = serializedObject.FindProperty("m_skins");
			m_currentSkinIdxProp = serializedObject.FindProperty("m_currentSkinIdx");
			m_thisUiMainStyleConfig = target as UiMainStyleConfig;
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			Draw();
			EditorGUILayout.Space(10);

			var skinNames = UiMainStyleConfig.Instance.SkinNames;
			int numSkins = skinNames.Count;
			string copyFrom = skinNames.Count > 0 ? skinNames[0] : string.Empty;

			Action<EditorInputDialog> additionalContent = _ =>
			{
				if (string.IsNullOrEmpty(copyFrom))
					return;

				if (EditorUiUtility.StringPopup("Copy string from ", skinNames, copyFrom,
					    out copyFrom,
					    null, false) != -1)
				{
				}

				EditorGUILayout.Space(20);
			};

			if (EditorUiUtility.StringPopup("Current Skin", skinNames, UiMainStyleConfig.Instance.CurrentSkinName, out string selectedName,
				    null, false, "Add Skin", "Adds a new skin", additionalContent) != -1)
			{
				if (numSkins != skinNames.Count)
				{
					AddSkin(selectedName, copyFrom);
				}

				UiMainStyleConfig.Instance.CurrentSkinName = selectedName;
			}

			serializedObject.ApplyModifiedProperties();
		}

		private void Draw()
		{
			try
			{
				for (int i = 0; i < m_skinsProp.arraySize; i++)
				{
					var skinProp = m_skinsProp.GetArrayElementAtIndex(i);
					EditorGUILayout.PropertyField(skinProp);
					EditorGUILayout.Space(PerSkinGap);
				}
			} catch {}
		}

		private string AddSkin(string _name, string _copyFromName)
		{
			if (m_thisUiMainStyleConfig.SkinNames.Contains(_name))
				return string.Empty;

			var newSkin = new UiSkin(_name);

			UiSkin copyFrom = null;
			if (!string.IsNullOrEmpty(_copyFromName))
			{
				foreach (var skin in m_thisUiMainStyleConfig.Skins)
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
					var newStyle = style.Clone();
					newSkin.Styles.Add(newStyle);
				}
			}

			m_skinsProp.arraySize += 1;
			m_skinsProp.GetArrayElementAtIndex(m_skinsProp.arraySize - 1).boxedValue = newSkin;
			serializedObject.ApplyModifiedProperties();
			UiMainStyleConfig.Instance.EditorSave();
			return _name;
		}
	}
}
