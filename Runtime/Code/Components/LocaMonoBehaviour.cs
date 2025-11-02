using System.Runtime.CompilerServices;
using UnityEngine;

namespace GuiToolkit
{
	/// <summary>
	/// Base MonoBehaviour with gettext-style localization helpers.
	/// 
	/// This class provides convenience wrappers around the LocaManager
	/// so that derived classes can use standard gettext naming conventions
	/// (e.g. "_" and "ngettext") directly inside Unity MonoBehaviours.
	/// 
	/// Note:
	/// - The "_" shortcut is unusual in C# but standard in gettext/PO/POT tools.
	/// - All functions delegate to LocaManager.Instance for actual translations.
	/// - Use "__" only to mark strings for POT creation without runtime translation.
	/// </summary>
	public class LocaMonoBehaviour : MonoBehaviour
	{
		/// <summary>
		/// Shortcut for gettext translation of a single string.
		/// The "_" name is standard in gettext conventions.
		/// </summary>
		/// <param name="_s">Source string (msgid) to translate.</param>
		/// <returns>Localized string.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static string _( string _s, string _group = null )
		{
			return gettext(_s, _group);
		}

		/// <summary>
		/// Translate a single string (singular form).
		/// This is the long-form version of "_".
		/// </summary>
		/// <param name="_s">Source string (msgid) to translate.</param>
		/// <returns>Localized string.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static string gettext( string _s, string _group = null )
		{
			return LocaManager.Instance.Translate(_s, _group);
		}

		/// <summary>
		/// Shortcut for plural form translation using gettext style.
		/// </summary>
		/// <param name="_singular">Singular source string.</param>
		/// <param name="_plural">Plural source string.</param>
		/// <param name="_n">Number used for pluralization rules.</param>
		/// <returns>Localized string in correct plural form.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static string _n( string _singular, string _plural, int _n, string _group = null )
		{
			return ngettext(_singular, _plural, _n, _group);
		}

		/// <summary>
		/// Translate plural form strings (singular/plural) according to language rules.
		/// This is the long-form version of "_n".
		/// </summary>
		/// <param name="_singular">Singular source string.</param>
		/// <param name="_plural">Plural source string.</param>
		/// <param name="_n">Number used for pluralization rules.</param>
		/// <returns>Localized string in correct plural form.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static string ngettext( string _singular, string _plural, int _n, string _group = null )
		{
			return LocaManager.Instance.Translate(_singular, _plural, _n, _group);
		}

		/// <summary>
		/// Mark a string for extraction into POT files without translating it at runtime.
		/// Useful for cases where you want a string to appear in the catalog
		/// but not be localized immediately.
		/// </summary>
		/// <param name="_s">Source string to be extracted.</param>
		/// <returns>Unmodified source string.</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		protected static string __( string _s, string _group = null )
		{
			return _s;
		}
	}
}
