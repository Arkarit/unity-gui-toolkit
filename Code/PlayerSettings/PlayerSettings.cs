using System;
using System.Collections.Generic;
using UnityEngine;

namespace GuiToolkit
{
	public class PlayerSettings
	{
		private readonly Dictionary<string,PlayerSetting> m_playerSettings = new Dictionary<string,PlayerSetting>();
		private Dictionary<KeyCode, KeyCode> m_keyCodes = new Dictionary<KeyCode, KeyCode>();

		private static PlayerSettings s_instance;
		public static PlayerSettings Instance
		{
			get
			{
				if (s_instance == null)
					s_instance = new PlayerSettings();
				return s_instance;
			}
		}

		public void Add(List<PlayerSetting> _playerSettings)
		{
			foreach (PlayerSetting playerSetting in _playerSettings )
			{
				if (playerSetting == null)
					continue;

				m_playerSettings.Add(playerSetting.Key, playerSetting);
				if (playerSetting.IsKeyCode)
					m_keyCodes.Add(playerSetting.GetDefaultValue<KeyCode>(), playerSetting.GetValue<KeyCode>());

				UiEvents.OnPlayerSettingChanged.Invoke(playerSetting);
			}
		}

		public void Clear()
		{
			m_playerSettings.Clear();
			m_keyCodes.Clear();
		}

		public void TempSaveValues()
		{
			foreach (var kv in m_playerSettings)
				kv.Value.TempSaveValue();
		}

		public void TempRestoreValues()
		{
			foreach (var kv in m_playerSettings)
				kv.Value.TempRestoreValue();
		}

		public Dictionary<string, Dictionary<string, List<PlayerSetting>>> GetCategorized()
		{
			Dictionary<string, Dictionary<string, List<PlayerSetting>>> result = new Dictionary<string, Dictionary<string, List<PlayerSetting>>>();

			foreach (var kv in m_playerSettings)
			{
				PlayerSetting playerSetting = kv.Value;
				if (!result.ContainsKey(playerSetting.Category))
					result.Add(playerSetting.Category, new Dictionary<string, List<PlayerSetting>>());
				Dictionary<string, List<PlayerSetting>> groupDict = result[playerSetting.Category];
				if (!groupDict.ContainsKey(playerSetting.Group))
					groupDict.Add(playerSetting.Group, new List<PlayerSetting>());
				groupDict[playerSetting.Group].Add(playerSetting);
			}
			return result;
		}

		public void RestoreDefaults()
		{
			foreach (var kv in m_playerSettings)
				kv.Value.Value = kv.Value.DefaultValue;
		}


	}
}