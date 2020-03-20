using System.Collections.Generic;
using UnityEngine;

namespace GuiToolkit
{
	public class UiPool : MonoBehaviour
	{
		private class PoolEntry
		{
			public readonly List<GameObject> m_instances = new List<GameObject>();
		}

		public Transform m_container;

		private readonly Dictionary<GameObject,PoolEntry> m_poolEntryByPrefab = new Dictionary<GameObject, PoolEntry>();
		private readonly Dictionary<GameObject,PoolEntry> m_poolEntryByGameObject = new Dictionary<GameObject, PoolEntry>();

		public static UiPool Instance { get; private set; }

		protected void Awake()
		{
			Instance = this;
		}

		protected void OnDestroy()
		{
			Instance = null;
		}

		public GameObject DoInstantiate( GameObject _prefab )
		{
			GameObject result;

			if (m_poolEntryByPrefab.ContainsKey(_prefab))
			{
				PoolEntry poolEntry = m_poolEntryByPrefab[_prefab];
				if (poolEntry.m_instances.Count > 0)
				{
					int lastIdx = poolEntry.m_instances.Count-1;
					result = poolEntry.m_instances[lastIdx];
					poolEntry.m_instances.RemoveAt(lastIdx);
					result.transform.parent = null;
					result.SetActive(true);
					m_poolEntryByGameObject.Add(result, poolEntry);
					return result;
				}

				result = Instantiate(_prefab);
				m_poolEntryByGameObject.Add(result, poolEntry);
				return result;
			}

			result = Instantiate(_prefab);
			PoolEntry newPoolEntry = new PoolEntry();
			m_poolEntryByPrefab.Add(_prefab, newPoolEntry);
			m_poolEntryByGameObject.Add(result, newPoolEntry);

			return result;
		}

		public T DoInstantiate<T>( T _componentOnPrefabRoot ) where T : Component
		{
			return DoInstantiate(_componentOnPrefabRoot.gameObject).GetComponent<T>();
		}

		public void DoDestroy( GameObject _gameObject )
		{
			if (! m_poolEntryByGameObject.ContainsKey(_gameObject))
			{
				_gameObject.Destroy();
				return;
			}

			PoolEntry poolEntry = m_poolEntryByGameObject[_gameObject];
			m_poolEntryByGameObject.Remove(_gameObject);
			_gameObject.transform.parent = m_container;
			_gameObject.SetActive(false);
			poolEntry.m_instances.Add(_gameObject);
		}

		public void DoDestroy<T>( T _component ) where T : Component
		{
			if (_component is UiView)
			{
				UiView view = _component as UiView;
				view.OnPooled();
			}
			DoDestroy(_component.gameObject);
		}


	}

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

	}
}