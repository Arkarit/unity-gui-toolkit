using GuiToolkit.AssetHandling;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using UnityEngine;
using UnityEngine.Serialization;

namespace GuiToolkit
{
	/// <summary>
	/// Provides runtime pooling for UI prefabs.
	/// Reuses inactive instances to reduce GC and instantiation overhead.
	/// </summary>
	public class UiPool : MonoBehaviour
	{
		/// <summary>
		/// The parent transform used to store inactive pooled objects.
		/// </summary>
		[FormerlySerializedAs("m_container")]
		public Transform m_poolContainer;

		private readonly Dictionary<GameObject, UiPoolPrefabInstances> m_instancesByPrefab = new();
		private readonly Dictionary<GameObject, UiPoolPrefabInstances> m_instancesByInstance = new();
		private readonly Dictionary<CanonicalAssetKey, UiPoolCanonicalInstances> m_canonicalByKey = new();
		private readonly Dictionary<GameObject, UiPoolCanonicalInstances> m_canonicalByInstance = new();

		/// <summary>
		/// Global singleton instance of the UI pool.
		/// Returns null if the main UI system is not initialized.
		/// </summary>
		public static UiPool Instance => UiMain.IsAwake ? UiMain.Instance.UiPool : null;

		protected void OnDestroy() => Clear();

		[Obsolete("Use Get() instead")]
		public GameObject DoInstantiate( GameObject _prefab ) => Get(_prefab);

		/// <summary>
		/// Retrieves an instance of the given prefab from the pool.
		/// If no pooled instance is available, a new one is instantiated.
		/// </summary>
		public GameObject Get( GameObject _prefab )
		{
			if (!m_instancesByPrefab.ContainsKey(_prefab))
				m_instancesByPrefab.Add(_prefab, new UiPoolPrefabInstances(_prefab, m_poolContainer, m_instancesByInstance));

			var instances = m_instancesByPrefab[_prefab];
			var result = instances.Get();
			return result;
		}

		[Obsolete("Use Get() instead")]
		public T DoInstantiate<T>( T _componentOnPrefabRoot ) where T : Component => Get<T>(_componentOnPrefabRoot);

		/// <summary>
		/// Retrieves a pooled instance based on a prefab component.
		/// Triggers OnPoolCreated() if the component implements IPoolable.
		/// </summary>
		public T Get<T>( T _componentOnPrefabRoot ) where T : Component
		{
			T result = Get(_componentOnPrefabRoot.gameObject).GetComponent<T>();
			if (result is IPoolable poolable)
				poolable.OnPoolCreated();

			return result;
		}

		/// <summary>
		/// Retrieves an instance via CanonicalAssetKey asynchronously.
		/// </summary>
		public Task<GameObject> GetAsync( CanonicalAssetKey _key, CancellationToken _ct = default )
		{
			return GetOrCreateCanonicalGroup(_key).GetAsync(_ct);
		}

		/// <summary>
		/// Retrieves a component via CanonicalAssetKey asynchronously.
		/// Triggers OnPoolCreated() if the component implements IPoolable.
		/// </summary>
		public async Task<T> GetAsync<T>( CanonicalAssetKey _key, CancellationToken _ct = default ) where T : Component
		{
			GameObject go = await GetAsync(_key, _ct);
			T result = go.GetComponent<T>();

			if (result is IPoolable poolable)
				poolable.OnPoolCreated();

			return result;
		}


		public bool HasPrefab<T>( T _componentOnPrefabRoot ) where T : Component => HasPrefab(_componentOnPrefabRoot.gameObject);

		public bool HasPrefab( GameObject _prefab )
		{
			if (!m_instancesByPrefab.ContainsKey(_prefab))
				return false;

			return m_instancesByPrefab[_prefab].HasInstances;
		}

		[Obsolete("Use Release() instead")]
		public void DoDestroy( GameObject _instance ) => Release(_instance);

		/// <summary>
		/// Releases a pooled GameObject back to the pool, or destroys it if unknown.
		/// </summary>
		public void Release( GameObject _instance )
		{
			if (_instance == null)
			{
				Debug.LogWarning("[UiPool] Releasing null object");
				return;
			}

#if UNITY_EDITOR
			if (!m_instancesByInstance.ContainsKey(_instance) && !m_canonicalByInstance.ContainsKey(_instance))
				Debug.LogWarning($"[UiPool] Releasing object '{_instance.GetPath()}', which was not created by the pool.");
#endif

			if (m_instancesByInstance.TryGetValue(_instance, out UiPoolPrefabInstances prefabGroup))
			{
				prefabGroup.Release(_instance);
				return;
			}

			if (m_canonicalByInstance.TryGetValue(_instance, out UiPoolCanonicalInstances canonicalGroup))
			{
				canonicalGroup.Release(_instance);
				return;
			}

			_instance.SafeDestroy();
		}

		[Obsolete("Use Release() instead")]
		public void DoDestroy<T>( T _component ) where T : Component => Release<T>(_component);

		/// <summary>
		/// Releases a pooled component's GameObject. 
		/// Calls OnPoolReleased() if the component implements IPoolable.
		/// </summary>
		public void Release<T>( T _component ) where T : Component
		{
			if (_component == null)
			{
				Debug.LogWarning("[UiPool] Releasing null component");
				return;
			}

			if (_component is IPoolable poolable)
				poolable.OnPoolReleased();

			Release(_component.gameObject);
		}

		/// <summary>
		/// Clears the entire pool, removing all tracked instances.
		/// Note: Leased objects are NOT destroyed, as forcibly destroying objects that are still in use
		/// may lead to confusing or unintended behavior.
		/// Calling Release() after Clear() is perfectly safe; such objects will simply be destroyed.
		/// </summary>
		public void Clear()
		{
			foreach (var kv in m_instancesByPrefab)
				kv.Value.Clear();

			foreach (var kv in m_canonicalByKey)
				kv.Value.Clear();

			m_instancesByPrefab.Clear();
			m_instancesByInstance.Clear();

			m_canonicalByKey.Clear();
			m_canonicalByInstance.Clear();
		}

		private UiPoolCanonicalInstances GetOrCreateCanonicalGroup( CanonicalAssetKey _key )
		{
			if (!m_canonicalByKey.TryGetValue(_key, out UiPoolCanonicalInstances group))
			{
				Transform container = m_poolContainer != null ? m_poolContainer : transform;
				group = new UiPoolCanonicalInstances(_key, container, m_canonicalByInstance);
				m_canonicalByKey.Add(_key, group);
			}
			return group;
		}

	}
}
