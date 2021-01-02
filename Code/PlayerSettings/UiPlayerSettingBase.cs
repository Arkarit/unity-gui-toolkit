using System;
using System.Collections.Generic;
using System.ComponentModel;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit
{
	public abstract class UiPlayerSettingBase : UiThing
	{
		[SerializeField] protected UiTMPTranslator m_titleTranslator;
		[SerializeField] protected UiImage[] m_imagesToDisable;
		[SerializeField] protected UiButton[] m_buttonsToMakeNonInteractive;

		protected PlayerSetting m_playerSetting;
		protected string m_subKey;
		protected string m_title;
		protected bool m_enabled = true;

		public bool Initialized => m_playerSetting != null;

		public bool Enabled
		{
			get => m_enabled;
			set
			{
				if (m_enabled == value)
					return;
				m_enabled = value;
				foreach (var image in m_imagesToDisable)
					image.Enabled = m_enabled;
			}
		}

		protected override void AddEventListeners()
		{
			base.AddEventListeners();
			UiEvents.OnPlayerSettingChanged.AddListener(OnPlayerSettingChanged);
		}

		protected override void RemoveEventListeners()
		{
			UiEvents.OnPlayerSettingChanged.RemoveListener(OnPlayerSettingChanged);
			base.RemoveEventListeners();
		}

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

		public virtual Toggle Toggle => null;

		public virtual void ApplyIcons(List<string> _assetPaths) {}

		public virtual void SetData(string _gameObjectNamePrefix, PlayerSetting _playerSetting, string _subKey)
		{
			m_subKey = _subKey;
			m_playerSetting = _playerSetting;

			if (string.IsNullOrEmpty(_subKey))
			{
				m_title = m_titleTranslator.Text = _playerSetting.Title;
			}
			else if (_playerSetting.Options.Titles == null)
			{
				m_title = m_titleTranslator.Text = _subKey;
			}
			else
			{
				for (int i=0; i<_playerSetting.Options.StringValues.Count; i++)
				{
					if (_playerSetting.Options.StringValues[i] == _subKey)
					{
						m_title = m_titleTranslator.Text = _playerSetting.Options.Titles[i];
						break;
					}
				}
			}

			gameObject.name = _gameObjectNamePrefix + m_title;

			if (_playerSetting.HasIcons)
				ApplyIcons(_playerSetting.Icons);

			Value = _playerSetting.Value;
		}

		protected virtual void OnPlayerSettingChanged( PlayerSetting _playerSetting )
		{
			if (!Initialized)
				return;

			if (_playerSetting.Key == m_playerSetting.Key)
			{
				m_playerSetting.InvokeEvents = false;
				Value = _playerSetting.Value;
				m_playerSetting.InvokeEvents = true;
			}
		}

	}
}