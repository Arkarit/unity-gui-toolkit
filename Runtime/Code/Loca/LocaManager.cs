using System;
using System.Collections.Generic;
using UnityEngine;

namespace GuiToolkit
{
	public abstract class LocaManager
	{
		public const string PLAYER_PREFS_KEY = StringConstants.PLAYER_PREFS_PREFIX + "Language";

		public abstract string Translate(string _key);
		public abstract string Translate(string _singularKey, string _pluralKey, int _n );

		public abstract bool ChangeLanguageImpl(string _languageId);

		public string Language { get; private set; } = null;

#if UNITY_EDITOR
		public abstract string[] AvailableLanguages { get; }
		public abstract void Clear();
		public abstract void AddKey( string _singularKey, string _pluralKey = null );
		public abstract void ReadKeyData();
		public abstract void WriteKeyData();
#endif

		private bool m_debugLoca = false;
		public bool DebugLoca => m_debugLoca;

		private static LocaManager s_locaManager = null;
		public static LocaManager Instance
		{
			get
			{
				if (s_locaManager == null)
				{
					s_locaManager = new LocaManagerDefaultImpl();
					UiEventDefinitions.EvLanguageChanged.Invoke(s_locaManager.Language);
				}
				return s_locaManager;
			}
			set
			{
				s_locaManager = value;
			}
		}

		protected LocaManager()
		{
			m_debugLoca = UiToolkitConfiguration.Instance.m_debugLoca;
			string language = PlayerPrefs.GetString(PLAYER_PREFS_KEY, "dev");
			ChangeLanguage(language, false);
		}

		public bool ChangeLanguage(string _languageId)
		{
			return ChangeLanguage(_languageId, true);
		}

		private bool ChangeLanguage(string _languageId, bool _invokeEvent)
		{
			if (Language == _languageId)
				return true;

			Language = _languageId;
			PlayerPrefs.SetString(PLAYER_PREFS_KEY, Language);

			if (!ChangeLanguageImpl(_languageId))
			{
				Debug.LogWarning($"Language '{_languageId}' not found");
				_languageId = "dev";
				ChangeLanguageImpl(_languageId);
			}

			if (_invokeEvent)
				UiEventDefinitions.EvLanguageChanged.Invoke(_languageId);

			return true;
		}
	}
}