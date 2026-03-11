namespace GuiToolkit
{
	/// <summary>
	/// Static class for determining language-specific plural form rules.
	/// 
	/// This class consists of an internal part (defining the API) and a generated part
	/// (implementing <see cref="GetPluralIdx(string, int, ref int, ref int)"/> with language-specific rules).
	/// The generated implementation is created by <see cref="GuiToolkit.Editor.LocaPluralProcessor"/>
	/// by parsing "Plural-Forms:" headers from PO files.
	/// 
	/// The partial class feature allows static extension without inheritance.
	/// </summary>
	public static partial class LocaPlurals
	{
		/// <summary>
		/// Determines the plural form index for a given number in the specified language.
		/// Uses language-specific plural rules extracted from PO files.
		/// Falls back to English rules (n != 1) if the language is not recognized.
		/// </summary>
		/// <param name="_languageId">The language identifier (e.g., "en", "de", "pl").</param>
		/// <param name="_number">The number to evaluate.</param>
		/// <returns>A tuple of (numPluralForms, pluralIdx) where numPluralForms is the total number of forms defined for the language, and pluralIdx is the zero-based index of the form to use.</returns>
		public static (int numPluralForms, int pluralIdx) GetPluralIdx(string _languageId, int _number)
		{
			if (string.IsNullOrEmpty(_languageId))
			{
				UnityEngine.Debug.LogWarning("[Loca] GetPluralIdx called with null/empty language. Using English fallback.");
				return (2, _number != 1 ? 1 : 0);
			}

			int numPluralForms = 0;
			int pluralIdx = 0;

			GetPluralIdx(_languageId, _number, ref numPluralForms, ref pluralIdx);

			return (numPluralForms, pluralIdx);
		}

		static partial void GetPluralIdx(string _languageId, int _number, ref int _numPluralForms, ref int _pluralIdx);
	}
}
