using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System;
using UnityEngine;

using Object = UnityEngine.Object;

namespace GuiToolkit.AssetHandling
{
	public sealed class DefaultInstanceHandle : IInstanceHandle
	{
		public Object Instance { get; }
		public void Release()
		{
			if (Instance)
				Object.Destroy(Instance);
		}

		public T GetInstance<T>() where T : Object => (T)Instance;
		public DefaultInstanceHandle( Object _object ) { Instance = _object; }
	}

	public sealed class DefaultAssetHandle : IAssetHandle
	{
		public Object Asset { get; }
		public object Key { get; }
		public bool IsLoaded => Asset != null;
		public T GetAsset<T>() where T : Object => (T) Asset;

		public DefaultAssetHandle( GameObject _asset, object _key ) { Asset = _asset; Key = _key; }
	}
	
	public sealed class DefaultAssetProvider : IAssetProvider
	{
		public async Task<IInstanceHandle> InstantiateAsync( object _key, Transform _parent = null, CancellationToken _cancellationToken = default )
		{
			var key = (DefaultAssetHandle)NormalizeKey(_key);
			if (!key.IsLoaded)
				throw new Exception("<Prefab missing>");

			var go = Object.Instantiate(key.GetAsset<GameObject>(), _parent);
			return new DefaultInstanceHandle(go);
		}

		public Task<IAssetHandle> LoadPrefabAsync( object _key, CancellationToken _cancellationToken = default )
		{
			var key = NormalizeKey(_key);
			if (!key.IsLoaded)
				return null;
			
			return Task.FromResult<IAssetHandle>(new DefaultAssetHandle(key.GetAsset<GameObject>(), _key));
		}

		public void Release( IAssetHandle _handle ) { }
		public Task PreloadAsync( IEnumerable<object> _keysOrLabels, CancellationToken _cancellationToken = default ) => Task.CompletedTask;
		public void ReleaseUnused() { }

		public IAssetHandle NormalizeKey( object _key )
		{
			if (_key is GameObject go)
				return new DefaultAssetHandle(go, _key);

			if (_key is string path)
				return new DefaultAssetHandle(Resources.Load<GameObject>(path), _key);

#if UNITY_EDITOR
			// Optional: GUID support for Editor tooling
			if (_key is Guid guid)
				return new DefaultAssetHandle
				(
					(GameObject)UnityEditor.AssetDatabase.LoadAssetAtPath<Object>(UnityEditor.AssetDatabase.GUIDToAssetPath(guid.ToString("N"))), 
					_key
				);
#endif
			
			return new DefaultAssetHandle(null, _key);
		}

	}
}