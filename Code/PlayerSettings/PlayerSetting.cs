using System;
using UnityEngine;
namespace GuiToolkit
{
	[Serializable]
	public partial class PlayerSetting
	{
		[SerializeField] protected string m_category;
		[SerializeField] protected string m_group;
		[SerializeField] protected string m_key;
		[SerializeField] protected object m_defaultValue;
		[SerializeField] protected System.Type m_type;
		[SerializeField] protected bool m_isRadio;

		protected object m_value;
		protected object m_savedValue;

		public string Key => m_key;
		public object Value
		{
			get => m_value;
			set
			{
				CheckType(value.GetType());
				m_value = value;
				Apply();
			}
		}
		public object DefaultValue => m_defaultValue;
		public System.Type Type => m_type;
		public bool IsKeyCode => m_type == typeof(KeyCode);
		public bool IsRadio => m_isRadio;

		public PlayerSetting( string _key, object _defaultValue )
		{
			System.Type type = _defaultValue.GetType();
			m_key = _key;
			m_defaultValue = _defaultValue;
			m_type = type;

			if (type == typeof(int) || type == typeof(bool) || type.IsEnum)
				m_value = PlayerPrefs.GetInt(Key, Convert.ToInt32(DefaultValue));
			else if (type == typeof(float))
				m_value = PlayerPrefs.GetFloat(Key, Convert.ToSingle(DefaultValue));
			else if (type == typeof(string))
				m_value = PlayerPrefs.GetString(Key, Convert.ToString(DefaultValue));
			else
				Debug.LogError($"Unknown type for player setting '{Key}': {type.Name}");
		}

		public T GetValue<T>()
		{
			CheckType(typeof(T));

			if (m_type == typeof(bool))
				return (T)(object)((int)Value != 0);

			return (T)Value;
		}

		public void TempSaveValue() => m_savedValue = m_value;
		public void TempRestoreValue()
		{
			m_value = m_savedValue;
			Apply();
		}

		private void Apply()
		{
			if (m_type == typeof(int) || m_type == typeof(bool) || m_type.IsEnum)
				m_value = PlayerPrefs.GetInt(Key, Convert.ToInt32(DefaultValue));
			else if (m_type == typeof(float))
				m_value = PlayerPrefs.GetFloat(Key, Convert.ToSingle(DefaultValue));
			else if (m_type == typeof(string))
				m_value = PlayerPrefs.GetString(Key, Convert.ToString(DefaultValue));
			else
				Debug.LogError($"Unknown type for player setting '{Key}': {m_type.Name}");
		}

		private void CheckType(Type _t)
		{
			if (_t != m_type)
				throw new ArgumentException($"Wrong value type in Player Setting '{m_key}': Should be '{m_type.Name}', is '{_t.Name}'");
		}
	}
}