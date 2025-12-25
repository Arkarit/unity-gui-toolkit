using System;
using System.Collections.Generic;

namespace GuiToolkit.Storage
{
	public sealed class StorageRoutingConfig
	{
		public IByteStore localStore;
		public IByteStore? backendStore;
		public ISerializer serializer;

		public readonly Dictionary<string, StoragePolicy> collectionPolicies =
			new Dictionary<string, StoragePolicy>();

		public StorageRoutingConfig( IByteStore _localStore, ISerializer _serializer )
		{
			localStore = _localStore ?? throw new ArgumentNullException(nameof(_localStore));
			serializer = _serializer ?? throw new ArgumentNullException(nameof(_serializer));
		}

		public StorageRoutingConfig SetPolicy( string _collection, StoragePolicy _policy )
		{
			if (string.IsNullOrWhiteSpace(_collection))
			{
				throw new ArgumentException("Collection must not be null or whitespace.", nameof(_collection));
			}

			collectionPolicies[_collection] = _policy;
			return this;
		}
	}
}
