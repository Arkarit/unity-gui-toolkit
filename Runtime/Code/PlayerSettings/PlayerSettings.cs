using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit
{
	public class PlayerSettings
	{
		private readonly int FIRST_MOUSE_KEY = (int)(object)KeyCode.Mouse0;
		private readonly Dictionary<string,PlayerSetting> m_playerSettings = new Dictionary<string,PlayerSetting>();
		private Dictionary<KeyCode, KeyCode> m_keyCodes = new Dictionary<KeyCode, KeyCode>();

		protected PlayerSettings()
		{
			UiEventDefinitions.EvPlayerSettingChanged.AddListener(OnPlayerSettingChanged);
		}
		~PlayerSettings()
		{
			UiEventDefinitions.EvPlayerSettingChanged.RemoveListener(OnPlayerSettingChanged);
		}

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

		public PlayerSetting GetPlayerSetting(string _key)
		{
			if (m_playerSettings.TryGetValue(_key, out PlayerSetting result))
				return result;

			return null;
		}

		public T GetValue<T>(string _key)
		{
			if (m_playerSettings.TryGetValue(_key, out PlayerSetting ps))
				return ps.GetValue<T>();

			Debug.LogError($"Player setting with key '{_key}' not found");
			return default;
		}

		public void Add(List<PlayerSetting> _playerSettings)
		{
			foreach (PlayerSetting playerSetting in _playerSettings )
			{
				if (playerSetting == null)
					continue;

				m_playerSettings.Add(playerSetting.Key, playerSetting);
				if (playerSetting.IsKeyCode)
				{
					if (m_keyCodes.TryGetValue(playerSetting.GetDefaultValue<KeyCode>(), out KeyCode existing))
					{
						Debug.LogError($"Default Key code '{existing}' of player setting '{playerSetting.Key}' already exists. Each default key code has to be unique.");
						continue;
					}
					
					m_keyCodes.Add(playerSetting.GetDefaultValue<KeyCode>(), playerSetting.GetValue<KeyCode>());
				}
			}

			// We want to only invoke the player settings changed event once all player settings have been added.
			// Thus second iteration.
			foreach (PlayerSetting playerSetting in _playerSettings )
			{
				playerSetting.AllowInvokeEvents = true;
				playerSetting.InvokeEvents();
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
			{
				var playerSetting = kv.Value;
				Log(playerSetting, "Saving");
				playerSetting.TempSaveValue();
			}
		}

		public void TempRestoreValues()
		{
			foreach (var kv in m_playerSettings)
			{
				var playerSetting = kv.Value;
				playerSetting.TempRestoreValue();
				playerSetting.InvokeEvents();
				Log(playerSetting, "Restored");
			}
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

		public bool GetKey(KeyCode _originalKeyCode)
		{
			if (m_keyCodes.TryGetValue(_originalKeyCode, out KeyCode boundKeyCode))
			{
				if (boundKeyCode == KeyCode.None)
					return false;

				if (UiUtility.IsMouse(boundKeyCode))
				{
					int keyCodeInt = (int)(object)boundKeyCode;
					int mouseNumber = keyCodeInt - FIRST_MOUSE_KEY;
					return Input.GetMouseButton(mouseNumber);
				}

				return Input.GetKey(boundKeyCode);
			}

			return false;
		}

		public bool GetKeyDown(KeyCode _originalKeyCode)
		{
			if (m_keyCodes.TryGetValue(_originalKeyCode, out KeyCode boundKeyCode))
			{
				if (boundKeyCode == KeyCode.None)
					return false;

				if (UiUtility.IsMouse(boundKeyCode))
				{
					int keyCodeInt = (int)(object)boundKeyCode;
					int mouseNumber = keyCodeInt - FIRST_MOUSE_KEY;
					return Input.GetMouseButton(mouseNumber);
				}

				return Input.GetKeyDown(boundKeyCode);
			}

			return false;
		}

		public bool HasUnboundKeys()
		{
			foreach (var kv in m_keyCodes)
				if (kv.Value == KeyCode.None)
					return true;
			return false;
		}

		// We need to update our key code dict, when a key binding was changed
		private void OnPlayerSettingChanged( PlayerSetting _playerSetting )
		{
			if (!_playerSetting.IsKeyCode)
				return;

			// Enter the new key binding
			KeyCode original = _playerSetting.GetDefaultValue<KeyCode>();
			KeyCode bound = _playerSetting.GetValue<KeyCode>();
			Debug.Assert(m_keyCodes.ContainsKey(original));
			m_keyCodes[original] = bound;

			// A "key binding" of "None" may occur multiple times, so we need not take care of other entries...
			if (bound == KeyCode.None)
				return;

			// ... but all other entries can only exist once, so we need to find out if an entry already uses this and set it to "None"
			foreach (var kv in m_playerSettings)
			{
				if (!kv.Value.IsKeyCode)
					continue;

				KeyCode currOriginal = kv.Value.GetDefaultValue<KeyCode>();
				if ( currOriginal == original)
					continue;

				KeyCode currBound = kv.Value.GetValue<KeyCode>();
				if (currBound == bound)
				{
					kv.Value.Value = KeyCode.None;
					break;
				}
			}
		}

		[System.Diagnostics.Conditional("DEBUG_PLAYER_SETTINGS")]
		public static void Log(PlayerSetting _playerSetting, string _performedAction)
		{
			Debug.Log($"{_performedAction} player Setting '{_playerSetting.Key}' : {_playerSetting.Value}");
		}

	}
}