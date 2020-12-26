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

		[HideInInspector]
		[SerializeField]
		protected PlayerSetting m_playerSetting;

		public object Value
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

		public void SetData(string _gameObjectNamePrefix, string _title, PlayerSetting _playerSetting)
		{
			gameObject.name = _gameObjectNamePrefix + _title;
			m_titleTranslator.Text = _title;
			m_playerSetting = _playerSetting;
		}

		public virtual string Icon {get; set;}
	}
}