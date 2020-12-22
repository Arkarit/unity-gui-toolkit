using System;
using System.Collections.Generic;
using System.ComponentModel;
using TMPro;
using UnityEngine;

namespace GuiToolkit
{
	public abstract class UiPlayerSettingBase<T> : UiThing where T:IConvertible
	{
		[SerializeField]
		protected TMP_Text m_text;

		[SerializeField]
		protected T m_defaultValue;

		[SerializeField]
		protected string m_uniqueKey;

		private T m_tempSavedValue;

		public static event Action<string, T> SettingChanged;

		public virtual T StoredValue
		{
			get
			{
				if (typeof(T) == typeof(int) || typeof(T) == typeof(bool) || typeof(T).IsEnum)
					return (T)Convert.ChangeType(PlayerPrefs.GetInt(Key, Convert.ToInt32(m_defaultValue)), typeof(T));
				if (typeof(T) == typeof(float))
					return (T)Convert.ChangeType(PlayerPrefs.GetFloat(Key, Convert.ToSingle(m_defaultValue)), typeof(T));
				if (typeof(T) == typeof(string))
					return (T)Convert.ChangeType(PlayerPrefs.GetString(Key, Convert.ToString(m_defaultValue)), typeof(T));

				Debug.Assert(false, "Unknown Settings type");
				return m_defaultValue;
			}

			set
			{
				if (typeof(T) == typeof(int) || typeof(T) == typeof(bool) || typeof(T).IsEnum)
					PlayerPrefs.SetInt(Key, Convert.ToInt32(value));
				else if (typeof(T) == typeof(float))
					PlayerPrefs.SetFloat(Key, Convert.ToSingle(value));
				else if (typeof(T) == typeof(string))
					PlayerPrefs.SetString(Key, Convert.ToString(value));
				else
					Debug.Assert(false, "Unknown Settings type");
			}
		}

		public void RestoreDefault() => StoredValue = m_defaultValue;
		public void SaveTemp() => m_tempSavedValue = StoredValue;
		public void RestoreTemp() => StoredValue = m_tempSavedValue;

		public virtual void Apply()
		{
			SettingChanged.Invoke(Key, StoredValue);
		}

		public string Key
		{
			get
			{
				if (string.IsNullOrEmpty(m_uniqueKey))
				{
					UiTMPTranslator translator = m_text.GetComponent<UiTMPTranslator>();
					m_uniqueKey = translator != null ? translator.LocaKey : m_text.text;
					m_uniqueKey = StringConstants.PLAYER_PREFS_PREFIX + m_uniqueKey;
				}
				return m_uniqueKey;
			}
		}
	}
}