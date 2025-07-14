using System.Collections.Generic;
using UnityEngine;

namespace GuiToolkit
{
	/// <summary>
	/// Internal pool data structure for one specific prefab.
	/// Stores and reuses deactivated instances.
	/// </summary>
	internal class UiPoolPrefabInstances
	{
		private GameObject m_prefab;
		private Transform m_poolContainer;
		private Dictionary<GameObject, UiPoolPrefabInstances> m_instancesByInstance;

		private readonly Stack<GameObject> m_instances = new();

		/// <summary>
		/// Initializes a pool entry for the given prefab.
		/// </summary>
		public UiPoolPrefabInstances(GameObject _prefab, Transform _poolContainer, Dictionary<GameObject, UiPoolPrefabInstances> _instancesByInstance)
		{
			m_prefab = _prefab;
			m_poolContainer = _poolContainer;
			m_instancesByInstance = _instancesByInstance;
		}

		/// <summary>
		/// Returns true if there are any available instances in the pool.
		/// </summary>
		public bool HasInstances => m_instances.Count > 0;

		/// <summary>
		/// Returns the number of currently available (inactive) instances.
		/// </summary>
		public int NumInstances => m_instances.Count;

		/// <summary>
		/// Retrieves an instance from the pool, or instantiates a new one if necessary.
		/// </summary>
		public GameObject Get()
		{
			if (m_instances.TryPop(out GameObject instance))
			{
				instance.transform.SetParent(null, false);
				instance.SetActive(true);
				m_instancesByInstance.Add(instance, this);
				return instance;
			}

			var result = Object.Instantiate(m_prefab);
			m_instancesByInstance.Add(result, this);
			return result;
		}

		/// <summary>
		/// Releases a GameObject instance back to the pool.
		/// </summary>
		public void Release(GameObject _instance)
		{
			_instance.SetActive(false);
			_instance.transform.SetParent(m_poolContainer, false);
			m_instancesByInstance.Remove(_instance);
			m_instances.Push(_instance);
		}

		/// <summary>
		/// Destroys all pooled instances and clears the internal stack.
		/// </summary>
		public void Clear()
		{
			foreach (var instance in m_instances)
				instance.SafeDestroy();

			m_instances.Clear();
		}
	}
}
