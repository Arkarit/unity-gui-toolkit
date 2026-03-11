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