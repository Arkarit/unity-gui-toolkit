using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit
{
	public abstract class UiPlayerSettingBase : UiThing
	{
		[SerializeField] protected UiTMPTranslator m_titleTranslator;
		[SerializeField] protected TMP_Text m_text;
		[SerializeField] protected bool m_isLocalized = true;

		protected PlayerSetting m_playerSetting;
		protected string m_subKey;

		private string Text
		{
			get => m_isLocalized ? m_titleTranslator.Text : m_text.text;
			set
			{
				if (m_isLocalized)
					m_titleTranslator.Text = value;
				else
					m_text.text = value;
			}
		}

		public bool Initialized => m_playerSetting != null;

		public PlayerSetting PlayerSetting => m_playerSetting;

		protected override void AddEventListeners()
		{
			base.AddEventListeners();
			UiEventDefinitions.EvPlayerSettingChanged.AddListener(OnPlayerSettingChanged);
		}

		protected override void RemoveEventListeners()
		{
			UiEventDefinitions.EvPlayerSettingChanged.RemoveListener(OnPlayerSettingChanged);
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
			m_isLocalized = _playerSetting.IsLocalized;
			string key = string.Empty;

			if (string.IsNullOrEmpty(_subKey))
			{
				key = Text = _playerSetting.Title;
			}
			else if (_playerSetting.Options.Titles == null)
			{
				key = Text = _subKey;
			}
			else
			{
				for (int i=0; i<_playerSetting.Options.StringValues.Count; i++)
				{
					if (_playerSetting.Options.StringValues[i] == _subKey)
					{
						key = Text = _playerSetting.Options.Titles[i];
						break;
					}
				}
			}

			gameObject.name = _gameObjectNamePrefix + key;

			if (_playerSetting.HasIcons)
				ApplyIcons(_playerSetting.Icons);
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