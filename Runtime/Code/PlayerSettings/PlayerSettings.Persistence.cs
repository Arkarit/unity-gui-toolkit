using GuiToolkit.Settings;
using System;
using UnityEngine;

namespace GuiToolkit
{
	public partial class PlayerSettings
	{
		private SettingsPersistedAggregate m_persistedAggregate;
		private bool m_isApplyingLoadedValues;
		private System.Threading.Tasks.TaskScheduler m_mainThreadScheduler;

		private bool IsLoaded => m_persistedAggregate != null && m_persistedAggregate.IsLoaded;
		internal void InitializePersistence( SettingsPersistedAggregate _settings )
		{
			m_persistedAggregate = _settings;
			m_mainThreadScheduler = System.Threading.Tasks.TaskScheduler.FromCurrentSynchronizationContext();
			
			UiEventDefinitions.OnTickPerFrame.RemoveListener(Update);
			if (!ManualUpdate)
				UiEventDefinitions.OnTickPerFrame.AddListener(Update);
			
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

		private void HandlePersistence( PlayerSetting _playerSetting )
		{
			if (m_persistedAggregate == null)
				return;

			if (m_isApplyingLoadedValues)
				return;

			if (!_playerSetting.Options.IsSaveable || _playerSetting.IsButton)
				return;

			StoreInAggregate(_playerSetting, true);
		}

	}
}