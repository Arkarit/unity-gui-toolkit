using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using UnityEngine;

namespace GuiToolkit.AssetHandling
{
	public interface IAssetProvider
	{
		// Load prefab without instantiation (optional; can be no-op)
		Task<IAssetHandle<GameObject>> LoadPrefabAsync( object _key, CancellationToken _cancellationToken = default );

		// Instantiate and return a handle that knows how to free itself
		Task<IInstanceHandle> InstantiateAsync( object _key, Transform _parent = null, CancellationToken _cancellationToken = default );

		// Optional preload by label/set
		Task PreloadAsync( IEnumerable<object> _keysOrLabels, CancellationToken _cancellationToken = default );

		// Free loaded prefab handle (not the instance)
		void Release( IAssetHandle<GameObject> _handle );

		// Housekeeping hook
		void ReleaseUnused();
	}
}