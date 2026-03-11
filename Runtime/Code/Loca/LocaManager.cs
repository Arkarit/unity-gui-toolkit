using System.Globalization;
using System.Threading;
using UnityEngine;


namespace GuiToolkit
{
	/// <summary>
	/// Abstract base class for localization management using GNU gettext-style PO files.
	/// Manages translation loading, language switching, dynamic provider registration,
	/// and provides the core translation API for the entire UI toolkit.
	/// </summary>
	public abstract class LocaManager : IEditorAware
	{
		/// <summary>
		/// Defines behavior when a translation key is not found.
		/// </summary>
		public enum RetValIfNotFound
		{
			/// <summary>Returns the key itself (default, most visible for debugging).</summary>
			Key,
			/// <summary>Returns an empty string.</summary>
			EmptyString,
			/// <summary>Returns null.</summary>
			Null,
		}

		/// <summary>
		/// PlayerPrefs key used to persist the user's selected language across sessions.
		/// </summary>
		public const string PLAYER_PREFS_KEY = StringConstants.PLAYER_PREFS_PREFIX + "Language";

		/// <summary>
		/// Translates a singular string key to the currently active language.
		/// </summary>
		/// <param name="_key">The localization key (msgid).</param>
		/// <param name="_group">Optional group namespace for disambiguation. Null uses the default group.</param>
		/// <param name="_retValIfNotFound">Behavior when the key is not found.</param>
		/// <returns>Translated string, or a fallback based on <paramref name="_retValIfNotFound"/>.</returns>
		public abstract string Translate( string _key, string _group = null, RetValIfNotFound _retValIfNotFound = RetValIfNotFound.Key );
		
		/// <summary>
		/// Translates a pluralized string key according to the active language's plural rules.
		/// The appropriate form is chosen based on <paramref name="_n"/>.
		/// </summary>
		/// <param name="_singularKey">The singular form key (msgid).</param>
		/// <param name="_pluralKey">The plural form key (msgid_plural).</param>
		/// <param name="_n">The number determining which plural form to use.</param>
		/// <param name="_group">Optional group namespace for disambiguation.</param>
		/// <param name="_retValIfNotFound">Behavior when the key is not found.</param>
		/// <returns>Translated string in the appropriate plural form.</returns>
		public abstract string Translate( string _singularKey, string _pluralKey, int _n, string _group = null, RetValIfNotFound _retValIfNotFound = RetValIfNotFound.Key );
		
		/// <summary>
		/// Checks whether a translation key exists in the specified group for the current language.
		/// </summary>
		/// <param name="_key">The localization key to check.</param>
		/// <param name="_group">The group namespace to search in.</param>
		/// <returns>True if the key exists, false otherwise.</returns>
		public abstract bool HasKey(string _key, string _group);

		/// <summary>
		/// Translates <paramref name="_key"/> disambiguated by <paramref name="_context"/>.
		/// The composed lookup key follows the GNU gettext convention: "context\u0004msgid".
		/// Pass null or empty <paramref name="_context"/> to behave identically to
		/// <see cref="Translate(string,string,RetValIfNotFound)"/>.
		/// </summary>
		/// <param name="_key">The localization key (msgid).</param>
		/// <param name="_context">The disambiguation context (msgctxt). Empty/null for no context.</param>
		/// <param name="_group">Optional group namespace.</param>
		/// <param name="_retValIfNotFound">Behavior when the key is not found.</param>
		/// <returns>Translated string.</returns>
		public string Translate( string _key, string _context, string _group = null, RetValIfNotFound _retValIfNotFound = RetValIfNotFound.Key )
		{
			string composedKey = string.IsNullOrEmpty(_context) ? _key : $"{_context}\u0004{_key}";
			return Translate(composedKey, _group, _retValIfNotFound);
		}

		/// <summary>
		/// Implementation-specific language change logic.
		/// Called by <see cref="ChangeLanguage"/> after validation. Must load PO files and apply translations.
		/// </summary>
		/// <param name="_languageId">The target language identifier (e.g., "en", "de", "dev").</param>
		/// <returns>True if the language was successfully loaded, false otherwise.</returns>
		public abstract bool ChangeLanguageImpl( string _languageId );

		/// <summary>
		/// Registers a dynamic <see cref="ILocaProvider"/> (e.g. a DLC language pack).
		/// If a language is already active the provider is applied immediately.
		/// </summary>
		public virtual void RegisterProvider( ILocaProvider _provider ) { }

		/// <summary>
		/// Unregisters a previously registered dynamic provider.
		/// The provider's <see cref="ILocaProvider.Unload"/> method is called.
		/// Already-loaded translations are not removed; call
		/// <see cref="ChangeLanguage(string)"/> afterwards to refresh if needed.
		/// </summary>
		/// <param name="_provider">The provider to unregister.</param>
		public virtual void UnregisterProvider( ILocaProvider _provider ) { }

		/// <summary>
		/// Gets or sets the currently active language identifier (e.g., "en", "de", "dev").
		/// Setting this directly is not recommended; use <see cref="ChangeLanguage"/> instead.
		/// </summary>
		public string Language { get; protected set; } = null;

		/// <summary>
		/// Returns a language-appropriate ordinal suffix for a given number.
		/// Currently supports English (e.g., 1st, 2nd, 3rd, 4th) and defaults to "number." for other languages.
		/// </summary>
		/// <param name="_languageId">The language identifier.</param>
		/// <param name="_number">The number to format.</param>
		/// <returns>Formatted ordinal string.</returns>
		public string GetOrdinal( string _languageId, int _number )
		{
			switch (_languageId)
			{
				default:
					return $"{_number}.";

				case "en":
				case "en-us":
				case "dev":
					return GetEnglishOrdinal(_number);
			}
		}

#if UNITY_EDITOR
		/// <summary>
		/// (Editor-only) Gets the list of available language identifiers found in the project's PO assets.
		/// </summary>
		public abstract string[] EdAvailableLanguages { get; }
		
