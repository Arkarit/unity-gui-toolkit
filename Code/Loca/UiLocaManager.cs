using System;
using System.Collections.Generic;
using UnityEngine;

namespace GuiToolkit
{
	public interface ILocaListener
	{
		void OnLanguageChanged(string _languageId);
	}

	public abstract class UiLocaManager
	{
		// Builtin languages
		public enum ELanguage
		{
			dev,
			lol,
			en,
			us,
			en_us,
			de,
		}

		protected ELanguage m_language = ELanguage.dev;

		private readonly HashSet<ILocaListener> m_locaListeners = new HashSet<ILocaListener>();

		public abstract string Translate(string _key);
		public abstract string Translate(string _singularKey, string _pluralKey, int _n );

		public abstract bool ChangeLanguageImpl(string _languageId);

		private static string[] s_languageNames;
		private static readonly Dictionary<string, ELanguage> s_languageByString = new Dictionary<string, ELanguage>();

		public ELanguage Language => m_language;

		public static string StringByLanguage(ELanguage _language)
		{
			InitEnumConversionIfNecessary();
			return s_languageNames[(int) _language];
		}

		public static ELanguage LanguageByString(string _language)
		{
			if (s_languageByString.TryGetValue(_language, out ELanguage result))
				return result;

			Debug.LogWarning($"Language '{_language}' not supported");
			return ELanguage.dev;
		}

		private static void InitEnumConversionIfNecessary()
		{
			if (s_languageNames == null)
			{
				s_languageNames = System.Enum.GetNames(typeof(ELanguage));
				ELanguage[] languages = (ELanguage[]) System.Enum.GetValues(typeof(ELanguage));
				for (int i=0; i<languages.Length; i++ )
				{
					s_languageByString.Add(s_languageNames[i], (ELanguage) i);
				}
			}
		}


#if UNITY_EDITOR
		public abstract void Clear();
		public abstract void AddKey( string _singularKey, string _pluralKey = null );
		public abstract void ReadKeyData();
		public abstract void WriteKeyData();
#endif

		public bool ChangeLanguage(string _languageId)
		{
			m_language = s_languageByString[_languageId];

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

		public bool ChangeLanguage(ELanguage _language)
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

		public static UiLocaManager Instance {get; set;}

	}
}