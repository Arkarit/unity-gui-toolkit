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

#if UNITY_EDITOR
			if (Application.isPlaying)
				Object.Destroy(Instance);
			else
				Object.DestroyImmediate(Instance);
#else
			Object.Destroy(Instance);
#endif
			Instance = null;
		}
	}

	public sealed class DefaultAssetHandle<T> : IAssetHandle<T> where T : Object
	{
		public T Asset { get; }
		public CanonicalAssetKey Key { get; }
		public bool IsLoaded => Asset != null;

		public void Release() { }

		public DefaultAssetHandle( T _asset, CanonicalAssetKey _key )
		{
			Asset = _asset;
			Key = _key;
		}
	}

	public sealed class DefaultAssetProvider : IAssetProvider
	{
		public static IAssetProviderEditorBridge s_editorBridge;

		public string Name => "Default Asset Provider";
		public string ResName => "Resource";

		public IAssetProviderEditorBridge EditorBridge => s_editorBridge;

		public async Task<IInstanceHandle> InstantiateAsync
		(
			object _key,
			Transform _parent = null,
			CancellationToken _cancellationToken = default
		)
		{
			var assetKey = NormalizeKey<GameObject>(_key);
			var prefab = (GameObject)Load(assetKey, _cancellationToken);

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
			var obj = (T)Load(assetKey, _cancellationToken);

			// Actually the asset is loaded sync - waiting for finish leads to freezes in editor D&D
			//await Task.Yield();

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

		public void ReleaseUnused() { }

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
			
#if UNITY_EDITOR
			if (_key is Object obj)
				if ( EditorBridge != null && EditorBridge.TryMakeId(obj, out string id))
					return new CanonicalAssetKey(this, id, obj.GetType());
#else
			if (_key is Object obj)
				throw new InvalidOperationException("Object keys are not supported. Use a 'res:' path instead.");
#endif

			if (_key is string pathStr)
				return new CanonicalAssetKey(this, pathStr.StartsWith("res:", StringComparison.Ordinal) ? pathStr : $"res:{pathStr}", typeof(T));

			return new CanonicalAssetKey(this, $"unknown:{_key}", typeof(T));
		}

		public bool Supports( CanonicalAssetKey _key ) => _key.Provider == this;

		public bool Supports( string _id )
		{
			if ( string.IsNullOrEmpty(_id) )
				return false;
			
			if (_id.Contains(':'))
				return _id.StartsWith("res:");
			
			return true;
		}

		public bool Supports( object _obj )
		{
			if (_obj is CanonicalAssetKey key)
				return Supports(key);

			if (_obj is string id)
				return Supports(id);

#if UNITY_EDITOR
			if (_obj is Object obj)
				return EditorBridge != null
					&& EditorBridge.TryMakeId(obj, out _);
#endif

			return false;
		}

		public Object Load( CanonicalAssetKey _assetKey, CancellationToken _cancellationToken )
		{
			Object result = null;

			if (_assetKey.TryGetValue("res:", out string resourcePath))
			{
				result = Resources.Load(resourcePath, _assetKey.Type);
			}

			_cancellationToken.ThrowIfCancellationRequested();

			if (!result)
			{
				throw new Exception
				(
					$"DefaultAssetProvider.Load failed: id='{_assetKey.Id}', type='{_assetKey.Type?.Name ?? "null"}', provider='{GetType().Name}'."
				);
			}

			return result;
		}
	}
}
