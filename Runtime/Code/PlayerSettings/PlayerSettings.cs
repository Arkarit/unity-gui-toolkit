using GuiToolkit.Settings;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GuiToolkit
{
	public class PlayerSettings
	{
		private readonly int FIRST_MOUSE_KEY = (int)(object)KeyCode.Mouse0;
		private readonly Dictionary<string, PlayerSetting> m_playerSettings = new Dictionary<string, PlayerSetting>();
		private Dictionary<KeyCode, KeyCode> m_keyCodes = new Dictionary<KeyCode, KeyCode>();

		private SettingsPersistedAggregate m_persistedAggregate;
		private bool m_isApplyingLoadedValues;
		private System.Threading.Tasks.TaskScheduler m_mainThreadScheduler;

		public void Initialize( SettingsPersistedAggregate _settings )
		{
			m_persistedAggregate = _settings;
			m_mainThreadScheduler = System.Threading.Tasks.TaskScheduler.FromCurrentSynchronizationContext();
			Load(null, ex =>
			{
				UiLog.LogError($"Loading PlayerSettings failed:{ex}");
			});
		}

		public void Load( Action _onSuccess = null, Action<Exception> _onFail = null )
		{
			if (m_persistedAggregate == null)
			{
				_onFail?.Invoke(new InvalidOperationException("PlayerSettings not initialized."));
				return;
			}

			var t = m_persistedAggregate.LoadAsync();
			t.ContinueWith(
				_tt =>
				{
					if (_tt.Exception != null)
					{
						_onFail?.Invoke(_tt.Exception);
						return;
					}

					ApplyLoadedValuesToAll();
					_onSuccess?.Invoke();
				},
				m_mainThreadScheduler);
		}

		public void Save( Action _onSuccess = null, Action<Exception> _onFail = null )
		{
			if (m_persistedAggregate == null)
			{
				_onFail?.Invoke(new InvalidOperationException("PlayerSettings not initialized."));
				return;
			}

			if (!m_persistedAggregate.IsDirty)
				return;

			m_persistedAggregate.Apply(m_playerSettings);
			var t = m_persistedAggregate.SaveAsync();
			t.ContinueWith(
				_tt =>
				{
					if (_tt.Exception != null)
					{
						_onFail?.Invoke(_tt.Exception);
						return;
					}

					_onSuccess?.Invoke();
				},
				m_mainThreadScheduler);
		}

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
				StoreInAggregate( playerSetting, false );
				if (playerSetting.IsKeyCode)
				{
					if (m_keyCodes.TryGetValue(playerSetting.GetDefaultValue<KeyCode>(), out KeyCode existing))
					{
						UiLog.LogError($"Default Key code '{existing}' of player setting '{playerSetting.Key}' already exists. Each default key code has to be unique.");
						continue;
					}

					m_keyCodes.Add(playerSetting.GetDefaultValue<KeyCode>(), playerSetting.GetValue<KeyCode>());
				}
			}

			// We want to only invoke the player settings changed event once after all player settings have been added.
			// Thus second iteration.
			foreach (PlayerSetting playerSetting in _playerSettings)
			{
				playerSetting.AllowInvokeEvents = true;
				if (!playerSetting.IsButton)
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
				kv.Value.Value = kv.Value.DefaultValue;
		}

		public bool GetKey( KeyCode _originalKeyCode )
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

		public bool GetKeyDown( KeyCode _originalKeyCode )
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
				if (currOriginal == original)
					continue;

				KeyCode currBound = kv.Value.GetValue<KeyCode>();
				if (currBound == bound)
				{
					kv.Value.Value = KeyCode.None;
					break;
				}
			}

			if (m_persistedAggregate == null)
				return;

			if (m_isApplyingLoadedValues)
				return;

			if (!_playerSetting.Options.IsSaveable || _playerSetting.IsButton)
				return;

			StoreInAggregate(_playerSetting, true);
		}

		private object TryGetFromAggregate( PlayerSetting _ps )
		{
			if (_ps.Type == typeof(int))
				return m_persistedAggregate.GetInt(_ps.Key, _ps.GetDefaultValue<int>());

			if (_ps.Type == typeof(float))
				return m_persistedAggregate.GetFloat(_ps.Key, _ps.GetDefaultValue<float>());

			if (_ps.Type == typeof(bool))
				return m_persistedAggregate.GetBool(_ps.Key, _ps.GetDefaultValue<bool>());

			if (_ps.Type == typeof(string))
				return m_persistedAggregate.GetString(_ps.Key, _ps.GetDefaultValue<string>());

			if (_ps.Type == typeof(KeyCode))
			{
				int deflt = (int)(object)_ps.GetDefaultValue<KeyCode>();
				int v = m_persistedAggregate.GetEnumInt(_ps.Key, deflt);
				return (KeyCode)(object)v;
			}

			if (_ps.Type != null && _ps.Type.IsEnum)
			{
				int deflt = Convert.ToInt32(_ps.DefaultValue);
				int v = m_persistedAggregate.GetEnumInt(_ps.Key, deflt);
				return Enum.ToObject(_ps.Type, v);
			}

			return null;
		}

		private bool AggregateContains(PlayerSetting _ps)
		{
			if (!m_persistedAggregate.IsLoaded)
				return false;

			var key = _ps.Key;
			var type = _ps.Type;

			if (type == typeof(int) || type == typeof(KeyCode) || type != null && type.IsEnum)
				return m_persistedAggregate.ContainsInt(key);

			if (type == typeof(float))
				return m_persistedAggregate.ContainsFloat(key);

			if (type == typeof(bool))
				return m_persistedAggregate.ContainsBool(key);

			if (type == typeof(string))
				return m_persistedAggregate.ContainsString(key);

			return false;
		}

		private void StoreInAggregate( PlayerSetting _ps, bool _overwrite )
		{
			if (!_overwrite && AggregateContains(_ps))
				return;

			var key = _ps.Key;
			var type = _ps.Type;

			if (type == typeof(int))
				m_persistedAggregate.SetInt(key, _ps.GetValue<int>());
			else if (type == typeof(float))
				m_persistedAggregate.SetFloat(key, _ps.GetValue<float>());
			else if (type == typeof(bool))
				m_persistedAggregate.SetBool(key, _ps.GetValue<bool>());
			else if (type == typeof(string))
				m_persistedAggregate.SetString(key, _ps.GetValue<string>());
			else if (type == typeof(KeyCode))
				m_persistedAggregate.SetEnumInt(key, (int)(object)_ps.GetValue<KeyCode>());
			else if (type != null && type.IsEnum)
				m_persistedAggregate.SetEnumInt(key, Convert.ToInt32(_ps.Value));
		}

		private void ApplyLoadedValuesToAll()
		{
			if (m_persistedAggregate == null)
				return;

			m_isApplyingLoadedValues = true;

			foreach (var kv in m_playerSettings)
			{
				PlayerSetting ps = kv.Value;
				if (!ps.Options.IsSaveable || ps.IsButton)
					continue;

				object loaded = TryGetFromAggregate(ps);
				if (loaded != null)
					ps.SetValueSilent(loaded);
			}

			RebuildKeyCodes();

			foreach (var kv in m_playerSettings)
			{
				PlayerSetting ps = kv.Value;
				ps.AllowInvokeEvents = true;

				if (!ps.IsButton)
					ps.InvokeEvents();
			}

			m_isApplyingLoadedValues = false;
		}

		private void RebuildKeyCodes()
		{
			m_keyCodes.Clear();

			foreach (var kv in m_playerSettings)
			{
				PlayerSetting ps = kv.Value;
				if (!ps.IsKeyCode)
					continue;

				KeyCode original = ps.GetDefaultValue<KeyCode>();

				if (m_keyCodes.ContainsKey(original))
				{
					UiLog.LogError(
						$"Default Key code '{original}' of player setting '{ps.Key}' already exists. " +
						"Each default key code has to be unique.");
					continue;
				}

				KeyCode bound = ps.GetValue<KeyCode>();
				m_keyCodes.Add(original, bound);
			}
		}

		[System.Diagnostics.Conditional("DEBUG_PLAYER_SETTINGS")]
		public static void Log( PlayerSetting _playerSetting, string _performedAction )
		{
			UiLog.Log($"{_performedAction} player Setting '{_playerSetting.Key}' : {_playerSetting.Value}");
		}

	}
}