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
		protected string m_language = "dev";
		protected readonly HashSet<string> m_availableLanguages = new HashSet<string>();

		private readonly HashSet<ILocaListener> m_locaListeners = new HashSet<ILocaListener>();

		public abstract string Translate(string _key);
		public abstract string Translate(string _singularKey, string _pluralKey, int _n );

		public abstract bool ChangeLanguageImpl(string _languageId);

		public string Language => m_language;

#if UNITY_EDITOR
		public abstract void Clear();
		public abstract void AddKey( string _singularKey, string _pluralKey = null );
		public abstract void ReadKeyData();
		public abstract void WriteKeyData();
#endif

		public bool ChangeLanguage(string _languageId)
		{
			m_language = _languageId;

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