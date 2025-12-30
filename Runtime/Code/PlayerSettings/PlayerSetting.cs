using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

namespace GuiToolkit
{
	/// <summary>
	/// Player setting type enum.
	/// Usually default (Auto) can be used for int, float, bool and string.
	/// Only if the player setting needs special handling (e.g. radio buttons), the type needs to be explicitly defined
	/// </summary>
	public enum EPlayerSettingType
	{
		Auto,       //!< Automatically determined
		Language,   //!< Special case language type
		Radio,      //!< Special case radio buttons
	}

	/// <summary>
	/// Additional options for player settings.
	/// Used to keep PlayerSetting ctor small and clear.
	/// Also, we keep all options for all PlayerSetting flavors in one simple class without hierarchy to keep things simple
	/// </summary>
	public class PlayerSettingOptions
	{
		public static readonly List<KeyCode> KeyCodeNoMouseList =       //!< Convenient filter list for all mouse keys forbidden or only mouse keys allowed, depending on KeyCodeFilterListIsWhitelist
		new()
		{
			KeyCode.Mouse0,
			KeyCode.Mouse1,
			KeyCode.Mouse2,
			KeyCode.Mouse3,
			KeyCode.Mouse4,
			KeyCode.Mouse5,
			KeyCode.Mouse6,
#if UNITY_2023_1_OR_NEWER
			KeyCode.WheelDown,
			KeyCode.WheelUp,
#endif
		};

		public EPlayerSettingType Type = EPlayerSettingType.Auto;           //!< Player setting type. Usually left default (Auto: automatically determined)
		public string Key = null;                                           //!< Key. If left null or empty, player setting title is used as key.
		public List<string> Icons;                                          //!< List of icons to be used, depending on player setting type
		public List<string> Titles;                                         //!< Titles for UI display(optional, else string values are also used as titles)
		public List<string> StringValues;                                   //!< String values for string based PlayerSettingOptions
		public List<KeyCode> KeyCodeFilterList;                             //!< Filter list for keycodes
		public bool KeyCodeFilterListIsWhitelist;                           //!< Set to true if you want the filter list to be whitelist instead of blacklist
		public bool IsLocalized = true;                                     //!< Should usually be set to true; only set to false if you want to display languages (see TestMain language setting)
		public UnityAction<PlayerSetting> OnChanged = null;                 //!< Optional callback. To react to player setting changes, you may either use this or the global event UiEventDefinitions.EvPlayerSettingChanged
		public bool IsSaveable = true;                                      //!< Is the option saved in player prefs? Obviously usually true, but can be set to false for cheats etc.
		public object CustomData = null;                                    //!< Optional custom data to hand over to your handler
		public Func<float, string> ValueToStringFn = null;                  //!< Optional slider text conversion. If left null, no text is displayed.
		public GameObject CustomPrefab = null;                              //!< Custom prefab to be used in Ui

		public static PlayerSettingOptions NoMouseKeys =>
			new()
			{
				KeyCodeFilterList = KeyCodeNoMouseList
			};

		public static PlayerSettingOptions OnlyMouseKeys =>
			new()
			{
				KeyCodeFilterList = KeyCodeNoMouseList,
				KeyCodeFilterListIsWhitelist = true,
			};
	}

	[Serializable]
	public class PlayerSetting : LocaClass
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


		private TaskScheduler m_mainThreadScheduler;
		protected object m_value;
		protected object m_savedValue;
		protected bool m_allowInvokeEvents = false;
		protected PlayerSettingOptions m_options;

		public PlayerSettingOptions Options => m_options;
		public string Category => m_category;
		public string Group => m_group;
		public string Title => m_title;
		public string Key => m_key;

		public bool AllowInvokeEvents
		{
			get => m_allowInvokeEvents;
			set => m_allowInvokeEvents = value;
		}

		public object Value
		{
			get => GetValue(ref m_value);
			set
			{
				CheckType(value?.GetType());
				m_value = value;
				InvokeEvents();
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
		public bool IsButton => m_type == null;
		public bool HasIcons => m_icons != null;
		public List<string> Icons => m_icons;
		public bool IsLocalized => m_isLocalized;

		public PlayerSetting()
		{
			m_mainThreadScheduler = TaskScheduler.FromCurrentSynchronizationContext();
		}

		public PlayerSetting(string _category, string _group, string _title, object _defaultValue,
			PlayerSettingOptions _options = null) : this()
		{
			m_options = _options ?? new PlayerSettingOptions();
			Type type = _defaultValue?.GetType();
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

			InitValue(type);

			if (IsLanguage)
				UiEventDefinitions.EvLanguageChanged.AddListener(OnLanguageChanged);
		}

		~PlayerSetting()
		{
			if (IsLanguage)
				UiEventDefinitions.EvLanguageChanged.RemoveListener(OnLanguageChanged);
		}

		public void SetValueSilent(object _value)
		{
			CheckType(_value?.GetType());
			m_value = _value;
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
			if (m_options.Type == EPlayerSettingType.Language)
				LocaManager.Instance.ChangeLanguage((string)m_value);
		}

		public void InvokeEvents()
		{
			if (!AllowInvokeEvents)
				return;

			UiEventDefinitions.EvPlayerSettingChanged.Invoke(this);
			m_options.OnChanged?.Invoke(this);
		}

		protected T GetValue<T>(ref object _v)
		{
			CheckType(typeof(T));

			if (m_type == typeof(bool))
				return (T)(object)(Convert.ToBoolean(_v));

			return (T)_v;
		}

		protected object GetValue(ref object _v)
		{
			if (m_type == null)
				return null;
			if (m_type == typeof(bool))
				return Convert.ToBoolean(_v);
			if (m_type.IsEnum)
				return System.Enum.ToObject(m_type, _v);
			return _v;
		}

		protected void CheckType(Type _t)
		{
			if (_t != m_type)
				throw new ArgumentException(
					$"Wrong value type in Player Setting '{m_key}': Should be '{m_type.Name}', is '{_t.Name}'");
		}

		protected void OnLanguageChanged(string _language)
		{
			Value = _language;
		}

		protected static int InitValue(PlayerSettingOptions _options, string _key, int _defaultValue)
		{
			var deflt = Convert.ToInt32(_defaultValue);
			return _options.IsSaveable ? PlayerPrefs.GetInt(_key, deflt) : deflt;
		}

		protected static float InitValue(PlayerSettingOptions _options, string _key, float _defaultValue)
		{
			var deflt = Convert.ToSingle(_defaultValue);
			return _options.IsSaveable ? PlayerPrefs.GetFloat(_key, deflt) : deflt;
		}

		protected static string InitValue(PlayerSettingOptions _options, string _key, string _defaultValue)
		{
			var deflt = Convert.ToString(_defaultValue);
			return _options.IsSaveable ? PlayerPrefs.GetString(_key, deflt) : deflt;
		}

		protected void InitValue(Type _type)
		{
			if (_type == null)
				return;

			if (_type == typeof(int) || _type.IsEnum)
				m_value = Convert.ToInt32(DefaultValue);
			else if (_type == typeof(bool))
				m_value = Convert.ToBoolean(DefaultValue);
			else if (_type == typeof(float))
				m_value = Convert.ToSingle(DefaultValue);
			else if (_type == typeof(string))
				m_value = Convert.ToString(DefaultValue);
			else if (_type == typeof(KeyCode))
				m_value = (KeyCode)DefaultValue;
			else
				UiLog.LogError($"Unknown type for player setting '{Key}': {_type.Name}");
		}
	}
}