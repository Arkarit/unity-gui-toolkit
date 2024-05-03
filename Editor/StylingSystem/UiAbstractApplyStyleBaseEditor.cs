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

			var styleNames = UiMainStyleConfig.Instance.StyleNames;
			int countBefore = styleNames.Count;
			if (EditorUiUtility.StringPopup("Style", styleNames, m_nameProp.stringValue, out string newName,
				    null, false, "Add Style", "Adds a new style"))
			{
				if (styleNames.Count > countBefore)
				{
					UiMainStyleConfig.Instance.ForeachSkin(skin =>
					{
						var newStyle = m_thisAbstractApplyStyleBase.CreateStyle(newName);
						skin.Styles.Add(newStyle);
					});

					UiMainStyleConfig.EditorSave(UiMainStyleConfig.Instance);
				}

				m_thisAbstractApplyStyleBase.Name = newName;
				EditorUtility.SetDirty(m_thisAbstractApplyStyleBase);
			}

			DrawDefaultInspector();
			serializedObject.ApplyModifiedProperties();
		}
	}
}
