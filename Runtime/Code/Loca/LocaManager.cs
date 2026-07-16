using System.Collections.Generic;
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
		/// Resource name for the generated file that lists available language IDs (one per line).
		/// Written by the editor tools, read at runtime by <see cref="GetAvailableLanguages"/>.
		/// </summary>
		public const string AVAILABLE_LANGUAGES_RESOURCE = "uitk_available_languages";

		/// <summary>
		/// Converts a language identifier to the canonical BCP 47 form used throughout the toolkit:
		/// all-lowercase with hyphens as subtag separators (e.g. <c>zh-tw</c>, <c>pt-br</c>).
		/// Underscores are replaced with hyphens and the string is lowercased.
		/// A warning is logged when the input is not already in canonical form, so that callers
		/// (PO file names, Excel column headers, code) can be corrected at the source.
		/// </summary>
		/// <param name="_languageId">The raw language identifier to normalize.</param>
		/// <returns>Canonical lowercase-hyphen language ID.</returns>
		public static string NormalizeLanguageId( string _languageId )
		{
			if (string.IsNullOrEmpty(_languageId))
				return _languageId;

			string normalized = _languageId.Replace('_', '-').ToLowerInvariant();
			if (normalized != _languageId)
				UiLog.LogWarning($"Language ID '{_languageId}' is not in canonical form. Use '{normalized}' instead (lowercase, hyphens as separators).");

			return normalized;
		}

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
		/// Translates <paramref name="_key"/> disambiguated by <paramref name="_context"/>.
		/// The composed lookup key follows the GNU gettext convention: "context\u0004msgid".
		/// Pass null or empty <paramref name="_context"/> to behave identically to
		/// <see cref="Translate(string,string,RetValIfNotFound)"/>.
		/// </summary>
		/// <param name="_key">The localization key (msgid).</param>
		/// <param name="_context">The disambiguation context (msgctxt). Empty/null for no context.</param>
		/// <param name="_group">Group namespace.</param>
		/// <param name="_retValIfNotFound">Behavior when the key is not found.</param>
		/// <returns>Translated string.</returns>
		public string Translate( string _key, string _context, string _group, RetValIfNotFound _retValIfNotFound = RetValIfNotFound.Key )
		{
			string composedKey = string.IsNullOrEmpty(_context) ? _key : $"{_context}\u0004{_key}";
			return Translate(composedKey, _group, _retValIfNotFound);
		}
		
		/// <summary>
		/// Checks whether a translation key exists in the specified group for the current language.
		/// </summary>
		/// <param name="_key">The localization key to check.</param>
		/// <param name="_group">The group namespace to search in.</param>
		/// <returns>True if the key exists, false otherwise.</returns>
		public abstract bool HasKey(string _key, string _group);

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

		/// <summary>
		/// Returns the list of available language IDs (e.g. "en", "de", "fr").
		/// At runtime, reads the pre-generated <c>uitk_available_languages.txt</c> resource file.
		/// In the Unity Editor, falls back to scanning project assets when the file is not yet present.
		/// </summary>
		public string[] GetAvailableLanguages()
		{
			var lines = AssetUtility.ReadLines(AVAILABLE_LANGUAGES_RESOURCE, _removeEmpty: true);
			if (lines != null && lines.Count > 0)
				return lines.ToArray();

#if UNITY_EDITOR
			return EdAvailableLanguages;
#else
			return System.Array.Empty<string>();
#endif
		}

		/// <summary>
		/// Maps every Unity <see cref="SystemLanguage"/> to its ISO 639-1 code, with a region subtag where
		/// Unity is already specific (e.g. <see cref="SystemLanguage.ChineseSimplified"/> -&gt; "zh-cn").
		/// This is intentionally the full enum, independent of which languages a given project ships;
		/// <see cref="GetSupportedIsoCode"/> intersects it with the actually available languages.
		/// <see cref="SystemLanguage.Unknown"/> is deliberately absent (treated as "no mapping").
		/// </summary>
		private static readonly Dictionary<SystemLanguage, string> s_systemLanguageToIso =
			new Dictionary<SystemLanguage, string>
			{
				{ SystemLanguage.Afrikaans,          "af"    },
				{ SystemLanguage.Arabic,             "ar"    },
				{ SystemLanguage.Basque,             "eu"    },
				{ SystemLanguage.Belarusian,         "be"    },
				{ SystemLanguage.Bulgarian,          "bg"    },
				{ SystemLanguage.Catalan,            "ca"    },
				{ SystemLanguage.Chinese,            "zh"    },
				{ SystemLanguage.ChineseSimplified,  "zh-cn" },
				{ SystemLanguage.ChineseTraditional, "zh-tw" },
				{ SystemLanguage.Czech,              "cs"    },
				{ SystemLanguage.Danish,             "da"    },
				{ SystemLanguage.Dutch,              "nl"    },
				{ SystemLanguage.English,            "en"    },
				{ SystemLanguage.Estonian,           "et"    },
				{ SystemLanguage.Faroese,            "fo"    },
				{ SystemLanguage.Finnish,            "fi"    },
				{ SystemLanguage.French,             "fr"    },
				{ SystemLanguage.German,             "de"    },
				{ SystemLanguage.Greek,              "el"    },
				{ SystemLanguage.Hebrew,             "he"    },
				{ SystemLanguage.Hindi,              "hi"    },
				{ SystemLanguage.Hungarian,          "hu"    },
				{ SystemLanguage.Icelandic,          "is"    },
				{ SystemLanguage.Indonesian,         "id"    },
				{ SystemLanguage.Italian,            "it"    },
				{ SystemLanguage.Japanese,           "ja"    },
				{ SystemLanguage.Korean,             "ko"    },
				{ SystemLanguage.Latvian,            "lv"    },
				{ SystemLanguage.Lithuanian,         "lt"    },
				{ SystemLanguage.Norwegian,          "no"    },
				{ SystemLanguage.Polish,             "pl"    },
				{ SystemLanguage.Portuguese,         "pt"    },
				{ SystemLanguage.Romanian,           "ro"    },
				{ SystemLanguage.Russian,            "ru"    },
				{ SystemLanguage.SerboCroatian,      "sr"    },
				{ SystemLanguage.Slovak,             "sk"    },
				{ SystemLanguage.Slovenian,          "sl"    },
				{ SystemLanguage.Spanish,            "es"    },
				{ SystemLanguage.Swedish,            "sv"    },
				{ SystemLanguage.Thai,               "th"    },
				{ SystemLanguage.Turkish,            "tr"    },
				{ SystemLanguage.Ukrainian,          "uk"    },
				{ SystemLanguage.Vietnamese,         "vi"    },
			};

		// Cached grouping of the available languages by their base (primary) subtag, e.g.
		// "pt" -> { "pt-br" }, "zh" -> { "zh-cn", "zh-tw" }. Built lazily from GetAvailableLanguages().
		private static Dictionary<string, List<string>> s_availableByBase;

		/// <summary>
		/// Returns the primary (base) subtag of a language id, i.e. the part before the first hyphen.
		/// "pt-br" -&gt; "pt", "en" -&gt; "en".
		/// </summary>
		public static string GetBaseLanguage( string _languageId )
		{
			if (string.IsNullOrEmpty(_languageId))
				return _languageId;

			int dash = _languageId.IndexOf('-');
			return dash < 0 ? _languageId : _languageId.Substring(0, dash);
		}

		/// <summary>
		/// Resolves a device <see cref="SystemLanguage"/> to the best-matching language id that this project
		/// actually ships, based on the generated available-languages list (see <see cref="GetAvailableLanguages"/>).
		///
		/// For the base language the system language maps to (e.g. "pt" for <see cref="SystemLanguage.Portuguese"/>),
		/// it looks at the available languages sharing that base and resolves in this order:
		/// <list type="number">
		/// <item><description>If <paramref name="_preferredLanguageSubtype"/> names a variant for this base that
		/// is available, that variant wins.</description></item>
		/// <item><description>Else, if the exact code the system reports is available (relevant when Unity is
		/// region-specific, e.g. "zh-cn"), it is returned.</description></item>
		/// <item><description>Else, if the main language itself is available, it is returned (a main language
		/// together with one or more variants resolves to the main language).</description></item>
		/// <item><description>Else, if exactly one variant is available, it is returned.</description></item>
		/// <item><description>Else (several variants, none of them the main language) the result is ambiguous:
		/// a variant is picked at random and <paramref name="_errorHandling"/> is applied.</description></item>
		/// </list>
		/// If the language is not shipped at all, "en" is returned and <paramref name="_errorHandling"/> is applied.
		/// </summary>
		/// <param name="_systemLanguage">The device system language to resolve.</param>
		/// <param name="_errorHandling">How to report the "ambiguous" and "not found" cases.</param>
		/// <param name="_preferredLanguageSubtype">Optional map of base language id -&gt; preferred variant id
		/// (e.g. { "pt", "pt-br" }). Only honored when the preferred variant is actually available.</param>
		/// <returns>An available language id, or "en" as a last resort.</returns>
		public static string GetSupportedIsoCode(
			SystemLanguage _systemLanguage,
			EErrorHandling _errorHandling = EErrorHandling.None,
			Dictionary<string, string> _preferredLanguageSubtype = null )
		{
			return GetSupportedIsoCode(_systemLanguage, GetAvailableByBase(), _errorHandling, _preferredLanguageSubtype);
		}

		/// <summary>
		/// Overload of <see cref="GetSupportedIsoCode(SystemLanguage,EErrorHandling,Dictionary{string,string})"/>
		/// that resolves against an explicitly supplied set of available language ids instead of the project's
		/// generated list. Useful for callers that maintain their own set, and for unit tests (no Resources I/O).
		/// </summary>
		public static string GetSupportedIsoCode(
			SystemLanguage _systemLanguage,
			IEnumerable<string> _availableLanguages,
			EErrorHandling _errorHandling = EErrorHandling.None,
			Dictionary<string, string> _preferredLanguageSubtype = null )
		{
			return GetSupportedIsoCode(_systemLanguage, GroupByBase(_availableLanguages), _errorHandling, _preferredLanguageSubtype);
		}

		private static string GetSupportedIsoCode(
			SystemLanguage _systemLanguage,
			Dictionary<string, List<string>> _availableByBase,
			EErrorHandling _errorHandling,
			Dictionary<string, string> _preferredLanguageSubtype )
		{
			const string fallback = "en";

			if (!s_systemLanguageToIso.TryGetValue(_systemLanguage, out string desired))
			{
				HandleError(_errorHandling, $"No ISO code mapping for system language '{_systemLanguage}'. Falling back to '{fallback}'.");
				return fallback;
			}

			desired = NormalizeLanguageId(desired);
			string baseId = GetBaseLanguage(desired);

			if (!_availableByBase.TryGetValue(baseId, out var matches) || matches.Count == 0)
			{
				HandleError(_errorHandling, $"System language '{_systemLanguage}' ('{baseId}') is not among the available languages. Falling back to '{fallback}'.");
				return fallback;
			}

			// 1) Explicit caller preference for this base language wins, if it is actually available.
			if (_preferredLanguageSubtype != null
				&& _preferredLanguageSubtype.TryGetValue(baseId, out string preferred))
			{
				string preferredNorm = NormalizeLanguageId(preferred);
				if (matches.Contains(preferredNorm))
					return preferredNorm;
			}

			// 2) The exact code the system reports (relevant when it is region-specific, e.g. "zh-cn").
			if (matches.Contains(desired))
				return desired;

			// 3) The main/base language, if present (main language + variants -> main language).
			if (matches.Contains(baseId))
				return baseId;

			// 4) A single variant, with no main language present.
			if (matches.Count == 1)
				return matches[0];

			// 5) Several variants, none of them the main language: ambiguous.
			string picked = matches[UnityEngine.Random.Range(0, matches.Count)];
			HandleError(_errorHandling, $"Ambiguous language variants [{string.Join(", ", matches)}] for base '{baseId}' with no main language available. Picking '{picked}'.");
			return picked;
		}

		private static Dictionary<string, List<string>> GroupByBase( IEnumerable<string> _languages )
		{
			var result = new Dictionary<string, List<string>>();
			if (_languages == null)
				return result;

			foreach (string raw in _languages)
			{
				string id = NormalizeLanguageId(raw);
				if (string.IsNullOrEmpty(id))
					continue;

				string baseId = GetBaseLanguage(id);
				if (!result.TryGetValue(baseId, out var list))
				{
					list = new List<string>();
					result[baseId] = list;
				}

				if (!list.Contains(id))
					list.Add(id);
			}

			return result;
		}

		private static Dictionary<string, List<string>> GetAvailableByBase()
		{
			if (s_availableByBase == null)
				s_availableByBase = GroupByBase(Instance.GetAvailableLanguages());

			return s_availableByBase;
		}

		private static void HandleError( EErrorHandling _errorHandling, string _message )
		{
			switch (_errorHandling)
			{
				case EErrorHandling.None:
					break;
				case EErrorHandling.Warning:
					UiLog.LogWarning(_message);
					break;
				case EErrorHandling.Error:
					UiLog.LogError(_message);
					break;
				case EErrorHandling.Throw:
					throw new System.InvalidOperationException(_message);
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
		/// <param name="_sourceRef">Optional source reference (e.g. "Assets/Prefabs/Foo.prefab") written as a #: comment in the POT file.</param>
		public abstract void EdAddKey( string _singularKey, string _pluralKey = null, string _group = null, string _sourceRef = null );
		
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
			else
			{
				_languageId = NormalizeLanguageId(_languageId);
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