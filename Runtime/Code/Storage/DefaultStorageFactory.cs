using System.Collections.Generic;
using UnityEngine;

namespace GuiToolkit.Storage
{
	[CreateAssetMenu]
	public class DefaultStorageFactory : AbstractStorageFactory
	{
		public override IReadOnlyList<StorageRoutingConfig> CreateRoutingConfigs()
		{
			List<StorageRoutingConfig> routingConfigs = new();
			IByteStore byteStore = new FileByteStore(Application.persistentDataPath);
			ISerializer serializer = new NewtonsoftJsonSerializer();
			var config = new StorageRoutingConfig(byteStore, serializer);
			config.SetPolicy(StringConstants.PLAYER_SETTINGS_COLLECTION, StoragePolicy.LocalOnly);
			routingConfigs.Add(config);

			return routingConfigs;
		}
	}
}