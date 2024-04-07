using System;
using System.Collections.Generic;
using UnityEngine;
namespace GuiToolkit
{
	public enum EPlayerSettingType
	{
		Auto,
		Language,
		Radio,
	}

	[Serializable]
	public class PlayerSettingOptions
	{
		public EPlayerSettingType Type = EPlayerSettingType.Auto;
		public string Key = null;
		public List<string> Icons;
		public List<string> Titles;
		public List<string> StringValues;
		public bool IsLocalized = true;
	}

	[Serializable]
	public class PlayerSetting
	{
		[SerializeField] protected string m_category;
		[SerializeField] protected string m_group;
		[SerializeField] protected string m_title;
		[SerializeField] protected string m_key;
		[SerializeField] protected object m_defaultValue;
		[SerializeField] protected System.Type m_type;
		[SerializeField] protected bool m_isRadio;
		[SerializeField] protected bool m_isLanguage;
		[SerializeField] protected List<string> m_icons;
		[SerializeField] protected bool m_isLocalized;

		protected object m_value;
		protected object m_savedValue;
		protected bool m_invokeEvents = false;
		protected PlayerSettingOptions m_options;

		public PlayerSettingOptions Options => m_options;
		public string Category => m_category;
		public string Group => m_group;
		public string Title => m_title;
		public string Key => m_key;
		public bool InvokeEvents
		{
			get => m_invokeEvents;
			set => m_invokeEvents = value;
		}

		public object Value
		{
			get => GetValue(ref m_value);
			set
			{
				CheckType(value.GetType());
				m_value = value;
				Apply();
				if (InvokeEvents)
				{
					UiEvents.OnPlayerSettingChanged.Invoke(this);
				}
			}
		}
		public object DefaultValue => GetValue(ref m_defaultValue);

		public System.Type Type => m_type;
		public bool IsKeyCode => m_type == typeof(KeyCode);
		public bool IsRadio => m_isRadio;
		public bool IsLanguage => m_isLanguage;
		public bool IsFloat => m_type == typeof(float);
		public bool IsBool => m_type == typeof(bool);
		public bool IsString => m_type == typeof(string);
		public bool HasIcons => m_icons != null;
		public List<string> Icons => m_icons;
		public bool IsLocalized => m_isLocalized;

		public PlayerSetting( string _category, string _group, string _title, object _defaultValue, PlayerSettingOptions _options = null )
		{
			m_options = _options != null ? _options : new PlayerSettingOptions();

			System.Type type = _defaultValue.GetType();
			m_category = _category;
			m_group = _group;
			m_isRadio = m_options.Type == EPlayerSettingType.Radio || m_options.Type == EPlayerSettingType.Language;
			m_isLanguage = m_options.Type == EPlayerSettingType.Language;
			m_title = _title;
			m_key = string.IsNullOrEmpty(m_options.Key) ? _title : m_options.Key;
			m_defaultValue = _defaultValue;
			m_icons = m_options.Icons;
			m_type = type;
			m_isLocalized = m_options.IsLocalized;

			if (type == typeof(int) || type == typeof(bool) || type.IsEnum)
				m_value = PlayerPrefs.GetInt(Key, Convert.ToInt32(DefaultValue));
			else if (type == typeof(float))
				m_value = PlayerPrefs.GetFloat(Key, Convert.ToSingle(DefaultValue));
			else if (type == typeof(string))
				m_value = PlayerPrefs.GetString(Key, Convert.ToString(DefaultValue));
			else
				Debug.LogError($"Unknown type for player setting '{Key}': {type.Name}");

			if (IsLanguage)
				UiEvents.OnLanguageChanged.AddListener(OnLanguageChanged);
		}

		~PlayerSetting()
		{
			if (IsLanguage)
				UiEvents.OnLanguageChanged.RemoveListener(OnLanguageChanged);
		}

		public T GetValue<T>()
		{
			return GetValue<T>(ref m_value);
		}
		public T GetDefaultValue<T>()
		{
			return GetValue<T>(ref m_defaultValue);
		}

		public void TempSaveValue() => m_savedValue = m_value;
		public void TempRestoreValue()
		{
			m_value = m_savedValue;
			Apply();
			if (m_options.Type == EPlayerSettingType.Language)
				LocaManager.Instance.ChangeLanguage((string) m_value);
		}

		private T GetValue<T>(ref object _v)
		{
			CheckType(typeof(T));

			if (m_type == typeof(bool))
				return (T)(object)(Convert.ToBoolean(_v));

			return (T)_v;
		}

		private object GetValue(ref object _v)
		{
			if (m_type == typeof(bool))
				return Convert.ToBoolean(_v);
			if (m_type.IsEnum)
				return System.Enum.ToObject(m_type, _v);
			return _v;
		}

		private void Apply()
		{
			if (m_type == typeof(int) || m_type == typeof(bool) || m_type.IsEnum)
				PlayerPrefs.SetInt(Key, Convert.ToInt32(m_value));
			else if (m_type == typeof(float))
				PlayerPrefs.SetFloat(Key, Convert.ToSingle(m_value));
			else if (m_type == typeof(string))
				PlayerPrefs.SetString(Key, Convert.ToString(m_value));
			else
				Debug.LogError($"Unknown type for player setting '{Key}': {m_type.Name}");
		}

		private void CheckType(Type _t)
		{
			if (_t != m_type)
				throw new ArgumentException($"Wrong value type in Player Setting '{m_key}': Should be '{m_type.Name}', is '{_t.Name}'");
		}

		private void OnLanguageChanged( string _language )
		{
			Value = _language;
		}


	}
}