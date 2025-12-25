using System;

namespace GuiToolkit.Storage
{
	public static class StorageFactory
	{
		public static IDocumentStore CreateDocumentStore( StorageRoutingConfig _config )
		{
			if (_config == null)
			{
				throw new ArgumentNullException(nameof(_config));
			}

			IByteStore byteStore = CreateByteStore(_config);
			return new DocumentStore(byteStore, _config.serializer);
		}

		public static IByteStore CreateByteStore( StorageRoutingConfig _config )
		{
			RoutingByteStore routing = new RoutingByteStore(_config.localStore);

			foreach (var kvp in _config.collectionPolicies)
			{
				string collection = kvp.Key;
				StoragePolicy policy = kvp.Value;

				string prefix = BuildCollectionPrefix(collection);
				IByteStore store = ResolveStoreForPolicy(_config, policy);

				routing.AddRoute(prefix, store);
			}

			return routing;
		}

		private static IByteStore ResolveStoreForPolicy( StorageRoutingConfig _config, StoragePolicy _policy )
		{
			switch (_policy)
			{
				case StoragePolicy.LocalOnly:
					return _config.localStore;

				case StoragePolicy.BackendOnly:
					if (_config.backendStore == null)
					{
						throw new InvalidOperationException("Backend store is not set.");
					}
					return _config.backendStore;

				case StoragePolicy.MirrorWrite:
					throw new NotSupportedException("MirrorWrite is reserved for later.");

				case StoragePolicy.CacheReadThrough:
					throw new NotSupportedException("CacheReadThrough is reserved for later.");

				default:
					throw new NotSupportedException("Unknown storage policy.");
			}
		}

		private static string BuildCollectionPrefix( string _collection )
		{
			return $"doc/{_collection}/";
		}
	}
}
