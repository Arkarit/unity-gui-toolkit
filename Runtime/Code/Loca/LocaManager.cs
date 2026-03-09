using System.Globalization;
using System.Threading;
using UnityEngine;


namespace GuiToolkit
{
	public abstract class LocaManager : IEditorAware
	{
		public enum RetValIfNotFound
		{
			Key,
			EmptyString,
			Null,
		}

		public const string PLAYER_PREFS_KEY = StringConstants.PLAYER_PREFS_PREFIX + "Language";

		public abstract string Translate( string _key, string _group = null, RetValIfNotFound _retValIfNotFound = RetValIfNotFound.Key );
		public abstract string Translate( string _singularKey, string _pluralKey, int _n, string _group = null, RetValIfNotFound _retValIfNotFound = RetValIfNotFound.Key );
		public abstract bool HasKey(string _key, string _group);

		/// <summary>
		/// Translates <paramref name="_key"/> disambiguated by <paramref name="_context"/>.
		/// The composed lookup key follows the GNU gettext convention: "context\u0004msgid".
		/// Pass null or empty <paramref name="_context"/> to behave identically to
		/// <see cref="Translate(string,string,RetValIfNotFound)"/>.
		/// </summary>
		public string Translate( string _key, string _context, string _group = null, RetValIfNotFound _retValIfNotFound = RetValIfNotFound.Key )
		{
			string composedKey = string.IsNullOrEmpty(_context) ? _key : $"{_context}\u0004{_key}";
			return Translate(composedKey, _group, _retValIfNotFound);
		}

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
		public virtual void UnregisterProvider( ILocaProvider _provider ) { }

		public string Language { get; protected set; } = null;

		// Hardcoded for now; extend if necessary.
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
		public abstract string[] EdAvailableLanguages { get; }
		public abstract void EdClear();
		public abstract void EdAddKey( string _singularKey, string _pluralKey = null, string _group = null );
		public abstract void EdReadKeyData();
		public abstract void EdWriteKeyData();
#endif

		private bool m_debugLoca = false;
		public bool DebugLoca => m_debugLoca;

		private static LocaManager s_locaManager = null;
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