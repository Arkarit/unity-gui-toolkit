using System.Threading.Tasks;
using GuiToolkit.Storage;

namespace GuiToolkit.Settings
{
	public sealed class SettingsPersistedAggregate
	{
		private readonly PersistedAggregate<SettingsData> m_aggregate;

		public SettingsPersistedAggregate( IDocumentStore _store )
		{
			m_aggregate = new PersistedAggregate<SettingsData>(
				_store,
				_collection: "settings",
				_id: "user",
				_initialState: new SettingsData()
			);
		}

		public Task LoadAsync()
		{
			return m_aggregate.LoadAsync();
		}

		public Task SaveAsync()
		{
			return m_aggregate.SaveAsync();
		}

		public int GetInt( string _key, int _default )
		{
			if (m_aggregate.State.ints.TryGetValue(_key, out int v))
				return v;
			return _default;
		}

		public void SetInt( string _key, int _value )
		{
			m_aggregate.Mutate(s => s.ints[_key] = _value);
		}

		public float GetFloat( string _key, float _default )
		{
			if (m_aggregate.State.floats.TryGetValue(_key, out float v))
				return v;
			return _default;
		}

		public void SetFloat( string _key, float _value )
		{
			m_aggregate.Mutate(s => s.floats[_key] = _value);
		}

		public bool GetBool( string _key, bool _default )
		{
			if (m_aggregate.State.bools.TryGetValue(_key, out bool v))
				return v;
			return _default;
		}

		public void SetBool( string _key, bool _value )
		{
			m_aggregate.Mutate(s => s.bools[_key] = _value);
		}

		public string GetString( string _key, string _default )
		{
			if (m_aggregate.State.strings.TryGetValue(_key, out string v) && v != null)
				return v;
			return _default;
		}

		public void SetString( string _key, string _value )
		{
			m_aggregate.Mutate(s => s.strings[_key] = _value ?? string.Empty);
		}

		public int GetEnumInt( string _key, int _default ) => GetInt(_key, _default);
		public void SetEnumInt( string _key, int _value ) => SetInt(_key, _value);
	}
}
