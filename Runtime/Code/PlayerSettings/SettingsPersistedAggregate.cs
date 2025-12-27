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

		// analog: GetFloat / SetFloat / GetBool / SetBool / ...
	}
}
