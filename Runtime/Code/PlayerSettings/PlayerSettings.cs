using GuiToolkit.Settings;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace GuiToolkit
{
	public class PlayerSettings
	{
		private readonly Dictionary<string, PlayerSetting> m_playerSettings = new();
		private readonly Dictionary<int, KeyBinding> m_keyBindings = new();
		private readonly Dictionary<int, PlayerSetting> m_keyBindingPlayerSettings = new();
		private readonly HashSet<int> m_activeBindings = new();

		private SettingsPersistedAggregate m_persistedAggregate;
		private bool m_isApplyingLoadedValues;
		private System.Threading.Tasks.TaskScheduler m_mainThreadScheduler;

		public IInputProxy InputProxy = new UnityInputProxy();

		internal void Initialize( SettingsPersistedAggregate _settings )
		{
			m_persistedAggregate = _settings;
			m_mainThreadScheduler = System.Threading.Tasks.TaskScheduler.FromCurrentSynchronizationContext();
			UiEventDefinitions.OnTickPerFrame.AddListener(Update);
			Load(null, ex =>
			{
				UiLog.LogError($"Loading PlayerSettings failed:{ex}");
			});
		}

		private readonly HashSet<PlayerSetting> m_activeKeyBindingSettings = new();
		
		private void Update( int _ )
		{
			if (!m_persistedAggregate.IsLoaded)
				return;
			
			Event e = Event.current;
			KeyCode keyCode = UiUtility.EventToKeyCode(e);
			KeyBinding.EModifiers modifiers = GetCurrentModifiers();
			var currentKey = new KeyBinding(keyCode, modifiers);
			
			bool foundKey = m_keyBindingPlayerSettings.TryGetValue(currentKey.Encoded, out PlayerSetting playerSetting);
			
			if (foundKey)
			{
				if (IsPressedUp(currentKey))
				{
					m_activeKeyBindingSettings.Remove(playerSetting);
					playerSetting.OnUp.Invoke();
				}
			}

			foreach (var activePlayerSetting in m_activeKeyBindingSettings)
				activePlayerSetting.WhilePressed.Invoke();
			
			if (foundKey)
			{
				if (IsPressedDown(currentKey))
				{
					m_activeKeyBindingSettings.Add(playerSetting);
					playerSetting.OnDown.Invoke();
				}
			}
		}
		
		private KeyBinding.EModifiers GetCurrentModifiers()
		{
			KeyBinding.EModifiers mods = KeyBinding.EModifiers.None;

			if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
				mods |= KeyBinding.EModifiers.Shift;

			if (Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
				mods |= KeyBinding.EModifiers.Ctrl;

			if (Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt))
				mods |= KeyBinding.EModifiers.Alt;

			return mods;
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
			m_keyBindingPlayerSettings.Clear();
			
			m_keyBindings.Clear();
			m_activeBindings.Clear();
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

		public bool GetKey( KeyCode _originalKeyCode ) => GetKey(new KeyBinding(_originalKeyCode));

		public bool GetKeyDown( KeyCode _originalKeyCode ) => GetKeyDown(new KeyBinding(_originalKeyCode));

		public bool GetKeyUp( KeyCode _originalKeyCode ) => GetKeyUp(new KeyBinding(_originalKeyCode));

		public bool GetKey( KeyBinding _originalKeyBinding )
		{
			KeyBinding binding = ResolveKey(_originalKeyBinding);
			return IsPressed(binding);
		}

		public bool GetKeyDown( KeyBinding _original )
		{
			KeyBinding resolved = ResolveKey(_original);
			bool isPressed = IsPressedDown(resolved);
			bool wasActive = m_activeBindings.Contains(resolved.Encoded);

			if (isPressed && !wasActive)
			{
				m_activeBindings.Add(resolved.Encoded);
				return true;
			}

			return false;
		}

		public bool GetKeyUp( KeyBinding _original )
		{
			KeyBinding resolved = ResolveKey(_original);
			bool isPressed = IsPressedUp(resolved);
			bool wasActive = m_activeBindings.Contains(resolved.Encoded);

			if (wasActive && !isPressed)
			{
				m_activeBindings.Remove(resolved.Encoded);
				return true;
			}

			return false;
		}

		public KeyBinding ResolveKey( KeyBinding _originalKeyBinding ) =>
			m_keyBindings.GetValueOrDefault(_originalKeyBinding.Encoded, _originalKeyBinding);

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

				Debug.Assert(m_keyBindings.ContainsKey(original.Encoded));
				m_keyBindings[original.Encoded] = bound;

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

				if (m_keyBindings.ContainsKey(original.Encoded))
				{
					UiLog.LogError(
						$"Default KeyBinding '{original}' of player setting '{ps.Key}' already exists. " +
						"Each default key binding has to be unique.");
					continue;
				}

				KeyBinding bound = ps.GetValue<KeyBinding>();
				m_keyBindings.Add(original.Encoded, bound);
			}
		}

		private bool IsModifierActive( KeyBinding.EModifiers _mod )
		{
			if ((_mod & KeyBinding.EModifiers.Shift) != 0)
			{
				if (!InputProxy.GetKey(KeyCode.LeftShift) && !InputProxy.GetKey(KeyCode.RightShift))
					return false;
			}

			if ((_mod & KeyBinding.EModifiers.Ctrl) != 0)
			{
				if (!InputProxy.GetKey(KeyCode.LeftControl) && !InputProxy.GetKey(KeyCode.RightControl))
					return false;
			}

			if ((_mod & KeyBinding.EModifiers.Alt) != 0)
			{
				if (!InputProxy.GetKey(KeyCode.LeftAlt) && !InputProxy.GetKey(KeyCode.RightAlt))
					return false;
			}

			return true;
		}

		private bool IsPressed( KeyBinding _binding )
		{
			if (_binding.KeyCode == KeyCode.None)
				return false;

			if (!IsModifierActive(_binding.Modifiers))
				return false;

			return InputProxy.GetKey(_binding.KeyCode);
		}

		private bool IsPressedDown( KeyBinding _binding )
		{
			if (_binding.KeyCode == KeyCode.None)
				return false;

			if (!IsModifierActive(_binding.Modifiers))
				return false;

			return InputProxy.GetKeyDown(_binding.KeyCode);
		}

		private bool IsPressedUp( KeyBinding _binding )
		{
			if (_binding.KeyCode == KeyCode.None)
				return false;

			if (!m_activeBindings.Contains(_binding.Encoded))
				return false;

			// Key went up?
			if (InputProxy.GetKeyUp(_binding.KeyCode))
				return true;

			// Modifier went up?
			if ((_binding.Modifiers & KeyBinding.EModifiers.Shift) != 0)
				if (InputProxy.GetKeyUp(KeyCode.LeftShift) || InputProxy.GetKeyUp(KeyCode.RightShift))
					return true;

			if ((_binding.Modifiers & KeyBinding.EModifiers.Ctrl) != 0)
				if (InputProxy.GetKeyUp(KeyCode.LeftControl) || InputProxy.GetKeyUp(KeyCode.RightControl))
					return true;

			if ((_binding.Modifiers & KeyBinding.EModifiers.Alt) != 0)
				if (InputProxy.GetKeyUp(KeyCode.LeftAlt) || InputProxy.GetKeyUp(KeyCode.RightAlt))
					return true;

			return false;
		}

		[System.Diagnostics.Conditional("DEBUG_PLAYER_SETTINGS")]
		public static void Log( PlayerSetting _playerSetting, string _performedAction )
		{
			UiLog.LogInternal($"{_performedAction} player Setting '{_playerSetting.Key}' : {_playerSetting.Value}");
		}
	}
}
