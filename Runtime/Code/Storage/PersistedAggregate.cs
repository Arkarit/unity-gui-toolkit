using System;
using System.Threading;
using System.Threading.Tasks;

namespace GuiToolkit.Storage
{
	public class PersistedAggregate<T>
	{
		protected readonly IDocumentStore m_store;
		protected readonly string m_collection;
		protected readonly string m_id;

		protected T m_state;
		protected bool m_isLoaded;
		protected bool m_isDirty;

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

		public bool IsLoaded => m_isLoaded;
		public bool IsDirty => m_isDirty;

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
				m_state = Merge(loaded);
			}

			m_isLoaded = true;
			m_isDirty = false;
		}

		public void Mutate( Action<T> _mutation )
		{
			// It is necessary to be able to mutate even before loaded;
			// otherwise it wouldn't be possible to add entries before loading (build up defaults)
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

		protected virtual T Merge(T _incoming) => _incoming;
	}
}
