using System.Collections.Generic;
using UnityEngine;

namespace GuiToolkit
{
	public static class UiPoolExtensions
	{
		public static GameObject PoolInstantiate( this GameObject _prefab )
		{
			if (UiPool.Instance == null)
				return Object.Instantiate(_prefab);

			return UiPool.Instance.DoInstantiate(_prefab);
		}

		public static T PoolInstantiate<T>( this T _prefabComponent ) where T : Component
		{
			if (UiPool.Instance == null)
				return Object.Instantiate(_prefabComponent.gameObject).GetComponent<T>();

			return UiPool.Instance.DoInstantiate(_prefabComponent);
		}

		public static void PoolDestroy( this GameObject _gameObject )
		{
			if (UiPool.Instance == null)
			{
				_gameObject.Destroy();
				return;
			}

			UiPool.Instance.DoDestroy(_gameObject);
		}

		public static void PoolDestroy<T>( this T _prefabComponent ) where T : Component
		{
			if (UiPool.Instance == null)
			{
				_prefabComponent.gameObject.Destroy();
				return;
			}

			UiPool.Instance.DoDestroy(_prefabComponent);
		}

		public static void PoolDestroyChildren<T>( this Transform _transform) where T : Component
		{
			T[] children = _transform.GetComponentsInChildren<T>();
			foreach (var child in children)
				child.PoolDestroy();
		}

		public static void PoolDestroyChildren<T>( this GameObject _gameObject) where T : Component
		{
			T[] children = _gameObject.GetComponentsInChildren<T>();
			foreach (var child in children)
				child.PoolDestroy();
		}

	}
}