using GuiToolkit.Settings;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace GuiToolkit
{
	public partial class PlayerSettings
	{
		private readonly Dictionary<string, PlayerSetting> m_playerSettings = new();

		private static PlayerSettings s_instance;

		public IInputProxy InputProxy = new UnityInputProxy();

		internal void Initialize( SettingsPersistedAggregate _settings, float _dragTreshold = 5 )
		{
			InitializePersistence(_settings);
			InitializeInput(_dragTreshold);
		}

		internal PlayerSettings()
		{
			UiEventDefinitions.EvPlayerSettingChanged.AddListener(OnPlayerSettingChanged);
		}

		~PlayerSettings()
		{
			UiEventDefinitions.EvPlayerSettingChanged.RemoveListener(OnPlayerSettingChanged);
		}

		public static PlayerSettings Instance
		{
			get
			{
				Bootstrap.ThrowIfNotInitialized();
				return s_instance;
			}

			internal set
			{
				s_instance = value;
			}
		}

		public PlayerSetting GetPlayerSetting( string _key )
		{
			if (m_playerSettings.TryGetValue(_key, out PlayerSetting result))
				return result;

			return null;
		}

		public T GetValue<T>( string _key )
		{
			if (m_playerSettings.TryGetValue(_key, out PlayerSetting ps))
				return ps.GetValue<T>();

			UiLog.LogError($"Player setting with key '{_key}' not found");
			return default;
		}

		public void Add( List<PlayerSetting> _playerSettings )
		{
			Debug.Assert(m_persistedAggregate != null, "PlayerSettings not initialized.");

			foreach (PlayerSetting playerSetting in _playerSettings)
			{
				if (playerSetting == null)
					continue;

				m_playerSettings.Add(playerSetting.Key, playerSetting);
				StoreInAggregate(playerSetting, false);

				if (!playerSetting.IsKeyBinding)
					continue;

				KeyBinding original = playerSetting.GetDefaultValue<KeyBinding>();
				KeyBinding bound = playerSetting.GetValue<KeyBinding>();

				if (m_keyBindings.TryGetValue(original.Encoded, out KeyBinding existing))
				{
					UiLog.LogError(
						$"Default KeyBinding '{existing}' of player setting '{playerSetting.Key}' already exists. " +
						"Each default key binding has to be unique.");
					continue;
				}

				m_keyBindings.Add(original.Encoded, bound);
				m_keyBindingPlayerSettings.Add(original.Encoded, playerSetting);
			}

			foreach (PlayerSetting playerSetting in _playerSettings)
			{
				playerSetting.AllowInvokeEvents = true;

				if (!playerSetting.IsButton)
					playerSetting.InvokeEvents();
			}
		}

		public void Clear()
		{
			foreach (var kv in m_playerSettings)
				kv.Value.Clear();

			m_playerSettings.Clear();
			ClearInput();
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

				if (!playerSetting.IsButton)
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
				if (kv.Value.DefaultValue != null)
					kv.Value.Value = kv.Value.DefaultValue;
		}

		private void OnPlayerSettingChanged( PlayerSetting _playerSetting )
		{
			if (_playerSetting.IsKeyBinding)
				HandleKeyBindings(_playerSetting);

			HandlePersistence(_playerSetting);
		}


		[System.Diagnostics.Conditional("DEBUG_PLAYER_SETTINGS")]
		public static void Log( PlayerSetting _playerSetting, string _performedAction )
		{
			UiLog.LogInternal($"{_performedAction} player Setting '{_playerSetting.Key}' : {_playerSetting.Value}");
		}
	}
}
