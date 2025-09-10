using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using UnityEngine;

namespace GuiToolkit.AssetHandling
{
	public interface IAssetProvider
	{
		// Load prefab without instantiation (optional; can be no-op)
		Task<IAssetHandle<T>> LoadAssetAsync<T>( object _key, CancellationToken _cancellationToken = default ) where T : Object;

		// Instantiate and return a handle that knows how to free itself
		Task<IInstanceHandle> InstantiateAsync( object _key, Transform _parent = null, CancellationToken _cancellationToken = default );

		// Optional preload by label/set
		Task PreloadAsync( IEnumerable<object> _keysOrLabels, CancellationToken _cancellationToken = default );

		// Free loaded asset handle
		void Release<T>( IAssetHandle<T> _handle ) where T : Object;
		
		// Free loaded instance handle
		void Release(IInstanceHandle _handle);

		// Housekeeping hook
		void ReleaseUnused();
		
		AssetKey NormalizeKey<T>(object _key) where T : Object;
		
		bool Supports(AssetKey _key);
		bool Supports(string _id);
		bool Supports(object _obj);
	}
}