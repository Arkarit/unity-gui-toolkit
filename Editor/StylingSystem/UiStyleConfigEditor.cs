using System;
using GuiToolkit.Style;
using UnityEditor;

namespace GuiToolkit.Editor
{
	[CustomEditor(typeof(UiStyleConfig), true)]
	public class UiStyleConfigEditor : UnityEditor.Editor
	{
		private const float PerSkinGap = 20;

		private SerializedProperty m_skinsProp;
		private SerializedProperty m_currentSkinIdxProp;
		private UiStyleConfig m_thisUiStyleConfig;

		private static string m_filterString = string.Empty;

		public static string DisplayFilter => m_filterString;



		protected virtual void OnEnable()
		{
			m_skinsProp = serializedObject.FindProperty("m_skins");
			m_currentSkinIdxProp = serializedObject.FindProperty("m_currentSkinIdx");
			m_thisUiStyleConfig = target as UiStyleConfig;
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			var skinNames = UiStyleConfig.Instance.SkinNames;
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

			if (EditorUiUtility.StringPopup("Current Skin", skinNames, UiStyleConfig.Instance.CurrentSkinName, out string selectedName,
				    null, false, "Add Skin", "Adds a new skin", additionalContent) != -1)
			{
				if (numSkins != skinNames.Count)
				{
					AddSkin(selectedName, copyFrom);
				}

				UiStyleConfig.Instance.CurrentSkinName = selectedName;
			}

			EditorGUILayout.Space(10);
			m_filterString = EditorGUILayout.TextField("Style display filter", m_filterString);

			EditorGUILayout.Space(10);
			Draw();


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
			if (m_thisUiStyleConfig.SkinNames.Contains(_name))
				return string.Empty;

			var newSkin = new UiSkin(_name);

			UiSkin copyFrom = null;
			if (!string.IsNullOrEmpty(_copyFromName))
			{
				foreach (var skin in m_thisUiStyleConfig.Skins)
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

			m_skinsProp.arraySize += 1;
			m_skinsProp.GetArrayElementAtIndex(m_skinsProp.arraySize - 1).boxedValue = newSkin;
			serializedObject.ApplyModifiedProperties();
			UiStyleConfig.EditorSave(UiStyleConfig.Instance);
			return _name;
		}
	}
}
