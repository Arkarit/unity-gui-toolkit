using System;
using System.Threading;
using System.Threading.Tasks;

namespace GuiToolkit.Storage
{
	public sealed class PersistedAggregate<T>
	{
		private readonly IDocumentStore m_store;
		private readonly string m_collection;
		private readonly string m_id;

		private T m_state;
		private bool m_isLoaded;
		private bool m_isDirty;

		public PersistedAggregate(
			IDocumentStore _store,
			string _collection,
			string _id,
			T _initialState )
		{
			m_store = _store ?? throw new ArgumentNullException(nameof(_store));
			m_collection = _collection ?? throw new ArgumentNullException(nameof(_collection));
			m_id = _id ?? throw new ArgumentNullException(nameof(_id));
			m_state = _initialState;
		}

		public T State
		{
			get
			{
				if (!m_isLoaded)
					throw new InvalidOperationException("Aggregate not loaded yet.");
				return m_state;
			}
		}

		public async Task LoadAsync( CancellationToken _ct = default )
		{
			T loaded = await m_store.LoadAsync<T>(m_collection, m_id, _ct);
			if (loaded != null)
			{
				m_state = loaded;
			}

			m_isLoaded = true;
			m_isDirty = false;
		}

		public void Mutate( Action<T> _mutation )
		{
			if (!m_isLoaded)
				throw new InvalidOperationException("Aggregate not loaded yet.");

			_mutation(m_state);
			m_isDirty = true;
		}

		public async Task SaveAsync( CancellationToken _ct = default )
		{
			if (!m_isLoaded || !m_isDirty)
				return;

			await m_store.SaveAsync(m_collection, m_id, m_state, _ct);
			m_isDirty = false;
		}
	}
}
