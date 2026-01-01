using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace GuiToolkit.Storage
{
	/// <summary>
	/// Global entry point for configuring and accessing the storage system.
	/// </summary>
	/// <remarks>
	/// Call Initialize() once during application startup.
	/// After initialization, access the configured document store via Documents.
	/// </remarks>
	public static class Storage
	{
		private static IDocumentStore? s_documents;

		private static SynchronizationContext? s_mainContext;

		/// <summary>
		/// Posts an action to Unity's main thread synchronization context.
		/// </summary>
		/// <param name="_action">Action to execute on the main thread.</param>
		/// <remarks>
		/// Initialize() captures the current SynchronizationContext as the main thread context.
		/// </remarks>
		public static void PostToMainThread( Action _action )
		{
			if (s_mainContext == null)
			{
				_action();
				return;
			}

			s_mainContext.Post(_ => _action(), null);
		}

		/// <summary>
		/// Gets the configured document store.
		/// </summary>
		/// <returns>The initialized document store.</returns>
		/// <exception cref="System.InvalidOperationException">Thrown if Initialize() has not been called yet.</exception>
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

		/// <summary>
		/// Initializes the storage system from routing configurations.
		/// </summary>
		/// <param name="_routingConfigs">Routing configurations describing stores, serializer and per-collection policies.</param>
		/// <exception cref="System.ArgumentNullException">Thrown if routingConfigs is null.</exception>
		/// <exception cref="System.ArgumentException">Thrown if routingConfigs is empty or invalid.</exception>
		/// <remarks>
		/// This method captures the current SynchronizationContext as the main thread context.
		/// </remarks>
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

		[Conditional("DEBUG_STORAGE")]
		/// <summary>
		/// Debug logging hook used when DEBUG_STORAGE is enabled.
		/// </summary>
		/// <param name="s">Message to log.</param>
		/// <remarks>
		/// This method is compiled only when the DEBUG_STORAGE symbol is defined.
		/// </remarks>
		public static void Log( string s )
		{
			UiLog.Log($"---::: DebugStorage: {s}");
		}
	}
}
