using GuiToolkit.AssetHandling;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;

public sealed class AddrInstanceHandle : IInstanceHandle
{
	public Object Instance { get; }
	private UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<GameObject> _h;
	public AddrInstanceHandle( UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<GameObject> h ) { _h = h; Instance = h.Result; }
	Object IInstanceHandle.Instance => Instance;

	public void Release() { UnityEngine.AddressableAssets.Addressables.ReleaseInstance(Instance); }
	public T GetInstance<T>() where T : Object => (T)Instance;
}

public sealed class AddrAssetHandle : IAssetHandle
{
	public Object Asset => Handle.Result;
	public object Key { get; }
	public bool IsLoaded => Asset != null;
	public T GetAsset<T>() where T : Object => (T)Asset;
	public UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<GameObject> Handle;
	public AddrAssetHandle( UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<GameObject> handle, object key )
	{
		Handle = handle;
		Key = key;
	}
}

public sealed class AddressablesProvider : IAssetProvider
{
	public async Task<IInstanceHandle> InstantiateAsync( object key, Transform parent = null, CancellationToken ct = default )
	{
		UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle<GameObject> h;
		if (key is UnityEngine.AddressableAssets.AssetReferenceGameObject ar)
			h = ar.InstantiateAsync(parent);
		else
			h = UnityEngine.AddressableAssets.Addressables.InstantiateAsync(key, parent);

		await h.Task;
		return new AddrInstanceHandle(h);
	}

	public async Task<IAssetHandle> LoadPrefabAsync( object key, CancellationToken ct = default )
	{
		var h = UnityEngine.AddressableAssets.Addressables.LoadAssetAsync<GameObject>(key);
		await h.Task;
		return new AddrAssetHandle(h, key);
	}

	public void Release( IAssetHandle handle )
	{
		var a = (AddrAssetHandle)handle;
		UnityEngine.AddressableAssets.Addressables.Release(a.Handle);
	}

	public async Task PreloadAsync( IEnumerable<object> keysOrLabels, CancellationToken ct = default )
	{
		foreach (var keyOrLabel in keysOrLabels)
		{
			var handle = UnityEngine.AddressableAssets.Addressables.LoadAssetAsync<GameObject>(keyOrLabel);
			await handle.Task;
			UnityEngine.AddressableAssets.Addressables.Release(handle);
		}
	}

	public void ReleaseUnused() { /* optional trimming */ }

	public IAssetHandle NormalizeKey( object _key ) => new AddrAssetHandle(new AsyncOperationHandle<GameObject>(), _key);
}
