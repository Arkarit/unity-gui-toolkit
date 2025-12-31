using System.Collections.Generic;
using System.Threading.Tasks;
using GuiToolkit.Storage;

namespace GuiToolkit.Settings
{
	public class SettingsPersistedAggregate : PersistedAggregate<SettingsData>
	{
		public SettingsPersistedAggregate( IDocumentStore _store, string _collection, string _id ) :
			base(_store, _collection, _id, new SettingsData())
		{
		}

		public int GetInt( string _key, int _default )
		{
			if (!IsLoaded)
				return _default;

			if (State.ints.TryGetValue(_key, out int v))
				return v;

			return _default;
		}

		public void SetInt( string _key, int _value )
		{
			Mutate(s => s.ints[_key] = _value);
		}

		public float GetFloat( string _key, float _default )
		{
			if (!IsLoaded)
				return _default;

			if (State.floats.TryGetValue(_key, out float v))
				return v;

			return _default;
		}

		public void SetFloat( string _key, float _value )
		{
			Mutate(s => s.floats[_key] = _value);
		}

		public bool GetBool( string _key, bool _default )
		{
			if (!IsLoaded)
				return _default;

			if (State.bools.TryGetValue(_key, out bool v))
				return v;

			return _default;
		}

		public void SetBool( string _key, bool _value )
		{
			Mutate(s => s.bools[_key] = _value);
		}

		public string GetString( string _key, string _default )
		{
			if (!IsLoaded)
				return _default;

			if (State.strings.TryGetValue(_key, out string v) && v != null)
				return v;

			return _default;
		}

		public void SetString( string _key, string _value )
		{
			Mutate(s => s.strings[_key] = _value ?? string.Empty);
		}

		public int GetEnumInt( string _key, int _default ) => GetInt(_key, _default);
		public void SetEnumInt( string _key, int _value ) => SetInt(_key, _value);

		protected override SettingsData Merge( SettingsData _incoming )
		{
			if (_incoming == null)
				return m_state;

			if (m_state == null)
				return _incoming;

			foreach (KeyValuePair<string, int> kv in _incoming.ints)
				m_state.ints[kv.Key] = kv.Value;

			foreach (KeyValuePair<string, float> kv in _incoming.floats)
				m_state.floats[kv.Key] = kv.Value;

			foreach (KeyValuePair<string, string> kv in _incoming.strings)
				m_state.strings[kv.Key] = kv.Value ?? string.Empty;

			foreach (KeyValuePair<string, bool> kv in _incoming.bools)
				m_state.bools[kv.Key] = kv.Value;

			return m_state;
		}

	}
}
