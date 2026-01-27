using GuiToolkit.AssetHandling;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using UnityEngine;

namespace GuiToolkit
{
	public static class UiPoolExtensions
	{
		public static Task<GameObject> PoolInstantiateAsync( this CanonicalAssetKey _key, CancellationToken _ct = default )
		{
			return UiMain.IsAwake ? UiPool.Instance.GetAsync(_key, _ct) : Task.FromResult<GameObject>(null);
		}

		public static Task<T> PoolInstantiateAsync<T>( this CanonicalAssetKey _key, CancellationToken _ct = default ) where T : Component
		{
			return UiMain.IsAwake ? UiPool.Instance.GetAsync<T>(_key, _ct) : Task.FromResult<T>(null);
		}
		
		public static GameObject PoolInstantiate( this GameObject _prefab, Transform _parent = null )
		{
			return UiMain.IsAwake ? UiPool.Instance.Get(_prefab, _parent) : null;
		}

		public static T PoolInstantiate<T>( this T _prefabComponent ) where T : Component
		{
			return UiMain.IsAwake ? UiPool.Instance.Get(_prefabComponent) : null;
		}

		public static T PoolInstantiate<T>( this T _prefabComponent, Transform _parent ) where T : Component
		{
			if (!UiMain.IsAwake)
				return null;

			T result = UiPool.Instance.Get(_prefabComponent, _parent);
			return result;
		}

		public static void PoolDestroy( this GameObject _gameObject )
		{
			if (!UiMain.IsAwake)
				return;

			UiPool.Instance.Release(_gameObject);
		}

		public static void PoolDestroy<T>( this T _prefabComponent ) where T : Component
		{
			if (!UiMain.IsAwake)
				return;

			UiPool.Instance.Release(_prefabComponent);
		}

		public static void PoolDestroyChildren<T>( this Transform _transform ) where T : Component
		{
			T[] children = _transform.GetComponentsInChildren<T>();
			foreach (var child in children)
				child.PoolDestroy();
		}

		public static void PoolDestroyChildren<T>( this GameObject _gameObject ) where T : Component
		{
			T[] children = _gameObject.GetComponentsInChildren<T>();
			foreach (var child in children)
				child.PoolDestroy();
		}

	}
}