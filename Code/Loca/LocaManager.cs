using System;
using System.Collections.Generic;
using UnityEngine;

namespace GuiToolkit
{
	public interface ILocaListener
	{
		void OnLanguageChanged(string _languageId);
	}

	public abstract class LocaManager
	{
		// Builtin languages
		public enum Language
		{
			dev,
			lol,
			en,
			us,
			en_us,
			de,
		}

		private readonly HashSet<ILocaListener> m_locaListeners = new HashSet<ILocaListener>();

		public abstract string Translate(string _key, string _group = "", bool _fallbackToDefaultGroup = true);
		public abstract string Translate(string _singularKey, string _pluralKey, int _n, string _group = "", bool _fallbackToDefaultGroup = true);

		public abstract bool ChangeLanguageImpl(string _languageId);

		private static string[] s_languageNames;
		private static readonly Dictionary<string, Language> s_languageByString = new Dictionary<string, Language>();

		public static string StringByLanguage(Language _language)
		{
			InitEnumConversionIfNecessary();
			return s_languageNames[(int) _language];
		}

		public static Language LanguageByString(string _language)
		{
			if (s_languageByString.TryGetValue(_language, out Language result))
				return result;

			Debug.LogWarning($"Language '{_language}' not supported");
			return Language.dev;
		}

		private static void InitEnumConversionIfNecessary()
		{
			if (s_languageNames == null)
			{
				s_languageNames = System.Enum.GetNames(typeof(Language));
				Language[] languages = (Language[]) System.Enum.GetValues(typeof(Language));
				for (int i=0; i<languages.Length; i++ )
				{
					s_languageByString.Add(s_languageNames[i], (Language) i);
				}
			}
		}


#if UNITY_EDITOR
		public abstract void Clear();
		public abstract void AddKey( string _group, string _singularKey, string _pluralKey = null );
		public abstract void ReadKeyData(LocaGroup _lgd);
		public abstract void WriteKeyData(LocaGroup _lgd);

		public void ReadKeyData()
		{
			Clear();
			UiSettings settings = UiSettings.EditorLoad();
			LocaGroup[] locaGroups = settings.m_locaGroups;
			if (locaGroups == null)
				return;
			foreach (var locaGroup in locaGroups)
				ReadKeyData(locaGroup);
		}
		public void WriteKeyData()
		{
			UiSettings settings = UiSettings.EditorLoad();
			LocaGroup[] locaGroups = settings.m_locaGroups;
			if (locaGroups == null)
				return;
			foreach (var locaGroup in locaGroups)
				WriteKeyData(locaGroup);
		}
#endif

		public bool ChangeLanguage(string _languageId)
		{
			if (!ChangeLanguageImpl(_languageId))
			{
				Debug.LogWarning($"Language '{_languageId}' not found");
				_languageId = "dev";
				ChangeLanguageImpl(_languageId);
			}

			foreach (ILocaListener listener in m_locaListeners)
				listener.OnLanguageChanged(_languageId);

			return true;
		}

		public bool ChangeLanguage(Language _language)
		{
			return ChangeLanguage(StringByLanguage(_language));
		}

		public void AddListener(ILocaListener _listener)
		{
			m_locaListeners.Add(_listener);
		}

		public void RemoveListener(ILocaListener _listener)
		{
			m_locaListeners.Remove(_listener);
		}

		public static LocaManager Instance {get; set;}

	}
}