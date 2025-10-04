using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using UnityEngine;


namespace GuiToolkit
{
	public abstract class LocaManager : IEditorAware
	{
		public const string PLAYER_PREFS_KEY = StringConstants.PLAYER_PREFS_PREFIX + "Language";

		public abstract string Translate( string _key, string _group = null );
		public abstract string Translate( string _singularKey, string _pluralKey, int _n, string _group = null );

		public abstract bool ChangeLanguageImpl( string _languageId );

		public string Language { get; private set; } = null;

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
	}
}