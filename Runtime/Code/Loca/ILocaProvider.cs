using System;
using System.Collections.Generic;

namespace GuiToolkit
{
	/// <summary>
	/// Container for processed localization data from a single source (e.g., an Excel sheet or PO file set).
	/// Represents one group with all its language entries.
	/// </summary>
	[Serializable]
	public sealed class ProcessedLoca
	{
		/// <summary>
		/// The localization group name. Empty string is treated as the default group.
		/// </summary>
		public string Group = string.Empty;
		
		/// <summary>
		/// The list of processed localization entries. Each entry contains a key, language, and translation.
		/// </summary>
		public List<ProcessedLocaEntry> Entries = new();
		
		public ProcessedLoca() {}
		public ProcessedLoca(string _group, List<ProcessedLocaEntry>  _entries)
		{
			Group = _group;
			Entries = _entries;
			SortByKey();
		}

		private void SortByKey()
		{
			if (Entries == null || Entries.Count <= 1)
				return;

			Entries.Sort(static ( a, b ) =>
			{
				// null checks first
				if (a == null && b == null) return 0;
				if (a == null) return 1;
				if (b == null) return -1;

				// empty keys after non-empty
				bool aEmpty = string.IsNullOrEmpty(a.Key);
				bool bEmpty = string.IsNullOrEmpty(b.Key);
				if (aEmpty && bEmpty) return 0;
				if (aEmpty) return 1;
				if (bEmpty) return -1;

				int keyCompare = string.Compare(a.Key, b.Key, StringComparison.Ordinal);
				if (keyCompare != 0)
					return keyCompare;

				// secondary: LanguageId
				return string.Compare(a.LanguageId, b.LanguageId, StringComparison.Ordinal);
			});
		}
	}

	/// <summary>
	/// Represents a single localization entry: one key in one language with its translation(s).
	/// Supports both singular and plural forms, optional context, and PO file metadata (fuzzy, comments, source refs).
	/// </summary>
	[Serializable]
	public sealed class ProcessedLocaEntry
	{
		/// <summary>The localization key (msgid).</summary>
		public string Key;
		
		/// <summary>
		/// Optional disambiguation context (msgctxt). Null or empty means no context.
		/// When present, the effective lookup key becomes "Context\u0004Key".
		/// </summary>
		public string Context;
		
		/// <summary>The language identifier for this entry (e.g., "en", "de").</summary>
		public string LanguageId;
		
		/// <summary>The translated text for singular form. Null or empty if only plural forms are defined.</summary>
		public string Text;
		
		/// <summary>
		/// Translated plural forms (msgstr[0], msgstr[1], ...). Up to 6 forms supported.
		/// Null if this entry has no plurals.
		/// </summary>
		public string[] Forms;
		
		/// <summary>
		/// True if this entry is marked fuzzy in the PO file (needs translator review).
		/// Fuzzy entries may still be used but typically indicate outdated or uncertain translations.
		/// </summary>
		public bool IsFuzzy;
		
		/// <summary>
		/// Translator-extracted comment from the PO file (#. comment).
		/// Provides context or notes for translators.
		/// </summary>
		public string TranslatorComment;
		
		/// <summary>
		/// Source reference from the PO file (#: source reference).
		/// Typically file:line pairs indicating where this key is used in source code.
		/// </summary>
		public string SourceRef;
	}

	/// <summary>
	/// Interface for dynamically loadable localization data sources (e.g., Excel bridges, JSON files, DLC packs).
	/// Providers can be registered via <see cref="LocaManager.RegisterProvider"/> to extend available translations at runtime.
	/// </summary>
	public interface ILocaProvider
	{
		/// <summary>
		/// Gets the processed localization data provided by this source.
		/// Must return a valid <see cref="ProcessedLoca"/> instance; never null.
		/// </summary>
		public ProcessedLoca Localization { get; }

		/// <summary>
		/// Called when this provider should load data for <paramref name="_language"/>.
		/// Invoked on <see cref="LocaManager.RegisterProvider"/> (with the current language) and
		/// whenever the language changes while the provider is registered.
		/// Default implementation is a no-op.
		/// </summary>
		public void Load(string _language) { }

		/// <summary>
		/// Called when this provider is unregistered via <see cref="LocaManager.UnregisterProvider"/>.
		/// Default implementation is a no-op.
		/// </summary>
		public void Unload() { }

#if UNITY_EDITOR
		/// <summary>
		/// (Editor-only) Collects or refreshes localization data from the provider's source (e.g., downloads and parses an Excel file).
		/// Called by the Loca processor tool to regenerate translations before packaging.
		/// </summary>
		public void CollectData();
#endif
	}
}
