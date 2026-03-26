using System.Collections.Generic;
using UnityEngine;

namespace GuiToolkit
{
	/// <summary>
	/// (Editor-only) Interface for components and ScriptableObjects that provide localization keys.
	/// Used by the Loca processor tool to scan scenes, prefabs, and assets for translatable strings.
	/// </summary>
	public interface ILocaKeyProvider
	{
#if UNITY_EDITOR
		/// <summary>
		/// Returns true if this provider actively participates in key collection.
		/// When false, the Loca processor skips this component entirely.
		/// For example, <see cref="UiLocalizedTextMeshProUGUI"/> returns <c>m_autoLocalize</c>
		/// so that non-localizing text components don't pollute the key database.
		/// </summary>
		bool UsesLocaKey {get;}

		/// <summary>
		/// Returns true if this provider supplies multiple keys via <see cref="LocaKeys"/>,
		/// false if it supplies a single key via <see cref="LocaKey"/>.
		/// </summary>
		bool UsesMultipleLocaKeys {get;}
		
		/// <summary>
		/// The single localization key provided by this object.
		/// Only used when <see cref="UsesMultipleLocaKeys"/> is false.
		/// </summary>
		string LocaKey {get;}
		
		/// <summary>
		/// The list of localization keys provided by this object.
		/// Only used when <see cref="UsesMultipleLocaKeys"/> is true.
		/// </summary>
		List<string> LocaKeys {get;}
		
		/// <summary>
		/// The optional localization group namespace for these keys.
		/// </summary>
		string Group {get;}
#endif
	}
}