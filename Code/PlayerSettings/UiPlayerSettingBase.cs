using System;
using System.Collections.Generic;
using System.ComponentModel;
using TMPro;
using UnityEngine;

namespace GuiToolkit
{
	public abstract class UiPlayerSettingBase : UiThing
	{
		[SerializeField]
		protected UiTMPTranslator m_titleTranslator;

		protected PlayerSetting m_playerSetting;

		public bool Initialized => m_playerSetting != null;

		public T GetValue<T>()
		{
			if (m_playerSetting == null)
			{
				Debug.LogError($"GetValue() is only available after GetData() for '{GetType().ToString()}' on '{gameObject.name}'");
				return default;
			}
			return m_playerSetting.GetValue<T>();
		}

		public T GetDefaultValue<T>()
		{
			if (m_playerSetting == null)
			{
				Debug.LogError($"GetDefaultValue() is only available after GetData() for '{GetType().ToString()}' on '{gameObject.name}'");
				return default;
			}
			return m_playerSetting.GetDefaultValue<T>();
		}

		public virtual object Value
		{
			get
			{
				if (m_playerSetting == null)
				{
					Debug.LogError($"Value is only available after GetData() for '{GetType().ToString()}' on '{gameObject.name}'");
					return null;
				}
				return m_playerSetting.Value;
			}
			set
			{
				if (m_playerSetting == null)
				{
					Debug.LogError($"Value is only available after GetData() for '{GetType().ToString()}' on '{gameObject.name}'");
					return;
				}
				m_playerSetting.Value = value;
			}
		}

		public virtual void ApplyIcon(string _assetPath, bool _isPlayerSettingIcon) {}

		public virtual void SetData(string _gameObjectNamePrefix, string _title, PlayerSetting _playerSetting)
		{
			gameObject.name = _gameObjectNamePrefix + _title;
			m_titleTranslator.Text = _title;
			m_playerSetting = _playerSetting;

			if (_playerSetting.HasIcon)
				ApplyIcon(_playerSetting.Icon, true);
			else if (_playerSetting.IsString)
				ApplyIcon(_playerSetting.GetValue<string>(), false);

			Value = _playerSetting.Value;
		}

	}
}