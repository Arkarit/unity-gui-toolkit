// Note: don't move this file to an Editor folder, since it needs to be available
// for inplace editor code
#if UNITY_EDITOR
using System.Linq;

namespace GuiToolkit
{
	public static class EditorLocaUtility
	{
		public static bool LanguagePopup( string _labelText, string _current, out string _new, string _labelText2 = " " )
		{
			var languages = LocaManager.Instance.EdAvailableLanguages.ToList();
			return EditorUiUtility.StringPopup(_labelText, languages, _current, out _new, _labelText2) != -1;
		}
	}
}
#endif