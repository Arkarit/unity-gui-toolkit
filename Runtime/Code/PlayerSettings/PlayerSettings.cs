using GuiToolkit.Settings;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace GuiToolkit
{
	public class PlayerSettings
	{
		private readonly Dictionary<string, PlayerSetting> m_playerSettings = new();
		private Dictionary<KeyBinding, KeyBinding> m_keyBindings = new();

		private SettingsPersistedAggregate m_persistedAggregate;
		private bool m_isApplyingLoadedValues;
		private System.Threading.Tasks.TaskScheduler m_mainThreadScheduler;

		internal void Initialize( SettingsPersistedAggregate _settings )
		{
			m_persistedAggregate = _settings;
			m_mainThreadScheduler = System.Threading.Tasks.TaskScheduler.FromCurrentSynchronizationContext();
			Load(
				null,
				ex =>
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

		internal PlayerSettings()
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

				if (playerSetting.IsKeyBinding)
				{
					KeyBinding original = playerSetting.GetDefaultValue<KeyBinding>();
					KeyBinding bound = playerSetting.GetValue<KeyBinding>();

					if (m_keyBindings.TryGetValue(original, out KeyBinding existing))
					{
						UiLog.LogError(
							$"Default KeyBinding '{existing}' of player setting '{playerSetting.Key}' already exists. " +
							"Each default key binding has to be unique.");
						continue;
					}

					m_keyBindings.Add(original, bound);
				}
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
			m_playerSettings.Clear();
			m_keyBindings.Clear();
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
			return GetKey(new KeyBinding(_originalKeyCode));
		}

		public bool GetKeyDown( KeyCode _originalKeyCode )
		{
			return GetKeyDown(new KeyBinding(_originalKeyCode));
		}

		public bool GetKeyUp( KeyCode _originalKeyCode )
		{
			return GetKeyUp(new KeyBinding(_originalKeyCode));
		}

		public bool GetKey( KeyBinding _originalKeyBinding )
		{
			KeyBinding binding = ResolveKey(_originalKeyBinding);
			return IsPressed(binding);
		}

		public bool GetKeyDown( KeyBinding _originalKeyBinding )
		{
			KeyBinding binding = ResolveKey(_originalKeyBinding);
			return IsPressedDown(binding);
		}

		public bool GetKeyUp( KeyBinding _originalKeyBinding )
		{
			KeyBinding binding = ResolveKey(_originalKeyBinding);
			return IsPressedUp(binding);
		}

		public KeyBinding ResolveKey( KeyBinding _originalKeyBinding ) =>
			m_keyBindings.GetValueOrDefault(_originalKeyBinding, _originalKeyBinding);

		public bool HasUnboundKeys()
		{
			foreach (var kv in m_keyBindings)
				if (kv.Value.KeyCode == KeyCode.None)
					return true;

			return false;
		}

		private void OnPlayerSettingChanged( PlayerSetting _playerSetting )
		{
			if (_playerSetting.IsKeyBinding)
			{
				KeyBinding original = _playerSetting.GetDefaultValue<KeyBinding>();
				KeyBinding bound = _playerSetting.GetValue<KeyBinding>();

				Debug.Assert(m_keyBindings.ContainsKey(original));
				m_keyBindings[original] = bound;

				if (bound.KeyCode == KeyCode.None)
					return;

				foreach (var kv in m_playerSettings)
				{
					PlayerSetting ps = kv.Value;
					if (!ps.IsKeyBinding)
						continue;

					KeyBinding currOriginal = ps.GetDefaultValue<KeyBinding>();
					if (currOriginal == original)
						continue;

					KeyBinding currBound = ps.GetValue<KeyBinding>();

					// A) Exact conflict: same key+mods already used
					if (currBound == bound)
					{
						ps.Value = new KeyBinding(KeyCode.None);
						continue;
					}

					// B) New binding uses modifiers -> kick out single-key bindings that use modifier keys as primary
					if (bound.HasKeycodeAsModifier(currBound.KeyCode))
					{
						ps.Value = new KeyBinding(KeyCode.None);
						continue;
					}
					
					// C) New binding is a standalone modifier key -> kick out all bindings which use it as a modifier key
					if (currBound.HasKeycodeAsModifier(bound.KeyCode))
					{
						ps.Value = new KeyBinding(KeyCode.None);
						continue;
					}
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

			if (_ps.Type == typeof(KeyBinding))
			{
				int deflt = _ps.GetDefaultValue<KeyBinding>().Encoded;
				int v = m_persistedAggregate.GetInt(_ps.Key, deflt);
				return new KeyBinding(v);
			}

			if (_ps.Type != null && _ps.Type.IsEnum)
			{
				int deflt = Convert.ToInt32(_ps.DefaultValue);
				int v = m_persistedAggregate.GetEnumInt(_ps.Key, deflt);
				return Enum.ToObject(_ps.Type, v);
			}

			return null;
		}

		private bool AggregateContains( PlayerSetting _ps )
		{
			if (m_persistedAggregate == null || !m_persistedAggregate.IsLoaded)
				return false;

			var key = _ps.Key;
			var type = _ps.Type;

			if (type == typeof(int) || type == typeof(KeyBinding) || type != null && type.IsEnum)
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
			else if (type == typeof(KeyBinding))
				m_persistedAggregate.SetInt(key, _ps.GetValue<KeyBinding>().Encoded);
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

			RebuildKeyBindings();

			foreach (var kv in m_playerSettings)
			{
				PlayerSetting ps = kv.Value;
				ps.AllowInvokeEvents = true;

				if (!ps.IsButton)
					ps.InvokeEvents();
			}

			m_isApplyingLoadedValues = false;
		}

		private void RebuildKeyBindings()
		{
			m_keyBindings.Clear();

			foreach (var kv in m_playerSettings)
			{
				PlayerSetting ps = kv.Value;
				if (!ps.IsKeyBinding)
					continue;

				KeyBinding original = ps.GetDefaultValue<KeyBinding>();

				if (m_keyBindings.ContainsKey(original))
				{
					UiLog.LogError(
						$"Default KeyBinding '{original}' of player setting '{ps.Key}' already exists. " +
						"Each default key binding has to be unique.");
					continue;
				}

				KeyBinding bound = ps.GetValue<KeyBinding>();
				m_keyBindings.Add(original, bound);
			}
		}

		private static bool IsModifierDown( KeyBinding.EModifiers _mod )
		{
			if ((_mod & KeyBinding.EModifiers.Shift) != 0)
			{
				if (!Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift))
					return false;
			}

			if ((_mod & KeyBinding.EModifiers.Ctrl) != 0)
			{
				if (!Input.GetKey(KeyCode.LeftControl) && !Input.GetKey(KeyCode.RightControl))
					return false;
			}

			if ((_mod & KeyBinding.EModifiers.Alt) != 0)
			{
				if (!Input.GetKey(KeyCode.LeftAlt) && !Input.GetKey(KeyCode.RightAlt))
					return false;
			}

			return true;
		}

		private static bool IsPressed( KeyBinding _binding )
		{
			if (_binding.KeyCode == KeyCode.None)
				return false;

			if (!IsModifierDown(_binding.Modifiers))
				return false;

			return Input.GetKey(_binding.KeyCode);
		}

		private static bool IsPressedDown( KeyBinding _binding )
		{
			if (_binding.KeyCode == KeyCode.None)
				return false;

			if (!IsModifierDown(_binding.Modifiers))
				return false;

			return Input.GetKeyDown(_binding.KeyCode);
		}

		private static bool IsPressedUp( KeyBinding _binding )
		{
			if (_binding.KeyCode == KeyCode.None)
				return false;

			if (!IsModifierDown(_binding.Modifiers))
				return false;

			return Input.GetKeyUp(_binding.KeyCode);
		}

		[System.Diagnostics.Conditional("DEBUG_PLAYER_SETTINGS")]
		public static void Log( PlayerSetting _playerSetting, string _performedAction )
		{
			UiLog.LogInternal($"{_performedAction} player Setting '{_playerSetting.Key}' : {_playerSetting.Value}");
		}
	}
}
