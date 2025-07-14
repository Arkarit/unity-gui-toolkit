using System.Collections.Generic;
using UnityEngine;

namespace GuiToolkit
{
	internal class UiPoolPrefabInstances
	{
		private GameObject m_prefab;
		private Transform m_poolContainer;
		private Dictionary<GameObject,UiPoolPrefabInstances> m_instancesByInstance;
		
		private readonly Stack<GameObject> m_instances = new ();
		
		public UiPoolPrefabInstances(GameObject _prefab, Transform _poolContainer, Dictionary<GameObject,UiPoolPrefabInstances> _instancesByInstance)
		{
			m_prefab = _prefab;
			m_poolContainer = _poolContainer;
			m_instancesByInstance = _instancesByInstance;
		}

		public bool HasInstances => m_instances.Count > 0;
		
		public int NumInstances => m_instances.Count;
		
		public GameObject Get()
		{
			if (m_instances.TryPop(out GameObject instance))
			{
				instance.transform.SetParent(null, false);
				instance.SetActive(true);
				m_instancesByInstance.Add(instance, this);
				return instance;
			}
			
			return Object.Instantiate(m_prefab);
		}
		
		public void Release(GameObject _instance)
		{
			_instance.SetActive(false);
			_instance.transform.SetParent(m_poolContainer, false);
			m_instancesByInstance.Remove(_instance);
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