		/// <summary>
		/// (Editor-only) Clears all accumulated localization keys.
		/// Called at the start of the Loca processing pass.
		/// </summary>
		public abstract void EdClear();
		
		/// <summary>
		/// (Editor-only) Registers a localization key discovered during scanning.
		/// Keys are accumulated and written to POT files via <see cref="EdWriteKeyData"/>.
		/// </summary>
		/// <param name="_singularKey">The singular key (msgid).</param>
		/// <param name="_pluralKey">Optional plural key (msgid_plural). Null for singular-only entries.</param>
		/// <param name="_group">Optional group namespace. Null uses the default group.</param>
		public abstract void EdAddKey( string _singularKey, string _pluralKey = null, string _group = null );
		
		/// <summary>
		/// (Editor-only) Reads existing POT files to preserve keys that may not be found in the current scan.
		/// Called before the scanning pass starts.
		/// </summary>
		public abstract void EdReadKeyData();
		
		/// <summary>
		/// (Editor-only) Writes all accumulated localization keys to POT template files.
		/// Called after the scanning pass completes.
		/// </summary>
		public abstract void EdWriteKeyData();
#endif

		private bool m_debugLoca = false;
		/// <summary>
		/// Gets whether debug logging is enabled for localization operations.
		/// Configured via <see cref="UiToolkitConfiguration.DebugLoca"/>.
		/// </summary>
		public bool DebugLoca => m_debugLoca;

		private static LocaManager s_locaManager = null;
		/// <summary>
		/// Gets or sets the global localization manager instance.
		/// Defaults to <see cref="LocaManagerDefaultImpl"/> if not explicitly set.
		/// </summary>
		public static LocaManager Instance
		{
			get
			{
				if (s_locaManager == null)
					s_locaManager = new LocaManagerDefaultImpl();
				return s_locaManager;
			}
			set
			{
				s_locaManager = value;
			}
		}

		/// <summary>
		/// Changes the active language to <paramref name="_languageId"/>.
		/// Loads the corresponding PO file, applies registered providers, persists the choice to PlayerPrefs,
		/// and broadcasts <see cref="UiEventDefinitions.EvLanguageChanged"/>.
		/// Falls back to "dev" if the requested language cannot be loaded.
		/// </summary>
		/// <param name="_languageId">The target language identifier (e.g., "en", "de", "dev").</param>
		/// <returns>True if the requested language was successfully loaded, false if fallback was used.</returns>
		public bool ChangeLanguage( string _languageId )
		{
			return ChangeLanguage(_languageId, true);
		}

		protected LocaManager()
		{
			AssetReadyGate.WhenReady(() =>
			{
				m_debugLoca = UiToolkitConfiguration.Instance.DebugLoca;
				string language = PlayerPrefs.GetString(PLAYER_PREFS_KEY, "dev");
				ChangeLanguage(language, false);
			});
		}


		private bool ChangeLanguage( string _languageId, bool _invokeEvent )
		{
			if (string.IsNullOrWhiteSpace(_languageId))
			{
				_languageId = "dev";
			}

			if (Language == _languageId)
				return true;

			// Try desired language first, without committing state yet.
			if (ChangeLanguageImpl(_languageId))
			{
				CommitLanguage(_languageId, _invokeEvent);
				return true;
			}

			UiLog.LogWarning($"Language '{_languageId}' not found. Falling back to 'dev'.");

			// Fallback
			const string fallback = "dev";
			if (Language == fallback)
			{
				// Already at fallback; still notify if requested.
				if (_invokeEvent)
					UiEventDefinitions.EvLanguageChanged.Invoke(fallback);
				return false;
			}

			if (ChangeLanguageImpl(fallback))
			{
				CommitLanguage(fallback, _invokeEvent);
				return false; // indicates we fell back
			}

			UiLog.LogError("Fallback language 'dev' could not be set.");
			return false;
		}

		private void CommitLanguage( string _finalLanguageId, bool _invokeEvent )
		{
			Language = _finalLanguageId;

			SetCulture(_finalLanguageId);

			PlayerPrefs.SetString(PLAYER_PREFS_KEY, Language);
			// Optional: PlayerPrefs.Save();

			if (_invokeEvent)
				UiEventDefinitions.EvLanguageChanged.Invoke(Language);
		}

		private static void SetCulture( string _languageId )
		{
			try
			{
				var ci = new CultureInfo(_languageId);
				Thread.CurrentThread.CurrentCulture = ci;
				Thread.CurrentThread.CurrentUICulture = ci;
			}
			catch (CultureNotFoundException)
			{
				UiLog.LogWarning($"Culture '{_languageId}' not found, setting default culture");
				try
				{
					var ci = CultureInfo.InvariantCulture;
					Thread.CurrentThread.CurrentCulture = ci;
					Thread.CurrentThread.CurrentUICulture = ci;
				}
				catch (CultureNotFoundException)
				{
					UiLog.LogError("Could not set default culture.");
				}
			}
		}

		private string GetEnglishOrdinal( int _number )
		{
			int n = _number < 0 ? -_number : _number;

			int lastTwo = n % 100;
			if (lastTwo >= 11 && lastTwo <= 13)
				return _number.ToString() + "th";

			switch (n % 10)
			{
				case 1: return _number.ToString() + "st";
				case 2: return _number.ToString() + "nd";
				case 3: return _number.ToString() + "rd";
				default: return _number.ToString() + "th";
			}
		}
	}
}