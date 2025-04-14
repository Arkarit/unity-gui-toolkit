using System.Collections.Generic;
using UnityEditor;

namespace GuiToolkit.Editor
{
	[CustomEditor(typeof(UiLanguageToggle))]
	public class UiLanguageToggleEditor : UiThingEditor
	{
		private static readonly HashSet<string> m_excludedProperties = new()
		{
			"m_uiImage"
		};

		protected override HashSet<string> excludedProperties => m_excludedProperties;
		public override void OnInspectorGUI()
		{
			UiLanguageToggle thisUiLanguageToggle = (UiLanguageToggle)target;
			base.OnInspectorGUI();

			if (EditorLocaUtility.LanguagePopup("Select available language:", thisUiLanguageToggle.Language,
				    out string newLanguage))
			{
				thisUiLanguageToggle.Language = newLanguage;
				EditorUtility.SetDirty(thisUiLanguageToggle);
			}
		}
	}
}