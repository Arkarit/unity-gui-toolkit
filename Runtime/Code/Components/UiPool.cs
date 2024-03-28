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

		public static UiPool Instance => UiMain.Instance.UiPool;

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
					result.transform.SetParent(null, false);
					result.SetActive(true);
					if (!m_poolEntryByGameObject.ContainsKey(result))
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
			if (!m_poolEntryByGameObject.ContainsKey(_gameObject))
			{
				_gameObject.Destroy();
				return;
			}

			PoolEntry poolEntry = m_poolEntryByGameObject[_gameObject];
			_gameObject.transform.SetParent( m_container, false );
			_gameObject.SetActive(false);

			// We must check if the game object is already in the pool - it is not
			// forbidden to pool an object which is already in the pool (e.g. induced by events, which are also fired to inactive components)
			if (!poolEntry.m_instances.Contains(_gameObject))
				poolEntry.m_instances.Add(_gameObject);
		}

		public void DoDestroy<T>( T _component ) where T : Component
		{
			if (_component is UiPanel)
			{
				UiPanel view = _component as UiPanel;
				view.OnPooled();
			}
			DoDestroy(_component.gameObject);
		}


	}
}