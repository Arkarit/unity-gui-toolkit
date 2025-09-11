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

public sealed class AddressableInstanceHandle : IInstanceHandle
{
	private bool m_isReleased;
	private AsyncOperationHandle<GameObject> m_handle;

	public GameObject Instance { get; private set; }

	public AddressableInstanceHandle( AsyncOperationHandle<GameObject> _handle )
	{
		m_handle = _handle;
		Instance = _handle.Result;
	}

	public void Release()
	{
		if (m_isReleased)
			return;

		m_isReleased = true;

		if (Instance)
		{
			Addressables.ReleaseInstance(Instance);
			Instance = null;
			return;
		}

		if (m_handle.IsValid())
			Addressables.Release(m_handle);
	}
}

public sealed class AddressableAssetHandle<T> : IAssetHandle<T> where T : Object
{
	private bool m_isReleased;

	public CanonicalAssetKey Key { get; }
	public AsyncOperationHandle<T> Handle;

	public AddressableAssetHandle( AsyncOperationHandle<T> _handle, CanonicalAssetKey _key )
	{
		Handle = _handle;
		Key = _key;
	}

	public bool IsLoaded => Handle.IsValid() && Handle.Status == AsyncOperationStatus.Succeeded;

	public T Asset => (IsLoaded && !m_isReleased) ? Handle.Result : null;

	public void Release()
	{
		if (m_isReleased)
			return;

		m_isReleased = true;

		if (Handle.IsValid())
			Addressables.Release(Handle);
	}
}

public sealed class AddressablesProvider : IAssetProvider
{
	public static IAssetProviderEditorBridge s_editorBridge;
	public string Name => "Addressables Asset Provider";
	public string ResName => "Addressable";
	public IAssetProviderEditorBridge EditorBridge => s_editorBridge;

	public async Task<IInstanceHandle> InstantiateAsync
	(
		object _key,
		Transform _parent = null,
		CancellationToken _cancellationToken = default
	)
	{
		var assetKey = NormalizeKey<GameObject>(_key);

		if (!assetKey.TryGetValue("addr:", out string addr))
			throw new Exception($"AddressablesProvider.InstantiateAsync: Unsupported key '{assetKey.Id}' (expected 'addr:...').");

		AsyncOperationHandle<GameObject> handle = default;

		try
		{
			handle = Addressables.InstantiateAsync(addr, _parent);

			await handle.Task;

			if (_cancellationToken.IsCancellationRequested)
			{
				if (handle.IsValid() && handle.Status == AsyncOperationStatus.Succeeded && handle.Result)
					Addressables.ReleaseInstance(handle.Result);
				else if (handle.IsValid())
					Addressables.Release(handle);

				_cancellationToken.ThrowIfCancellationRequested();
			}

			if (!handle.IsValid() || handle.Status != AsyncOperationStatus.Succeeded || !handle.Result)
			{
				if (handle.IsValid())
					Addressables.Release(handle);

				throw new Exception($"InstantiateAsync failed for '{assetKey.Id}' (status {handle.Status}).");
			}

			return new AddressableInstanceHandle(handle);
		}
		catch
		{
			if (handle.IsValid())
			{
				if (handle.Status == AsyncOperationStatus.Succeeded && handle.Result)
					Addressables.ReleaseInstance(handle.Result);
				else
					Addressables.Release(handle);
			}
			throw;
		}
	}

	public async Task<IAssetHandle<T>> LoadAssetAsync<T>
	(
		object _key,
		CancellationToken _cancellationToken = default
	) where T : Object
	{
		var assetKey = NormalizeKey<T>(_key);

		if (!assetKey.TryGetValue("addr:", out string addr))
			throw new Exception($"AddressablesProvider.LoadAssetAsync: Unsupported key '{assetKey.Id}' (expected 'addr:...').");

#if UNITY_EDITOR
		if (!Application.isPlaying)
		{
			var h = Addressables.LoadAssetAsync<T>(addr);
			h.WaitForCompletion(); // safe im Editor
			if (!h.IsValid() || h.Status != AsyncOperationStatus.Succeeded)
			{
				if (h.IsValid()) Addressables.Release(h);
				throw new Exception($"LoadAssetAsync<{typeof(T).Name}> failed for '{assetKey.Id}' (status {h.Status}).");
			}
			
			return new AddressableAssetHandle<T>(h, assetKey);
		}
#endif
		
		AsyncOperationHandle<T> handle = default;

		try
		{
			handle = Addressables.LoadAssetAsync<T>(addr);

			await handle.Task;

			if (_cancellationToken.IsCancellationRequested)
			{
				if (handle.IsValid())
					Addressables.Release(handle);

				_cancellationToken.ThrowIfCancellationRequested();
			}

			if (!handle.IsValid() || handle.Status != AsyncOperationStatus.Succeeded)
			{
				if (handle.IsValid())
					Addressables.Release(handle);

				throw new Exception($"LoadAssetAsync<{typeof(T).Name}> failed for '{assetKey.Id}' (status {handle.Status}).");
			}

			return new AddressableAssetHandle<T>(handle, assetKey);
		}
		catch
		{
			if (handle.IsValid())
				Addressables.Release(handle);
			throw;
		}
	}

	public void Release<T>( IAssetHandle<T> _handle ) where T : Object
	{
		_handle?.Release();
	}

	public void Release( IInstanceHandle _handle )
	{
		_handle?.Release();
	}

	public void ReleaseUnused()
	{
		// optional trimming
	}

	public CanonicalAssetKey NormalizeKey<T>( object _key ) where T : Object
	{
		if (_key is CanonicalAssetKey assetKey)
		{
			if (!ReferenceEquals(assetKey.Provider, this))
				throw new InvalidOperationException
				(
					$"Attempt to use Key designed for Asset provider '{assetKey.Provider?.GetType().Name}' with '{GetType().Name}'"
				);

			return assetKey;
		}

		if (_key is AssetReference assetReference)
		{
			var runtimeKey = assetReference.RuntimeKey.ToString();
			if (!runtimeKey.StartsWith("addr:", StringComparison.Ordinal))
				runtimeKey = $"addr:{runtimeKey}";

			return new CanonicalAssetKey(this, runtimeKey, typeof(T));
		}

		if (_key is string s)
		{
			var id = s.StartsWith("addr:", StringComparison.Ordinal) ? s : $"addr:{s}";
			return new CanonicalAssetKey(this, id, typeof(T));
		}

#if UNITY_EDITOR
		if (_key is Object obj)
			if (EditorBridge != null && EditorBridge.TryMakeId(obj, out string id))
				return new CanonicalAssetKey(this, id, obj.GetType());
#else
		if (_key is Object obj)
			throw new InvalidOperationException("Object keys are not supported. Use an 'addr:' path instead.");
#endif

		return new CanonicalAssetKey(this, $"unknown:{_key}", typeof(T));
	}

	public bool Supports( CanonicalAssetKey _key ) => _key.Provider == this;
	public bool Supports( string _id ) => _id.StartsWith("addr:", StringComparison.Ordinal);
	public bool Supports( object _obj )
	{
		if (_obj is CanonicalAssetKey key)
			return Supports(key);

		if (_obj is string id)
			return Supports(id);

		if (_obj is AssetReference)
			return true;

#if UNITY_EDITOR
		if (_obj is Object obj)
			return EditorBridge != null
				&& EditorBridge.TryMakeId(obj, out _);
#endif

		return false;
	}


}
