using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System;
using UnityEngine;

using Object = UnityEngine.Object;

namespace GuiToolkit.AssetHandling
{
	public sealed class DefaultAssetProvider : IAssetProvider
	{
		public async Task<IInstanceHandle> InstantiateAsync( object _key, Transform _parent = null, CancellationToken _cancellationToken = default )
		{
			GameObject prefab = Resolve(_key); // GameObject or Resources.Load<GameObject>((string)key)
			if (!prefab) 
				throw new Exception("<Prefab missing>");
			
			var go = Object.Instantiate(prefab, _parent);
			return new DefaultInstanceHandle(go);
		}

		public Task<IAssetHandle<GameObject>> LoadPrefabAsync( object _key, CancellationToken _cancellationToken = default )
		{
			var prefab = Resolve(_key);
			return Task.FromResult<IAssetHandle<GameObject>>(new DefaultAssetHandle(prefab, _key));
		}

		public void Release( IAssetHandle<GameObject> _handle ) {}
		public Task PreloadAsync( IEnumerable<object> _keysOrLabels, CancellationToken _cancellationToken = default ) => Task.CompletedTask;
		public void ReleaseUnused() {}

		private static GameObject Resolve( object _key )
		{
			if (_key is GameObject go) return go;
			if (_key is string path) return Resources.Load<GameObject>(path);
#if UNITY_EDITOR
			// Optional: GUID support for Editor tooling
			if (_key is Guid guid) return (GameObject)UnityEditor.AssetDatabase.LoadAssetAtPath<Object>(
				UnityEditor.AssetDatabase.GUIDToAssetPath(guid.ToString("N")));
#endif
			return null;
		}

		private sealed class DefaultInstanceHandle : IInstanceHandle
		{
			public GameObject Instance { get; }
			public DefaultInstanceHandle( GameObject _go ) { Instance = _go; }
			public void Release()
			{
				if (Instance) 
					Object.Destroy(Instance);
			}
		}

		private sealed class DefaultAssetHandle : IAssetHandle<GameObject>
		{
			public GameObject Asset { get; }
			public object Key { get; }
			public DefaultAssetHandle( GameObject _asset, object _key ) { Asset = _asset; Key = _key; }
		}
	}
}