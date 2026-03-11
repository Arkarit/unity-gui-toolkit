using UnityEngine;

namespace GuiToolkit
{
	/// <summary>
	/// Generic base class with gettext-style localization helper methods.
	/// 
	/// Similar to <see cref="LocaMonoBehaviour"/> but for non-MonoBehaviour classes.
	/// Provides convenience wrappers around <see cref="LocaManager"/> using standard gettext naming conventions.
	/// 
	/// Note: The "_" shortcut is unusual in C# but standard in gettext/PO/POT tools.
	/// </summary>
	public class LocaClass
	{
		/// <summary>
		/// Shortcut for gettext translation of a single string.
		/// The "_" name is standard in gettext conventions.
		/// </summary>
		/// <param name="_s">Source string (msgid) to translate.</param>
		/// <returns>Localized string.</returns>
		protected static string _(string _s)
		{
			return gettext(_s);
		}

		/// <summary>
		/// Context-aware translation shortcut (pgettext convention).
		/// Composes the lookup key as "context\u0004msgid".
		/// </summary>
		/// <param name="_s">Source string (msgid) to translate.</param>
		/// <param name="_context">Disambiguation context (msgctxt).</param>
		/// <param name="_group">Optional localization group.</param>
		/// <returns>Localized string.</returns>
		protected static string pgettext(string _s, string _context, string _group = null)
		{
			return LocaManager.Instance.Translate(_s, _context, _group);
		}

		/// <summary>
		/// Translate a single string (singular form).
		/// This is the long-form version of "_".
		/// </summary>
		/// <param name="_s">Source string (msgid) to translate.</param>
		/// <returns>Localized string.</returns>
		protected static string gettext(string _s)
		{
			return LocaManager.Instance.Translate(_s);
		}

		/// <summary>
		/// Shortcut for plural form translation using gettext style.
		/// </summary>
		/// <param name="_singular">Singular source string.</param>
		/// <param name="_plural">Plural source string.</param>
		/// <param name="_n">Number used for pluralization rules.</param>
		/// <returns>Localized string in correct plural form.</returns>
		protected static string _n(string _singular, string _plural, int _n)
		{
			return ngettext(_singular, _plural, _n);
		}

		/// <summary>
		/// Translate plural form strings (singular/plural) according to language rules.
		/// This is the long-form version of "_n".
		/// </summary>
		/// <param name="_singular">Singular source string.</param>
		/// <param name="_plural">Plural source string.</param>
		/// <param name="_n">Number used for pluralization rules.</param>
		/// <returns>Localized string in correct plural form.</returns>
		protected static string ngettext(string _singular, string _plural, int _n)
		{
			return LocaManager.Instance.Translate(_singular, _plural, _n);
		}
		
		/// <summary>
		/// Returns a language-appropriate ordinal suffix for a given number.
		/// Delegates to <see cref="LocaManager.GetOrdinal"/>.
		/// </summary>
		/// <param name="_languageId">The language identifier.</param>
		/// <param name="_number">The number to format.</param>
		/// <returns>Formatted ordinal string.</returns>
		protected static string GetOrdinal( string _languageId, int _number)
		{
			return LocaManager.Instance.GetOrdinal(_languageId, _number);
		}

		/// <summary>
		/// Mark a string for extraction into POT files without translating it at runtime.
		/// Useful for cases where you want a string to appear in the catalog
		/// but not be localized immediately.
		/// </summary>
		/// <param name="_s">Source string to be extracted.</param>
		/// <returns>Unmodified source string.</returns>
		protected static string __(string _s)
		{
			return _s;
		}
	}
}