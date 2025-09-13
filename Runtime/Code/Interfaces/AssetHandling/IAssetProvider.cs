using System;
using System.Threading.Tasks;
using System.Threading;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GuiToolkit.AssetHandling
{
	public interface IAssetProviderEditorBridge
	{
		bool TryMakeId(Object _obj, out string _resId);
	}
	
	public interface IAssetProvider
	{
		// Nice name of the provider for usage in Editor GUI or debugging, e.g. "Default Asset Provider"
		string Name { get; }
		
		// Nice name of the loading type for usage in Editor GUI or debugging, e.g. "Resources"
		string ResName { get; }
		
		IAssetProviderEditorBridge EditorBridge {get;}
		
		bool IsInitialized { get; }
		void Init();

		// Load prefab without instantiation (optional; can be no-op)
		Task<IAssetHandle<T>> LoadAssetAsync<T>( object _key, CancellationToken _cancellationToken = default ) where T : Object;

		// Instantiate and return a handle that knows how to free itself
		Task<IInstanceHandle> InstantiateAsync( object _key, Transform _parent = null, CancellationToken _cancellationToken = default );

		// Free loaded asset handle
		void Release<T>( IAssetHandle<T> _handle ) where T : Object;
		
		// Free loaded instance handle
		void Release(IInstanceHandle _handle);

		// Housekeeping hook
		void ReleaseUnused();
		
		CanonicalAssetKey NormalizeKey<T>(object _key) where T : Object;
		CanonicalAssetKey NormalizeKey(object _key, Type _type);
		
		bool Supports(CanonicalAssetKey _key);
		bool Supports(string _id);
		bool Supports(object _obj);
	}
}