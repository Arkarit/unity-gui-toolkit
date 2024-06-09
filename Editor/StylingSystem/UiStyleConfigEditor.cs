using System.Data;
using GuiToolkit.Style;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Editor
{
	[CustomEditor(typeof(UiStyleConfig), true)]
	public class UiStyleConfigEditor : UnityEditor.Editor
	{
		private SerializedProperty m_skinsProp;
		private SerializedProperty m_indexProp;
		private UiStyleConfig m_thisUiStyleConfig;

		protected virtual void OnEnable()
		{
			m_thisUiStyleConfig = (UiStyleConfig)target;
			m_skinsProp = serializedObject.FindProperty("m_skins");
			m_indexProp = serializedObject.FindProperty("m_index");
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			var skins = m_thisUiStyleConfig.Skins.Clone();
			var oldCount = skins.Count;
			try
			{
				bool skinChanged = EditorUiUtility.StringPopup
				(
					"Skins",
					skins,
					m_thisUiStyleConfig.Skin,
					out string newSelection,
					out int newIndex,
					null,
					true,
					"Add new skin",
					"Add new skin to the list of skins. It needs to have a unique name."
				);

				if (skinChanged)
				{
					var count = skins.Count;
					if (count > oldCount)
						m_thisUiStyleConfig.AddSkin(newSelection);
					else if (count < oldCount)
						m_thisUiStyleConfig.RemoveCurrentSkin();
					
					m_thisUiStyleConfig.Index = newIndex;
					EditorUtility.SetDirty(m_thisUiStyleConfig);
				}
			}
			catch (DuplicateNameException e)
			{
				EditorApplication.delayCall += () => EditorUtility.DisplayDialog("Duplicate Name", e.Message, "OK");
				return;
			}

			serializedObject.ApplyModifiedProperties();
		}
	}
}
