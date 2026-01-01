using System;
using System.Threading;
using System.Threading.Tasks;

namespace GuiToolkit.Storage
{
	/// <summary>
	/// Helper for managing a persisted aggregate root backed by an IDocumentStore.
	/// </summary>
	/// <typeparam name="T">Aggregate state type.</typeparam>
	/// <remarks>
	/// This class keeps a local state instance, tracks dirty state and provides
	/// load/save operations against a fixed collection/id pair.
	/// </remarks>
	public class PersistedAggregate<T>
	{
		protected readonly IDocumentStore m_store;
		protected readonly string m_collection;
		protected readonly string m_id;

		protected T m_state;
		protected bool m_isLoaded;
		protected bool m_isDirty;

		/// <summary>
		/// Creates a new persisted aggregate wrapper.
		/// </summary>
		/// <param name="_store">Document store used for persistence.</param>
		/// <param name="_collection">Collection name.</param>
		/// <param name="_id">Document id.</param>
		/// <param name="_initialState">Initial state used before the first load.</param>
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

		/// <summary>
		/// Whether the aggregate has been loaded at least once.
		/// </summary>
		/// <returns>True if LoadAsync has completed successfully.</returns>
		public bool IsLoaded => m_isLoaded;
		/// <summary>
		/// Whether the aggregate has local changes that are not persisted yet.
		/// </summary>
		/// <returns>True if Mutate has been called since the last successful save.</returns>
		public bool IsDirty => m_isDirty;

		/// <summary>
		/// Current aggregate state.
		/// </summary>
		/// <returns>The in-memory state instance.</returns>
		public T State
		{
			get
			{
				if (!m_isLoaded)
					throw new InvalidOperationException("Aggregate not loaded yet.");
				return m_state;
			}
		}

		/// <summary>
		/// Loads the persisted state if available and merges it into the current state.
		/// </summary>
		/// <param name="_ct">Cancellation token.</param>
		/// <remarks>
		/// If the document does not exist, the aggregate remains at its current state.
		/// If it exists, Merge() is used to combine incoming and local state.
		/// </remarks>
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

		/// <summary>
		/// Applies a mutation to the state and marks the aggregate as dirty.
		/// </summary>
		/// <param name="_mutation">Mutation to apply to the state.</param>
		/// <exception cref="System.ArgumentNullException">Thrown if mutation is null.</exception>
		public void Mutate( Action<T> _mutation )
		{
			// It is necessary to be able to mutate even before loaded;
			// otherwise it wouldn't be possible to add entries before loading (build up defaults)
			_mutation(m_state);
			m_isDirty = true;
		}

		/// <summary>
		/// Saves the state if it has been loaded and is currently dirty.
		/// </summary>
		/// <param name="_ct">Cancellation token.</param>
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
