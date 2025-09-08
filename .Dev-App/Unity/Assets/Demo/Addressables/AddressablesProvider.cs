using System;
using GuiToolkit.AssetHandling;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;

public sealed class AddressableInstanceHandle : IInstanceHandle
{
	public GameObject Instance { get; }
	private AsyncOperationHandle<GameObject> m_handle;
	public AddressableInstanceHandle( AsyncOperationHandle<GameObject> _handle )
	{
		m_handle = _handle;
		Instance = _handle.Result;
	}

	public void Release()
	{
		UnityEngine.AddressableAssets.Addressables.ReleaseInstance(Instance);
	}
}

public sealed class AddressableAssetHandle<T> : IAssetHandle<T> where T : Object
{
	public T Asset => Handle.Result;
	public object Key { get; }
	public bool IsLoaded => Asset != null;
	public AsyncOperationHandle<T> Handle;
	public AddressableAssetHandle( AsyncOperationHandle<T> handle, object key )
	{
		Handle = handle;
		Key = key;
	}
}

public sealed class AddressablesProvider : IAssetProvider
{
	public async Task<IInstanceHandle> InstantiateAsync( object _key, Transform _parent = null, CancellationToken _cancellationToken = default )
	{
		AsyncOperationHandle<GameObject> handle;
		if (_key is UnityEngine.AddressableAssets.AssetReferenceGameObject assetReferenceGameObject)
			handle = assetReferenceGameObject.InstantiateAsync(_parent);
		else
			handle = UnityEngine.AddressableAssets.Addressables.InstantiateAsync(_key, _parent);

		await handle.Task;
		return new AddressableInstanceHandle(handle);
	}

	public async Task<IAssetHandle<T>> LoadAssetAsync<T>( object _key, CancellationToken _cancellationToken = default ) where T : Object
	{
		var handle = UnityEngine.AddressableAssets.Addressables.LoadAssetAsync<T>(_key);
		await handle.Task;
		return new AddressableAssetHandle<T>(handle, _key);
	}

	public void Release<T>( IAssetHandle<T> handle ) where T : Object
	{
		var assetHandle = (AddressableAssetHandle<T>)handle;
		UnityEngine.AddressableAssets.Addressables.Release(assetHandle.Handle);
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

	public AssetKey NormalizeKey<T>( object _key ) where T : Object
	{
		if (_key is AssetKey assetKey)
		{
			if (!ReferenceEquals(assetKey.Provider, this))
				throw new InvalidOperationException($"Attempt to use Key designed for Asset provider '{assetKey.Provider.GetType().Name}' with '{GetType().Name}'");

			return assetKey;
		}

		// Direct AssetReference
		if (_key is UnityEngine.AddressableAssets.AssetReference assetReference)
		{
			// RuntimeKey is the official, stable ID for Addressables
			return new AssetKey(this, assetReference.RuntimeKey.ToString(), typeof(T));
		}

		// String key/label (Addressables supports both)
		if (_key is string key)
			return new AssetKey(this, $"addr:{key}", typeof(T));

		// Editor convenience: GUID
#if UNITY_EDITOR
		if (_key is Guid guid)
			return new AssetKey(this, $"guid:{guid:N}", typeof(T));
#endif

		return new AssetKey(this, $"unknown:{_key}", typeof(T));
	}
}
