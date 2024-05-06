using System.Collections.Generic;
using GuiToolkit.Style;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Editor
{
	[CustomEditor(typeof(UiAbstractApplyStyleBase), true)]
	public class UiAbstractApplyStyleBaseEditor : UnityEditor.Editor
	{
		private UiAbstractApplyStyleBase m_thisAbstractApplyStyleBase;
		SerializedProperty 	m_nameProp;
		SerializedProperty m_styleProp;

		protected virtual void OnEnable()
		{
			m_thisAbstractApplyStyleBase = target as UiAbstractApplyStyleBase;
			m_nameProp = serializedObject.FindProperty("m_name");
			m_styleProp = serializedObject.FindProperty("m_style");
		}

		public override void OnInspectorGUI()
		{
			if (m_thisAbstractApplyStyleBase.Style == null)
				m_thisAbstractApplyStyleBase.SetStyle();

			string selectedName;

			EditorGUILayout.LabelField("Local Settings", EditorStyles.boldLabel);
			var styleNames = UiMainStyleConfig.Instance.GetStyleNamesByMonoBehaviourType(m_thisAbstractApplyStyleBase.SupportedMonoBehaviourType);
			int styleCountBefore = styleNames.Count;
			if (EditorUiUtility.StringPopup("Style", styleNames, m_nameProp.stringValue, out selectedName,
				    null, false, "Add Style", "Adds a new style"))
			{
				if (styleNames.Count > styleCountBefore)
				{
					UiMainStyleConfig.Instance.ForeachSkin(skin =>
					{
						var newStyle = m_thisAbstractApplyStyleBase.CreateStyle(selectedName);
						newStyle.Init();
						skin.Styles.Add(newStyle);
					});

					UiMainStyleConfig.EditorSave(UiMainStyleConfig.Instance);
				}

				m_thisAbstractApplyStyleBase.Name = selectedName;
				m_thisAbstractApplyStyleBase.Apply();
				EditorUtility.SetDirty(m_thisAbstractApplyStyleBase);
			}

			EditorGUILayout.Space(10);
			EditorGUILayout.LabelField("Global Settings", EditorStyles.boldLabel);

			var skinNames = UiMainStyleConfig.Instance.SkinNames;
			if (EditorUiUtility.StringPopup("Current Skin", skinNames, UiMainStyleConfig.Instance.CurrentSkinName, out selectedName,
				    null, false, "Add Skin", "Adds a new skin"))
			{
				UiMainStyleConfig.Instance.CurrentSkinName = selectedName;
			}

			DrawDefaultInspector();
			serializedObject.ApplyModifiedProperties();
		}
	}
}
