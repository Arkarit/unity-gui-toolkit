using System;
using GuiToolkit.AssetHandling;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using Object = UnityEngine.Object;
using Addressables = UnityEngine.AddressableAssets.Addressables;
using AssetReference = UnityEngine.AddressableAssets.AssetReference;
using AssetReferenceGameObject = UnityEngine.AddressableAssets.AssetReferenceGameObject;

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
		Addressables.ReleaseInstance(Instance);
	}
}

public sealed class AddressableAssetHandle<T> : IAssetHandle<T> where T : Object
{
	public T Asset => Handle.Result;
	public AssetKey Key { get; }
	public bool IsLoaded => Asset != null;
	public void Release()
	{
		if ( Handle.IsValid())
			Addressables.Release(Handle);
	}

	public AsyncOperationHandle<T> Handle;
	public AddressableAssetHandle( AsyncOperationHandle<T> _handle, AssetKey _key )
	{
		Handle = _handle;
		Key = _key;
	}
}

public sealed class AddressablesProvider : IAssetProvider
{
	public async Task<IInstanceHandle> InstantiateAsync( object _key, Transform _parent = null, CancellationToken _cancellationToken = default )
	{
		var key = NormalizeKey<GameObject>(_key);
		var handle = Load<GameObject>(key, _cancellationToken);
		await handle.Task;
		return new AddressableInstanceHandle(handle);
	}

	public async Task<IAssetHandle<T>> LoadAssetAsync<T>( object _key, CancellationToken _cancellationToken = default ) where T : Object
	{
		var key = NormalizeKey<T>(_key);
		
		var handle = Load<T>(key, _cancellationToken);
		await handle.Task;
		return new AddressableAssetHandle<T>(handle, key);
	}

	public void Release<T>( IAssetHandle<T> _handle ) where T : Object => _handle.Release();
	public void Release(IInstanceHandle _handle) => _handle.Release();

	public async Task PreloadAsync( IEnumerable<object> keysOrLabels, CancellationToken ct = default )
	{
		foreach (var keyOrLabel in keysOrLabels)
		{
			var handle = Addressables.LoadAssetAsync<GameObject>(keyOrLabel);
			await handle.Task;
			Addressables.Release(handle);
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
		if (_key is AssetReference assetReference)
		{
			// RuntimeKey is the official, stable ID for Addressables
			string runtimeKey = assetReference.RuntimeKey.ToString();
			return new AssetKey(this, $"addr:{runtimeKey}", typeof(T));
		}

		// String key/label (Addressables supports both)
		if (_key is string key)
			return new AssetKey(this, $"addr:{key}", typeof(T));

		return new AssetKey(this, $"unknown:{_key}", typeof(T));
	}

	public AsyncOperationHandle<T> Load<T>( AssetKey _assetKey, CancellationToken _cancellationToken ) where T : Object
	{
		AsyncOperationHandle<T> result = new AsyncOperationHandle<T>();

		if (_assetKey.TryGetValue("addr:", out string addr))
			result = Addressables.LoadAssetAsync<T>(addr);

		_cancellationToken.ThrowIfCancellationRequested();

		if (!result.IsValid())
			throw new Exception($"Could not load asset: '{_assetKey.Id}'");

		return result;
	}
}
