using System.Collections.Generic;
using UnityEngine;

namespace GuiToolkit.Storage
{
	/// <summary>
	/// Default storage factory used by the application.
	/// </summary>
	/// <remarks>
	/// Creates a file-based local store rooted at Application.persistentDataPath
	/// and uses JSON serialization via Newtonsoft.Json.
	/// </remarks>
	[CreateAssetMenu]
	public class DefaultStorageFactory : AbstractStorageFactory
	{
		/// <summary>
		/// Builds the routing configuration list for the storage system.
		/// </summary>
		/// <returns>Routing configurations to be passed to Storage.Initialize().</returns>
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