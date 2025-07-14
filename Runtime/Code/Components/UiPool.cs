using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace GuiToolkit
{
	public class UiPool : MonoBehaviour
	{
		[FormerlySerializedAs("m_container")] 
		public Transform m_poolContainer;

		private readonly Dictionary<GameObject,UiPoolPrefabInstances> m_instancesByPrefab = new ();
		private readonly Dictionary<GameObject,UiPoolPrefabInstances> m_instancesByInstance = new ();

		public static UiPool Instance => UiMain.IsAwake ? UiMain.Instance.UiPool : null;

		[Obsolete("Use Get() instead")]
		public GameObject DoInstantiate( GameObject _prefab ) => Get(_prefab);
		
		public GameObject Get( GameObject _prefab )
		{
			if (!m_instancesByPrefab.ContainsKey(_prefab))
				m_instancesByPrefab.Add(_prefab, new UiPoolPrefabInstances(_prefab, m_poolContainer));

			var instances = m_instancesByPrefab[_prefab];
			var result = instances.Get();
			m_instancesByInstance.Add(result, instances);
			return result;
		}

		[Obsolete("Use Get() instead")]
		public T DoInstantiate<T>( T _componentOnPrefabRoot ) where T : Component => Get<T>(_componentOnPrefabRoot);

		public T Get<T>( T _componentOnPrefabRoot ) where T : Component
		{
			return Get(_componentOnPrefabRoot.gameObject).GetComponent<T>();
		}
		
		[Obsolete("Use Release() instead")]
		public void DoDestroy( GameObject _instance ) => Release(_instance);

		public void Release( GameObject _instance )
		{
			if (m_instancesByInstance.TryGetValue(_instance, out UiPoolPrefabInstances instances))
			{
				m_instancesByInstance.Remove(_instance);
				instances.Release(_instance);
				return;
			}
			
			_instance.SafeDestroy();
		}

		[Obsolete("Use Release() instead")]
		public void DoDestroy<T>( T _component ) where T : Component => Release<T>(_component);
		
		public void Release<T>( T _component ) where T : Component
		{
			if (_component is UiPanel)
			{
				UiPanel view = _component as UiPanel;
				//TODO as interface instead
				view.OnPooled();
			}
			
			DoDestroy(_component.gameObject);
		}
		
		public void Clear(bool _destroyAllInstances = true)
		{
			foreach (var kv in m_instancesByPrefab)
			{
				kv.Value.Clear();
			}
			
			if (!_destroyAllInstances)
				return;

			foreach (var kv in m_instancesByInstance)
				kv.Key.SafeDestroy();
			
			m_instancesByPrefab.Clear();
			m_instancesByInstance.Clear();
		}
	}
}