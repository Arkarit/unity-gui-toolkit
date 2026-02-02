using System;
using System.Collections.Generic;
using UnityEngine;

namespace GuiToolkit
{
	public class PlayerSetting : LocaClass
	{
		protected string m_category;
		protected string m_group;
		protected string m_title;
		protected string m_key;
		protected bool m_isRadio;
		protected bool m_isLanguage;
		protected List<string> m_icons;
		protected bool m_isLocalized;
		protected object m_value;
		protected object m_defaultValue;
		protected Type m_type;
		protected object m_savedValue;
		protected bool m_allowInvokeEvents = false;
		protected PlayerSettingOptions m_options;

		// Note: These events are only data types. They are handled in PlayerSettings, but not here.
		////////////////////////////////////////////////////////////////////////////////////////////
		protected CEvent m_onKeyDown = new();
		protected CEvent m_onKeyUp = new();
		protected CEvent m_whileKey = new();
		protected CEvent m_onClick = new();
		protected CEvent<Vector3, Vector3, Vector3> m_onBeginDrag = new();
		protected CEvent<Vector3, Vector3, Vector3> m_whileDrag = new();
		protected CEvent<Vector3, Vector3, Vector3> m_onEndDrag = new();

		public PlayerSettingOptions Options => m_options;
		public string Category => m_category;
		public string Group => m_group;
		public string Title => m_title;
		public string Key => m_key;

		public CEvent OnKeyDown => m_onKeyDown;
		public CEvent OnKeyUp => m_onKeyUp;
		public CEvent WhileKey => m_whileKey;
		public CEvent OnClick => m_onClick;

		public CEvent<Vector3, Vector3, Vector3> OnBeginDrag => m_onBeginDrag;
		public CEvent<Vector3, Vector3, Vector3> WhileDrag => m_whileDrag;
		public CEvent<Vector3, Vector3, Vector3> OnEndDrag => m_onEndDrag;
		public bool SupportDrag => m_options.SupportDrag;

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

		public Type Type => m_type;
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

		protected PlayerSetting() { }

		public PlayerSetting( string _category, string _group, string _title, object _defaultValue,
			PlayerSettingOptions _options = null )
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

			if (m_options.OnKeyDown != null)
				OnKeyDown.AddListener(m_options.OnKeyDown);
			if (m_options.OnKeyUp != null)
				OnKeyUp.AddListener(m_options.OnKeyUp);
			if (m_options.WhileKey != null)
				WhileKey.AddListener(m_options.WhileKey);
			if (m_options.OnClick != null)
				OnClick.AddListener(m_options.OnClick);
			if (m_options.OnBeginDrag != null)
				OnBeginDrag.AddListener(m_options.OnBeginDrag);
			if (m_options.WhileDrag != null)
				WhileDrag.AddListener(m_options.WhileDrag);
			if (m_options.OnEndDrag != null)
				OnEndDrag.AddListener(m_options.OnEndDrag);

			InitValue(type);

			if (IsLanguage)
				UiEventDefinitions.EvLanguageChanged.AddListener(OnLanguageChanged);
		}

		public void Clear()
		{
			OnKeyDown.RemoveAllListeners();
			OnKeyUp.RemoveAllListeners();
			WhileKey.RemoveAllListeners();
			OnClick.RemoveAllListeners();

			OnBeginDrag.RemoveAllListeners();
			WhileDrag.RemoveAllListeners();
			OnEndDrag.RemoveAllListeners();

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

			if (m_type.IsEnum)
				return (T)System.Enum.ToObject(m_type, _v);

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

		protected void CheckType( Type _type )
		{
			if (_type != m_type)
			{
				var wantedStr = m_type != null ? m_type.Name : "<null>";
				var isStr = _type != null ? _type.Name : "<null>";
				throw new ArgumentException(
					$"Wrong value type in Player Setting '{m_key}': Should be '{wantedStr}', is '{isStr}'");
			}
		}

		protected void OnLanguageChanged( string _language )
		{
			Value = _language;
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