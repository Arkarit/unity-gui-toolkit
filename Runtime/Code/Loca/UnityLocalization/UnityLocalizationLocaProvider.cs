using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

namespace GuiToolkit
{
	/// <summary>
	/// Feeds the toolkit's localization from Unity's Localization package, so a project can keep its
	/// strings in String Tables and still use <c>@loca:</c> keys, <c>_()</c> and everything else that
	/// resolves through <see cref="LocaManager"/>.
	/// </summary>
	/// <remarks>
	/// For the common case of a project that already has Unity Localization and then adopts this toolkit
	/// — or runs both UI stacks side by side. Without a bridge, that project has two catalogs and an
	/// undrawn border: an authored prefab resolves its key against the PO files while the game's code
	/// looks the same key up in a String Table, and BOTH systems fall back to returning the key on a
	/// miss. So each half fails silently, and in the authoring language the result looks correct.
	///
	/// The point is to bridge rather than migrate. Nothing is re-keyed and nothing is copied into the PO
	/// files as data: the String Tables stay the single source of truth for their strings, and the
	/// toolkit becomes the API in front of them.
	///
	/// Lives in its own assembly, constrained to <c>GUITOOLKIT_HAS_UNITY_LOCALIZATION</c> (set from a
	/// version define on <c>com.unity.localization</c>), so a project without the package compiles as if
	/// this file did not exist.
	///
	/// Usage: create the asset under <c>Assets/Resources/LocaJson/</c> (see
	/// <see cref="LocaProviderList.RESOURCES_SUB_PATH"/>) and run the loca processor once. It writes the
	/// provider registry that <see cref="LocaManager"/> reads at runtime — the asset does not register
	/// itself, and an asset outside Resources cannot be loaded at all.
	/// </remarks>
	[CreateAssetMenu(fileName = nameof(UnityLocalizationLocaProvider),
		menuName = StringConstants.LOCA_UNITY_LOCALIZATION_PROVIDER)]
	public class UnityLocalizationLocaProvider : ScriptableObject, ILocaProvider
	{
		[Tooltip("String Table collection names to read, in order. A later table wins on a duplicate key, " +
		         "so put the more specific ones last.")]
		[SerializeField] private List<string> m_tableNames = new() { "UI" };

		[Tooltip("Toolkit loca group the entries land in. Empty = the default group, which is what " +
		         "@loca: keys without a group resolve against.")]
		[SerializeField] private string m_group = "";

		[Tooltip("Register the table's keys with the loca processor, so they end up in the .pot template.\n\n" +
		         "On (recommended when this toolkit owns localization): the toolkit's own tooling can then " +
		         "SEE these keys — most usefully, the screen baker's check for unresolved @loca: keys stops " +
		         "reporting every one of them as missing. Only the keys travel; the translations stay in the " +
		         "String Tables.\n\n" +
		         "Off: nothing is written, and edit-time key checks cannot know about these keys.")]
		[SerializeField] private bool m_contributeKeysToPot = true;

		private ProcessedLoca m_localization = new();

		/// <inheritdoc/>
		public ProcessedLoca Localization => m_localization;

		/// <inheritdoc/>
		/// <remarks>
		/// Entries are tagged with the language the caller ASKED for, not with Unity's locale code. The
		/// manager matches them by a plain lowercase comparison, so a table under "en-US" would never be
		/// found by a toolkit language of "en" — tagging by request sidesteps the mismatch entirely, and
		/// the manager filters to the current language anyway.
		/// </remarks>
		public void Load( string _language )
		{
			m_localization = new ProcessedLoca(m_group, new List<ProcessedLocaEntry>());

			if (string.IsNullOrEmpty(_language) || !LocalizationSettings.HasSettings)
				return;

			var locale = FindLocale(_language);
			if (locale == null)
			{
				UiLog.LogWarning($"[{nameof(UnityLocalizationLocaProvider)}] No Unity locale matches language " +
					$"'{_language}'; no entries contributed. Add the locale to the project's Available Locales, " +
					"or expect the toolkit's own catalog to answer for this language.");
				return;
			}

			var entries = m_localization.Entries;
			foreach (var tableName in m_tableNames)
			{
				if (string.IsNullOrEmpty(tableName))
					continue;

				var table = LocalizationSettings.StringDatabase.GetTable(tableName, locale);
				if (table == null)
				{
					UiLog.LogWarning($"[{nameof(UnityLocalizationLocaProvider)}] String table '{tableName}' " +
						$"not found for locale '{locale.Identifier.Code}'; skipped.");
					continue;
				}

				foreach (var entry in table.Values)
				{
					if (entry == null || string.IsNullOrEmpty(entry.Key))
						continue;

					// Value, not LocalizedValue: the raw string keeps "{0}" intact, and formatting is the
					// caller's job on this side of the bridge.
					string text = entry.Value;
					if (string.IsNullOrEmpty(text))
						continue;

					entries.Add(new ProcessedLocaEntry
					{
						Key = entry.Key,
						LanguageId = _language,
						Text = text,
					});
				}
			}
		}

