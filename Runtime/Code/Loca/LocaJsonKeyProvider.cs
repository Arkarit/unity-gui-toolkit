#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace GuiToolkit
{
	/// <summary>
	/// (Editor-only) ScriptableObject that harvests localization keys from JSON files.
	/// Each entry points to a JSON <see cref="TextAsset"/> and lists the property names whose
	/// string values should be treated as translatable keys.  The property names are searched
	/// recursively throughout the entire JSON structure, so nested arrays and objects are handled
	/// automatically.
	/// <para>
	/// At runtime the game code is responsible for passing the JSON values through <c>_()</c>
	/// (or <see cref="LocaManager.Instance.Translate"/>) so that the translation from the PO files
	/// is applied. This ScriptableObject itself only participates in editor-time key harvesting via
	/// <see cref="ILocaKeyProvider"/>.
	/// </para>
	/// </summary>
	/// <example>
	/// Create an asset via <em>Assets &gt; Create &gt; Loca &gt; JSON Key Provider</em>, then assign
	/// the JSON file and the field names you want to translate (e.g. <c>tutorialText</c>,
	/// <c>title</c>).  The Loca processor will pick this asset up automatically the next time you
	/// run <em>Tools &gt; Loca &gt; Process Loca Keys</em>.
	/// </example>
	[CreateAssetMenu(fileName = "JsonKeyProvider", menuName = "Loca/JSON Key Provider")]
	public class LocaJsonKeyProvider : ScriptableObject, ILocaKeyProvider
	{
		/// <summary>
		/// One entry in the provider: a reference to a JSON file and the field names to extract.
		/// </summary>
		[Serializable]
		public class Entry
		{
			/// <summary>JSON TextAsset to scan.</summary>
			public TextAsset JsonFile;

			/// <summary>
			/// Property names to search for recursively in the JSON graph.
			/// All string values found under these property names are harvested as loca keys.
			/// </summary>
			[Tooltip("Property names searched recursively throughout the entire JSON graph (e.g. \"tutorialText\", \"title\")")]
			public string[] FieldNames;
		}

		[SerializeField] private Entry[] m_entries;
		[SerializeField] private string m_group;

		// -----------------------------------------------------------------------
		// ILocaKeyProvider
		// -----------------------------------------------------------------------

		/// <inheritdoc/>
		public bool UsesLocaKey => true;

		/// <inheritdoc/>
		public bool UsesMultipleLocaKeys => true;

		/// <inheritdoc/>
		public string LocaKey => null;

		/// <inheritdoc/>
		public List<string> LocaKeys => ExtractAllKeys();

		/// <inheritdoc/>
		public string Group => m_group;

		// -----------------------------------------------------------------------
		// Key extraction
		// -----------------------------------------------------------------------

		private List<string> ExtractAllKeys()
		{
			var keys = new List<string>();

			if (m_entries == null)
				return keys;

			foreach (var entry in m_entries)
			{
				if (entry?.JsonFile == null || entry.FieldNames == null || entry.FieldNames.Length == 0)
					continue;

				ExtractKeysFromEntry(entry, keys);
			}

			return keys;
		}

		private static void ExtractKeysFromEntry( Entry _entry, List<string> _keys )
		{
			JToken root;
			try
			{
				root = JToken.Parse(_entry.JsonFile.text);
			}
			catch (Exception ex)
			{
				UiLog.LogError($"{nameof(LocaJsonKeyProvider)}: Failed to parse JSON '{_entry.JsonFile.name}': {ex.Message}");
				return;
			}

			var fieldNameSet = new HashSet<string>(_entry.FieldNames, StringComparer.Ordinal);
			CollectValues(root, fieldNameSet, _keys);
		}

		/// <summary>
		/// Recursively walks the JSON token tree and collects string values of matching property names.
		/// </summary>
		private static void CollectValues( JToken _token, HashSet<string> _fieldNames, List<string> _keys )
		{
			switch (_token.Type)
			{
				case JTokenType.Object:
					foreach (var prop in ((JObject) _token).Properties())
					{
						if (_fieldNames.Contains(prop.Name))
							AddIfTranslatable(prop.Value, _keys);
						else
							CollectValues(prop.Value, _fieldNames, _keys);
					}
					break;

				case JTokenType.Array:
					foreach (var child in (JArray) _token)
						CollectValues(child, _fieldNames, _keys);
					break;

				// Primitive types: nothing to recurse into
			}
		}

		/// <summary>
		/// Adds a JSON value to the key list if it is a non-empty string that is not a pure number.
		/// </summary>
		private static void AddIfTranslatable( JToken _value, List<string> _keys )
		{
			// Recurse into arrays under a matching key (e.g. an array of translatable strings)
			if (_value.Type == JTokenType.Array)
			{
				foreach (var item in (JArray) _value)
					AddIfTranslatable(item, _keys);
				return;
			}

			if (_value.Type != JTokenType.String)
				return;

			string text = _value.Value<string>();
			if (string.IsNullOrWhiteSpace(text))
				return;

			// Skip pure number strings and very short fragments (e.g. version codes)
			if (double.TryParse(text, out _))
				return;

			_keys.Add(text);
		}
	}
}
#endif
