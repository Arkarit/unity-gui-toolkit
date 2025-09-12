using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GuiToolkit.AssetHandling;
using UnityEngine;

namespace GuiToolkit
{
	/// <summary>
	/// Internal pool data structure for one CanonicalAssetKey.
	/// Reuses deactivated instances created via AssetManager (IInstanceHandle).
	/// </summary>
	internal sealed class UiPoolCanonicalInstances
	{
		private readonly CanonicalAssetKey m_key;
		private readonly Transform m_poolContainer;
		private readonly Dictionary<GameObject, UiPoolCanonicalInstances> m_instancesByInstance;

		private readonly Stack<GameObject> m_instances = new();
		private readonly Dictionary<GameObject, IInstanceHandle> m_handles = new();

		public UiPoolCanonicalInstances(
			CanonicalAssetKey _key,
			Transform _poolContainer,
			Dictionary<GameObject, UiPoolCanonicalInstances> _instancesByInstance )
		{
			m_key = _key;
			m_poolContainer = _poolContainer;
			m_instancesByInstance = _instancesByInstance;
		}

		public async Task<GameObject> GetAsync( CancellationToken _ct )
		{
			if (m_instances.TryPop(out GameObject instance))
			{
				instance.transform.SetParent(null, false);
				instance.SetActive(true);
				m_instancesByInstance.Add(instance, this);
				return instance;
			}

			IInstanceHandle handle = await AssetManager.InstantiateAsync(m_key, null, _ct);
			GameObject go = handle.Instance;

			m_handles[go] = handle;
			m_instancesByInstance.Add(go, this);
			return go;
		}

		public void Release( GameObject _instance )
		{
			if (_instance == null)
				return;

			_instance.SetActive(false);
			_instance.transform.SetParent(m_poolContainer, false);
			m_instancesByInstance.Remove(_instance);
			m_instances.Push(_instance);
		}

		/// <summary>
		/// Destroys all pooled instances and clears the internal stack via handle.Release().
		/// Leased (active) instances are not touched.
		/// </summary>
		public void Clear()
		{
			while (m_instances.Count > 0)
			{
				GameObject go = m_instances.Pop();

				if (m_handles.TryGetValue(go, out IInstanceHandle handle))
				{
					handle.Release();
				}
				else
				{
					// Fallback, should not happen.
					go.SafeDestroy();
				}

				m_handles.Remove(go);
			}
		}
	}
}