		/// <inheritdoc/>
		public void Unload() => m_localization = new ProcessedLoca(m_group, new List<ProcessedLocaEntry>());

		/// <summary>
		/// The locale for a toolkit language id: an exact code match, else one whose base language matches.
		/// </summary>
		/// <remarks>
		/// The fallback is what makes "de" find a project that only ships "de-DE". Deliberately not the
		/// other way round — picking a specific regional locale for a bare language is a reasonable guess,
		/// while answering a request for "de-AT" with "de-DE" is a decision the project should make itself.
		/// </remarks>
		private static Locale FindLocale( string _language )
		{
			var locales = LocalizationSettings.AvailableLocales?.Locales;
			if (locales == null)
				return null;

			foreach (var locale in locales)
			{
				if (locale != null && string.Equals(locale.Identifier.Code, _language,
					System.StringComparison.OrdinalIgnoreCase))
				{
					return locale;
				}
			}

			foreach (var locale in locales)
			{
				if (locale == null)
					continue;

				string code = locale.Identifier.Code;
				int dash = code?.IndexOf('-') ?? -1;
				if (dash > 0 && string.Equals(code.Substring(0, dash), _language,
					System.StringComparison.OrdinalIgnoreCase))
				{
					return locale;
				}
			}

			return null;
		}

#if UNITY_EDITOR
		/// <inheritdoc/>
		/// <remarks>
		/// Registers the table's KEYS with the loca processor. Keys only — the translations are not copied,
		/// because two places holding the same string is how they drift apart.
		///
		/// What this buys: every edit-time check that asks "is this a known loca key" starts answering
		/// correctly for String Table keys too. Without it the screen baker flags each of them as missing,
		/// which trains an author to ignore that warning — and it is a warning worth reading.
		///
		/// Keys are language-independent (they live in the shared table data), so one locale is enough to
		/// enumerate them.
		/// </remarks>
		public void CollectData()
		{
			if (!m_contributeKeysToPot || !LocalizationSettings.HasSettings)
				return;

			var locales = LocalizationSettings.AvailableLocales?.Locales;
			if (locales == null || locales.Count == 0)
				return;

			int added = 0;
			foreach (var tableName in m_tableNames)
			{
				if (string.IsNullOrEmpty(tableName))
					continue;

				StringTable table = null;
				foreach (var locale in locales)
				{
					if (locale == null)
						continue;
					table = LocalizationSettings.StringDatabase.GetTable(tableName, locale);
					if (table != null)
						break;
				}

				var shared = table?.SharedData;
				if (shared?.Entries == null)
					continue;

				string sourceRef = $"{nameof(UnityLocalizationLocaProvider)}:{tableName}";
				foreach (var sharedEntry in shared.Entries)
				{
					if (sharedEntry == null || string.IsNullOrEmpty(sharedEntry.Key))
						continue;

					LocaManager.Instance.EdAddKey(sharedEntry.Key, null, m_group, sourceRef);
					added++;
				}
			}

			if (added > 0)
			{
				UiLog.Log($"[{nameof(UnityLocalizationLocaProvider)}] contributed {added} key(s) from " +
					$"{m_tableNames.Count} String Table(s) to the loca key set.", this);
			}
		}
#endif
	}
}
