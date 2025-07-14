using System.Collections.Generic;
using UnityEngine;

namespace GuiToolkit
{
	internal class UiPoolPrefabInstances
	{
		private GameObject m_prefab;
		private Transform m_poolContainer;
		
		private readonly Stack<GameObject> m_instances = new ();
		
		public UiPoolPrefabInstances(GameObject _prefab, Transform _poolContainer)
		{
			m_prefab = _prefab;
			m_poolContainer = _poolContainer;
		}

		~UiPoolPrefabInstances() => Clear();

		public bool HasInstances => m_instances.Count > 0;
		
		public int NumInstances => m_instances.Count;
		
		public GameObject Get()
		{
			if (m_instances.TryPop(out GameObject instance))
			{
				instance.transform.SetParent(null, false);
				instance.SetActive(true);
				return instance;
			}
			
			return Object.Instantiate(m_prefab);
		}
		
		public void Release(GameObject _instance)
		{
			_instance.SetActive(false);
			_instance.transform.SetParent(m_poolContainer, false);
			m_instances.Push(_instance);
		}
		
		public void Clear()
		{
			foreach (var instance in m_instances)
				instance.SafeDestroy();
			m_instances.Clear();			
		}
	}
}