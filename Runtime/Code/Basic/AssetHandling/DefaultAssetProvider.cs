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

		public DefaultInstanceHandle( GameObject _object )
		{
			Instance = _object;
		}

		public void Release()
		{
			if (!Instance)
				return;

			Instance.SafeDestroy(false);
			Instance = null;
		}
	}

	public sealed class DefaultAssetHandle<T> : IAssetHandle<T> where T : Object
	{
		public T Asset { get; }
		public AssetKey Key { get; }
		public bool IsLoaded => Asset != null;

		public void Release() { }

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
			var prefab = (GameObject) Load(assetKey, _cancellationToken);

			await Task.Yield();
			_cancellationToken.ThrowIfCancellationRequested();

			var instance = Object.Instantiate(prefab, _parent);
			return new DefaultInstanceHandle(instance);
		}

		public async Task<IAssetHandle<T>> LoadAssetAsync<T>
		(
			object _key,
			CancellationToken _cancellationToken = default
		) where T : Object
		{
			var assetKey = NormalizeKey<T>(_key);
			var obj = (T) Load(assetKey, _cancellationToken);

			await Task.Yield();

			return new DefaultAssetHandle<T>(obj, assetKey);
		}

		public void Release<T>( IAssetHandle<T> _handle ) where T : Object
		{
			_handle?.Release(); // no-op
		}

		public void Release( IInstanceHandle _handle )
		{
			_handle?.Release();
		}

		public Task PreloadAsync
		(
			IEnumerable<object> _keysOrLabels,
			CancellationToken _cancellationToken = default
		) => Task.CompletedTask;

		public void ReleaseUnused() { }

		public AssetKey NormalizeKey<T>( object _key ) where T : Object
		{
			if (_key is AssetKey assetKey)
			{
				if (!ReferenceEquals(assetKey.Provider, this))
					throw new InvalidOperationException
					(
						$"Attempt to use Key designed for Asset provider '{assetKey.Provider?.GetType().Name}' with '{GetType().Name}'"
					);

				return assetKey;
			}

			if (_key is Object obj)
			{
#if UNITY_EDITOR
				if (!EditorUtility.IsPersistent(obj))
					throw new InvalidOperationException($"Invalid key object '{obj.name}': Only persistent assets can be used as keys");

				string path = AssetDatabase.GetAssetPath(obj);
				if (!string.IsNullOrEmpty(path))
				{
					string guidStr = AssetDatabase.AssetPathToGUID(path);
					if (!string.IsNullOrEmpty(guidStr))
						return new AssetKey(this, $"guid:{guidStr}", typeof(T));
				}

				throw new InvalidOperationException($"Could not derive GUID for asset object '{obj.name}'.");
#else
				throw new InvalidOperationException("Object keys are not supported at runtime. Use a 'res:' path instead.");
#endif
			}

			if (_key is string pathStr)
				return new AssetKey(this, pathStr.StartsWith("res:", StringComparison.Ordinal) ? pathStr : $"res:{pathStr}", typeof(T));

#if UNITY_EDITOR
			if (_key is Guid guid)
				return new AssetKey(this, $"guid:{guid:N}", typeof(T));
#endif

			return new AssetKey(this, $"unknown:{_key}", typeof(T));
		}

		public Object Load( AssetKey _assetKey, CancellationToken _cancellationToken )
		{
			Object result = null;

			if (_assetKey.TryGetValue("res:", out string resourcePath))
			{
				result = Resources.Load(resourcePath, _assetKey.Type);
			}

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
				throw new Exception
				(
					$"DefaultAssetProvider.Load failed: id='{_assetKey.Id}', type='{_assetKey.Type?.Name ?? "null"}', provider='{GetType().Name}'."
				);

			return result;
		}
	}
}
