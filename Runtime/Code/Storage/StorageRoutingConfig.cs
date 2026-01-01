using System;
using System.Collections.Generic;

namespace GuiToolkit.Storage
{
	/// <summary>
	/// Configuration container describing how storage should be wired and routed.
	/// </summary>
	/// <remarks>
	/// Holds references to local/backend stores, serializer and per-collection policies.
	/// Instances are typically created by an AbstractStorageFactory.
	/// </remarks>
	public sealed class StorageRoutingConfig
	{
		/// <summary>
		/// Local store used for collections routed to local persistence.
		/// </summary>
		/// <returns>Local store instance.</returns>
		public IByteStore localStore;
		/// <summary>
		/// Optional backend store used for collections routed to backend persistence.
		/// </summary>
		/// <returns>Backend store instance, or null.</returns>
		public IByteStore? backendStore;
		/// <summary>
		/// Serializer used by the document store.
		/// </summary>
		/// <returns>Serializer instance.</returns>
		public ISerializer serializer;

		/// <summary>
		/// Per-collection routing policies.
		/// </summary>
		/// <returns>Mapping from collection name to policy.</returns>
		public readonly Dictionary<string, StoragePolicy> collectionPolicies =
			new Dictionary<string, StoragePolicy>();

		/// <summary>
		/// Creates a new routing config.
		/// </summary>
		/// <param name="_localStore">Local byte store (required).</param>
		/// <param name="_serializer">Serializer used by the document store (required).</param>
		public StorageRoutingConfig( IByteStore _localStore, ISerializer _serializer )
		{
			localStore = _localStore ?? throw new ArgumentNullException(nameof(_localStore));
			serializer = _serializer ?? throw new ArgumentNullException(nameof(_serializer));
		}

		/// <summary>
		/// Assigns a routing policy for a collection.
		/// </summary>
		/// <param name="_collection">Collection name.</param>
		/// <param name="_policy">Policy to apply.</param>
		/// <returns>This instance for fluent configuration.</returns>
		public StorageRoutingConfig SetPolicy( string _collection, StoragePolicy _policy = StoragePolicy.LocalOnly )
		{
			if (string.IsNullOrWhiteSpace(_collection))
			{
				throw new ArgumentException("Collection must not be null or whitespace.", nameof(_collection));
			}

			collectionPolicies[_collection] = _policy;
			return this;
		}

		/// <summary>
		/// Checks whether the config contains an explicit policy for a collection.
		/// </summary>
		/// <param name="_collection">Collection name.</param>
		/// <returns>True if a policy exists; otherwise false.</returns>
		public bool HasCollection(string _collection) => collectionPolicies.ContainsKey(_collection);
	}
}
