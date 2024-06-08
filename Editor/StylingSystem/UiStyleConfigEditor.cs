using GuiToolkit.Style;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit.Editor
{
	[CustomEditor(typeof(UiStyleConfig), true)]
	public class UiStyleConfigEditor : UnityEditor.Editor
	{
		private SerializedProperty m_skinsProp;
		private UiStyleConfig m_thisUiStyleConfig;

		protected virtual void OnEnable()
		{
			m_thisUiStyleConfig = (UiStyleConfig)target;
			m_skinsProp = serializedObject.FindProperty("m_skins");
		}

		public override void OnInspectorGUI()
		{
			serializedObject.Update();

			var skins = m_thisUiStyleConfig.Skins.Clone();
			var oldCount = skins.Count;

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
				else
					m_thisUiStyleConfig.Index = newIndex;
			}

			serializedObject.ApplyModifiedProperties();
		}
	}
}
