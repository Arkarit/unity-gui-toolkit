using System;
using System.Collections.Generic;
using UnityEngine;

namespace GuiToolkit
{
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

		protected object m_value;
		protected object m_savedValue;
		protected bool m_allowInvokeEvents = false;
		protected PlayerSettingOptions m_options;
		
		// Note: These events are only data types. They are handled in PlayerSettings, but not here.
		////////////////////////////////////////////////////////////////////////////////////////////
		[NonSerialized] public CEvent m_onKeyDown = new();
		[NonSerialized] public CEvent m_onKeyUp = new();
		[NonSerialized] public CEvent m_whileKey = new();
		[NonSerialized] public CEvent m_onClick = new();
		
		[NonSerialized] public CEvent<Vector3, Vector3> m_onBeginDrag = new();
		[NonSerialized] public CEvent<Vector3, Vector3> m_whileDrag = new();
		[NonSerialized] public CEvent<Vector3, Vector3> m_onEndDrag = new();

		public PlayerSettingOptions Options => m_options;
		public string Category => m_category;
		public string Group => m_group;
		public string Title => m_title;
		public string Key => m_key;
		
		public CEvent OnKeyDown => m_onKeyDown;
		public CEvent OnKeyUp => m_onKeyUp;
		public CEvent WhileKey => m_whileKey;
		public CEvent OnClick => m_onClick;
		
		public CEvent<Vector3, Vector3> OnBeginDrag => m_onBeginDrag;
		public CEvent<Vector3, Vector3> WhileDrag => m_whileDrag;
		public CEvent<Vector3, Vector3> OnEndDrag => m_onBeginDrag;

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
				if (value == null)
					throw new ArgumentNullException(nameof(value));

				CheckType(value.GetType());
				m_value = value;
				InvokeEvents();
			}
		}

		public object DefaultValue => GetValue(ref m_defaultValue);

		public System.Type Type => m_type;
		public bool IsKeyBinding => m_type == typeof(KeyBinding);
		public bool IsRadio => m_isRadio; // is single-choice selection UI (incl. Language)
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
		}

		public PlayerSetting( string _category, string _group, string _title, object _defaultValue,
			PlayerSettingOptions _options = null ) : this()
		{
			m_options = _options ?? new PlayerSettingOptions();

			// We accept a single key code as a Key Binding without modifiers, but we convert it to KeyBinding
			Type type = _defaultValue?.GetType();
			if (type == typeof(KeyCode))
			{
				type = typeof(KeyBinding);
				KeyCode kc = (KeyCode)_defaultValue;
				_defaultValue = new KeyBinding(kc);
			}

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


		public void Clear()
		{
			OnKeyDown.RemoveAllListeners();
			OnKeyUp.RemoveAllListeners();
			WhileKey.RemoveAllListeners();
			if (IsLanguage)
				UiEventDefinitions.EvLanguageChanged.RemoveListener(OnLanguageChanged);
		}

		public void SetValueSilent( object _value )
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

		protected T GetValue<T>( ref object _v )
		{
			CheckType(typeof(T));

			if (m_type == typeof(bool))
				return (T)(object)(Convert.ToBoolean(_v));

			return (T)_v;
		}

		protected object GetValue( ref object _v )
		{
			if (m_type == null)
				return null;
			if (m_type == typeof(bool))
				return Convert.ToBoolean(_v);
			if (m_type.IsEnum)
				return System.Enum.ToObject(m_type, _v);
			return _v;
		}

		protected void CheckType( Type _t )
		{
			if (_t == null)
				throw new ArgumentNullException(nameof(_t));

			if (_t != m_type)
				throw new ArgumentException(
					$"Wrong value type in Player Setting '{m_key}': Should be '{m_type.Name}', is '{_t.Name}'");
		}

		protected void OnLanguageChanged( string _language )
		{
			Value = _language;
		}

		protected static int InitValue( PlayerSettingOptions _options, string _key, int _defaultValue )
		{
			var deflt = Convert.ToInt32(_defaultValue);
			return _options.IsSaveable ? PlayerPrefs.GetInt(_key, deflt) : deflt;
		}

		protected static float InitValue( PlayerSettingOptions _options, string _key, float _defaultValue )
		{
			var deflt = Convert.ToSingle(_defaultValue);
			return _options.IsSaveable ? PlayerPrefs.GetFloat(_key, deflt) : deflt;
		}

		protected static string InitValue( PlayerSettingOptions _options, string _key, string _defaultValue )
		{
			var deflt = Convert.ToString(_defaultValue);
			return _options.IsSaveable ? PlayerPrefs.GetString(_key, deflt) : deflt;
		}

		protected void InitValue( Type _type )
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
			else if (_type == typeof(KeyBinding))
			{
				var def = (KeyBinding)DefaultValue;
				m_value = new KeyBinding(def.Encoded);
			}
			else
				UiLog.LogError($"Unknown type for player setting '{Key}': {_type.Name}");
		}
	}
}