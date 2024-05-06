using System;
using System.Collections.Generic;
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

		protected virtual void OnEnable()
		{
			m_skinsProp = serializedObject.FindProperty("m_skins");
			m_currentSkinIdxProp = serializedObject.FindProperty("m_currentSkinIdx");
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			Draw();
/*
			if (GUILayout.Button("Sync Styles"))
				SyncStyles(false);
			if (GUILayout.Button("Replace Styles"))
				SyncStyles(true);
*/
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
				
				EditorGUILayout.PropertyField(m_currentSkinIdxProp);
				EditorGUILayout.Space(50);

			} catch {}
		}
/*
		private void SyncStyles(bool reset)
		{
			Dictionary<int, UiAbstractApplyStyleBase> applianceComponentByKey = new();
			EditorUiUtility.FindAllComponentsInAllAssets<UiAbstractApplyStyleBase>(component =>
			{
				if (!applianceComponentByKey.ContainsKey(component.Key))
					applianceComponentByKey.Add(component.Key, component);
			});

			var thisUiMainStyleConfig = target as UiMainStyleConfig;
			var skins = thisUiMainStyleConfig.Skins;

			bool configWasChanged = false;

			foreach (var skin in skins)
			{
				if (reset)
					skin.Styles.Clear();

				List<UiAbstractStyleBase> newStyles = new();

				foreach (var kv in applianceComponentByKey)
				{
					var key = kv.Key;
					var applyStyleComponent = kv.Value;

					if (skin.StyleByKey(key) == null)
					{
						var newStyle = applyStyleComponent.CreateStyle();
						newStyles.Add(newStyle);
					}
				}

				if (newStyles.Count > 0)
				{
					skin.Styles.AddRange(newStyles);
					configWasChanged = true;
				}
			}

			if (configWasChanged)
			{
				UiMainStyleConfig.EditorSave(thisUiMainStyleConfig);
			}
		}
*/
	}
}
