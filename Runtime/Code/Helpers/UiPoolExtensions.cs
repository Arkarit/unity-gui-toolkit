using System.Collections.Generic;
using UnityEngine;

namespace GuiToolkit
{
	public static class UiPoolExtensions
	{
		public static GameObject PoolInstantiate( this GameObject _prefab )
		{
			return UiMain.IsAwake ? UiPool.Instance.DoInstantiate(_prefab) : null;
		}

		public static T PoolInstantiate<T>( this T _prefabComponent ) where T : Component
		{
			return UiMain.IsAwake ? UiPool.Instance.DoInstantiate(_prefabComponent) : null;
		}

		public static T PoolInstantiate<T>( this T _prefabComponent, Transform _parent ) where T : Component
		{
			if (!UiMain.IsAwake)
				return null;
			
			T result = UiPool.Instance.DoInstantiate(_prefabComponent);
			result.transform.SetParent(_parent, false);
			return result;
		}

		public static void PoolDestroy( this GameObject _gameObject )
		{
			if (!UiMain.IsAwake)
				return;
			
			UiPool.Instance.DoDestroy(_gameObject);
		}

		public static void PoolDestroy<T>( this T _prefabComponent ) where T : Component
		{
			if (!UiMain.IsAwake)
				return;
			
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