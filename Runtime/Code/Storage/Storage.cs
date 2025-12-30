using System;
using System.Collections.Generic;
using System.Threading;

namespace GuiToolkit.Storage
{
	public static class Storage
	{
		private static IDocumentStore? s_documents;

		private static SynchronizationContext? s_mainContext;

		public static void PostToMainThread( Action _action )
		{
			if (s_mainContext == null)
			{
				_action();
				return;
			}

			s_mainContext.Post(_ => _action(), null);
		}

		public static IDocumentStore Documents
		{
			get
			{
				if (s_documents == null)
				{
					throw new InvalidOperationException("Storage is not initialized.");
				}

				return s_documents;
			}
		}

		public static void Initialize( IReadOnlyList<StorageRoutingConfig> _routingConfigs )
		{
			if (!GeneralUtility.InMainThread)
				throw new InvalidOperationException("Storage.Initialize() must be called from the Unity main thread.");

			s_mainContext = SynchronizationContext.Current;

			if (_routingConfigs == null)
			{
				throw new ArgumentNullException(nameof(_routingConfigs));
			}

			if (_routingConfigs.Count == 0)
			{
				throw new ArgumentException("Routing configs must not be empty.", nameof(_routingConfigs));
			}

			if (s_documents != null)
			{
				throw new InvalidOperationException("Storage is already initialized.");
			}

			StorageRoutingConfig firstConfig = _routingConfigs[0];
			if (firstConfig == null)
			{
				throw new ArgumentException("Routing configs must not contain null entries.", nameof(_routingConfigs));
			}

			ISerializer serializer = firstConfig.serializer ??
				throw new InvalidOperationException("Routing config serializer must not be null.");

			RoutingByteStore routingStore = new RoutingByteStore(firstConfig.localStore);

			HashSet<string> seenCollections = new HashSet<string>(StringComparer.Ordinal);

			for (int i = 0; i < _routingConfigs.Count; i++)
			{
				StorageRoutingConfig config = _routingConfigs[i];
				if (config == null)
				{
					throw new ArgumentException("Routing configs must not contain null entries.", nameof(_routingConfigs));
				}

				if (config.serializer == null)
				{
					throw new InvalidOperationException("Routing config serializer must not be null.");
				}

				if (config.serializer.GetType() != serializer.GetType())
				{
					throw new InvalidOperationException(
						"All routing configs must use the same serializer type.");
				}

				foreach (KeyValuePair<string, StoragePolicy> kv in config.collectionPolicies)
				{
					string collection = kv.Key;
					StoragePolicy policy = kv.Value;

					if (string.IsNullOrWhiteSpace(collection))
					{
						throw new InvalidOperationException("Collection id must not be null or whitespace.");
					}

					if (seenCollections.Add(collection) == false)
					{
						throw new InvalidOperationException(
							$"Duplicate storage policy for collection '{collection}'.");
					}

					string prefix = $"doc/{collection}/";
					IByteStore targetStore = ResolvePolicyStore(config, policy, collection);

					routingStore.AddRoute(prefix, targetStore);
				}
			}

			s_documents = new DocumentStore(routingStore, serializer);
		}

		private static IByteStore ResolvePolicyStore(
			StorageRoutingConfig _config,
			StoragePolicy _policy,
			string _collection )
		{
			switch (_policy)
			{
				case StoragePolicy.LocalOnly:
					return _config.localStore;

				case StoragePolicy.BackendOnly:
					if (_config.backendStore == null)
					{
						throw new InvalidOperationException(
							$"Collection '{_collection}' uses BackendOnly but backendStore is null.");
					}

					return _config.backendStore;

				case StoragePolicy.MirrorWrite:
				case StoragePolicy.CacheReadThrough:
					throw new NotSupportedException(
						$"StoragePolicy '{_policy}' is not implemented yet.");

				default:
					throw new ArgumentOutOfRangeException(nameof(_policy), _policy, "Unknown StoragePolicy.");
			}
		}
	}
}
