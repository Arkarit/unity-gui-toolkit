using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace GuiToolkit.Storage
{
	public sealed class RoutingByteStore : IByteStore
	{
		private readonly List<Route> m_routes = new List<Route>();
		private readonly IByteStore m_fallbackStore;

		private sealed class Route
		{
			public readonly string prefix;
			public readonly IByteStore store;

			public Route( string _prefix, IByteStore _store )
			{
				prefix = _prefix;
				store = _store;
			}
		}

		public RoutingByteStore( IByteStore _fallbackStore )
		{
			m_fallbackStore = _fallbackStore ?? throw new ArgumentNullException(nameof(_fallbackStore));
		}

		public RoutingByteStore AddRoute( string _prefix, IByteStore _store )
		{
			if (string.IsNullOrWhiteSpace(_prefix))
			{
				throw new ArgumentException("Prefix must not be null or whitespace.", nameof(_prefix));
			}

			if (_store == null)
			{
				throw new ArgumentNullException(nameof(_store));
			}

			m_routes.Add(new Route(_prefix, _store));
			return this;
		}

		public Task<bool> ExistsAsync( string _key, CancellationToken _cancellationToken = default )
		{
			IByteStore store = ResolveStore(_key);
			return store.ExistsAsync(_key, _cancellationToken);
		}

		public Task<byte[]?> LoadAsync( string _key, CancellationToken _cancellationToken = default )
		{
			IByteStore store = ResolveStore(_key);
			return store.LoadAsync(_key, _cancellationToken);
		}

		public Task SaveAsync( string _key, byte[] _data, CancellationToken _cancellationToken = default )
		{
			IByteStore store = ResolveStore(_key);
			return store.SaveAsync(_key, _data, _cancellationToken);
		}

		public Task DeleteAsync( string _key, CancellationToken _cancellationToken = default )
		{
			IByteStore store = ResolveStore(_key);
			return store.DeleteAsync(_key, _cancellationToken);
		}

		public async Task<IReadOnlyList<string>> ListKeysAsync(
			string _prefix,
			CancellationToken _cancellationToken = default )
		{
			if (string.IsNullOrEmpty(_prefix))
			{
				return Array.Empty<string>();
			}

			IByteStore? singleStore = TryResolveStoreForPrefix(_prefix);
			if (singleStore != null)
			{
				return await singleStore.ListKeysAsync(_prefix, _cancellationToken);
			}

			// Prefix does not map to a single store.
			// Aggregate results from all unique stores + fallback and filter by prefix.
			List<IByteStore> stores = CollectUniqueStores();

			List<string> merged = new List<string>();
			for (int i = 0; i < stores.Count; i++)
			{
				IReadOnlyList<string> keys = await stores[i].ListKeysAsync(_prefix, _cancellationToken);
				for (int k = 0; k < keys.Count; k++)
				{
					if (keys[k].StartsWith(_prefix, StringComparison.Ordinal))
					{
						merged.Add(keys[k]);
					}
				}
			}

			return merged;
		}

		private IByteStore ResolveStore( string _key )
		{
			if (string.IsNullOrEmpty(_key))
			{
				return m_fallbackStore;
			}

			int bestLen = -1;
			IByteStore? bestStore = null;

			for (int i = 0; i < m_routes.Count; i++)
			{
				string prefix = m_routes[i].prefix;
				if (_key.StartsWith(prefix, StringComparison.Ordinal))
				{
					if (prefix.Length > bestLen)
					{
						bestLen = prefix.Length;
						bestStore = m_routes[i].store;
					}
				}
			}

			return bestStore ?? m_fallbackStore;
		}

		private IByteStore? TryResolveStoreForPrefix( string _prefix )
		{
			int bestLen = -1;
			IByteStore? bestStore = null;

			for (int i = 0; i < m_routes.Count; i++)
			{
				string routePrefix = m_routes[i].prefix;

				if (_prefix.StartsWith(routePrefix, StringComparison.Ordinal))
				{
					if (routePrefix.Length > bestLen)
					{
						bestLen = routePrefix.Length;
						bestStore = m_routes[i].store;
					}
				}
			}

			if (bestStore != null)
			{
				return bestStore;
			}

			return null;
		}

		private List<IByteStore> CollectUniqueStores()
		{
			List<IByteStore> stores = new List<IByteStore>();

			AddUnique(stores, m_fallbackStore);

			for (int i = 0; i < m_routes.Count; i++)
			{
				AddUnique(stores, m_routes[i].store);
			}

			return stores;
		}

		private static void AddUnique( List<IByteStore> _stores, IByteStore _store )
		{
			for (int i = 0; i < _stores.Count; i++)
			{
				if (ReferenceEquals(_stores[i], _store))
				{
					return;
				}
			}

			_stores.Add(_store);
		}
	}
}
