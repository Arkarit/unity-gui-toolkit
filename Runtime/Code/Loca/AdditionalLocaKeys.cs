#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace GuiToolkit
{
	/// <summary>
	/// (Editor-only) ScriptableObject for declaring additional localization keys not found via code scanning.
	/// Useful for dynamically generated keys or keys referenced indirectly (e.g., from data files).
	/// Implements <see cref="ILocaKeyProvider"/> so the Loca processor can collect these keys for POT generation.
	/// </summary>
	[CreateAssetMenu()]
	public class AdditionalLocaKeys : ScriptableObject, ILocaKeyProvider
	{
		[FormerlySerializedAs("Keys")] [SerializeField] private List<string> m_keys;
		[SerializeField] private string m_group;

		/// <summary>
		/// Returns true, indicating this provider supplies multiple keys.
		/// </summary>
		public bool UsesMultipleLocaKeys => true;

		/// <summary>
		/// Returns null (not used for multi-key providers).
		/// </summary>
		public string LocaKey => null;

		/// <summary>
		/// Gets the list of additional localization keys to be included in POT generation.
		/// </summary>
		public List<string> LocaKeys => m_keys;

		/// <summary>
		/// Gets the localization group for these keys.
		/// </summary>
		public string Group => m_group;
	}
}
#endif