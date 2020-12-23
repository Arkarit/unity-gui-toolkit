using System;
using System.Collections.Generic;
using UnityEngine;
namespace GuiToolkit
{
	public class PlayerSettings : ScriptableObject
	{
		private readonly Dictionary<string,PlayerSetting> m_playerSettings = new Dictionary<string,PlayerSetting>();
		private Dictionary<KeyCode, KeyCode> m_keyCodes = new Dictionary<KeyCode, KeyCode>();

		public void Add(List<PlayerSetting> _playerSettings)
		{
			foreach (PlayerSetting playerSetting in _playerSettings )
			{
				if (!m_playerSettings.ContainsKey(playerSetting.Key))
					m_playerSettings.Add(playerSetting.Key, playerSetting);
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


	}
}