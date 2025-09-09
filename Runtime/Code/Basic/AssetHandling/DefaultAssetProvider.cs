using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System;
using UnityEngine;

using Object = UnityEngine.Object;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GuiToolkit.AssetHandling
{
	public sealed class DefaultInstanceHandle : IInstanceHandle
	{
		public GameObject Instance { get; private set; }
		public void Release()
		{
			if (Instance)
			{
				Object.Destroy(Instance);
				Instance = null;
			}
		}

		public DefaultInstanceHandle( GameObject _object ) { Instance = _object; }
	}

	public sealed class DefaultAssetHandle<T> : IAssetHandle<T> where T : Object
	{
		public T Asset { get; }
		public AssetKey Key { get; }
		public bool IsLoaded => Asset != null;
		public void Release()
		{
			if (Asset)
				Object.Destroy(Asset);
		}

		public DefaultAssetHandle( T _asset, AssetKey _key )
		{
			Asset = _asset; 
			Key = _key;
		}
	}

	public sealed class DefaultAssetProvider : IAssetProvider
	{
		public async Task<IInstanceHandle> InstantiateAsync
		(
			object _key,
			Transform _parent = null,
			CancellationToken _cancellationToken = default 
		)
		{
			var assetKey = NormalizeKey<GameObject>(_key);
			GameObject prefab = (GameObject) Load(assetKey, _cancellationToken);

			// Keep method truly async-friendly
			await Task.Yield();

			var instance = Object.Instantiate(prefab, _parent);
			return new DefaultInstanceHandle(instance);
		}

		public async Task<IAssetHandle<T>> LoadAssetAsync<T>( object _key, CancellationToken _cancellationToken = default ) where T : Object
		{
			var assetKey = NormalizeKey<T>(_key);
			T obj = (T) Load(assetKey, _cancellationToken);
			
			// Keep method truly async-friendly
			await Task.Yield();
			
			return new DefaultAssetHandle<T>(obj, assetKey);
		}

		public void Release<T>( IAssetHandle<T> _handle ) where T : Object => _handle.Release();
		public void Release(IInstanceHandle _handle) => _handle.Release();
		
		public Task PreloadAsync( IEnumerable<object> _keysOrLabels, CancellationToken _cancellationToken = default ) => Task.CompletedTask;

		public void ReleaseUnused() { }

		public AssetKey NormalizeKey<T>( object _key ) where T : Object
		{
			if (_key is AssetKey assetKey)
			{
				if (!ReferenceEquals(assetKey.Provider, this))
					throw new InvalidOperationException($"Attempt to use Key designed for Asset provider '{assetKey.Provider.GetType().Name}' with '{GetType().Name}'");

				return assetKey;
			}

			// Prefab reference directly assigned
			if (_key is Object obj)
			{
#if UNITY_EDITOR
				if (!EditorUtility.IsPersistent(obj))
					throw new InvalidOperationException($"Invalid key object '{obj.name}': Only persistent objects can be used as keys");
#endif
				return new AssetKey(this, obj.GetInstanceID().ToString(), typeof(T));
			}

			// Resources.Load() style: use path as stable Id
			if (_key is string path)
				return new AssetKey(this, $"res:{path}", typeof(T));

#if UNITY_EDITOR
			// Editor only: GUID normalized as string
			if (_key is Guid guid)
				return new AssetKey(this, $"guid:{guid:N}", typeof(T));
#endif

			// Fallback: ToString of whatever came in
			return new AssetKey(this, $"unknown:{_key}", typeof(T));
		}
		
		public Object Load(AssetKey _assetKey, CancellationToken _cancellationToken)
		{
			Object result = null;

			if (_assetKey.TryGetValue("res:", out string resourcePath))
				result = Resources.Load(resourcePath, _assetKey.Type);

#if UNITY_EDITOR
			else if (_assetKey.TryGetValue("guid:", out string guid))
			{
				var path = AssetDatabase.GUIDToAssetPath(guid);
				if (!string.IsNullOrEmpty(path))
					result = AssetDatabase.LoadAssetAtPath(path, _assetKey.Type);
			}
#endif

			_cancellationToken.ThrowIfCancellationRequested();

			if (!result)
				throw new Exception($"Could not load asset: '{_assetKey.Id}'");
			
			return result;
		}
	}
}