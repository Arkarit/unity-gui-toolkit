using System;
using System.Collections.Generic;
using UnityEngine;

namespace GuiToolkit
{
	public interface ILocaListener
	{
		void OnLanguageChanged(string _languageId);
	}

	public abstract class UiLocaManager : MonoBehaviour
	{
		private readonly HashSet<ILocaListener> m_locaListeners = new HashSet<ILocaListener>();

		public abstract string Translate(string _key);
		public abstract bool ChangeLanguageImpl(string _languageId);

#if UNITY_EDITOR
		public abstract void ChangeKey( string _oldKey, string _newKey );
#endif

		public bool ChangeLanguage(string _languageId)
		{
			if (!ChangeLanguageImpl(_languageId))
			{
				Debug.LogError($"Language '{_languageId}' not found");
				return false;
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

		private static UiLocaManager s_instance;
		public static UiLocaManager Instance
		{
			get
			{
				if (s_instance == null)
					s_instance = UnityEngine.Object.FindObjectOfType<UiLocaManager>();
#if UNITY_EDITOR
				if (s_instance == null)
					Debug.LogError("Attempt to access UiAbstractLocaManager.Instance, but game object containing the instance not found." +
						" Please set up a game object with an attached UiAbstractLocaManager component!");
#endif
				return s_instance;
			}
			private set
			{
				s_instance = value;
			}
		}
	}
}