using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GuiToolkit.Exceptions;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GuiToolkit.AssetHandling
{
	/// <summary>
	/// Central entry point for asset loading and instantiation.
	/// Delegates actual work to registered <see cref="IAssetProvider"/>s
	/// and manages release bookkeeping for loaded assets and instances.
	/// </summary>
	[EditorAware]
	public static class AssetManager
	{
		private static IAssetProvider[] s_assetProviders = Array.Empty<IAssetProvider>();

		private static readonly Dictionary<CanonicalAssetKey, List<Action>> s_assetReleases =
			new Dictionary<CanonicalAssetKey, List<Action>>();

		private static readonly Dictionary<CanonicalAssetKey, List<Action>> s_instanceReleases =
			new Dictionary<CanonicalAssetKey, List<Action>>();

		/// <summary>
		/// Gets the currently active asset providers.
		/// Throws <see cref="NotInitializedException"/> if not yet initialized.
		/// </summary>
		public static IAssetProvider[] AssetProviders
		{
			get
			{
				if (s_assetProviders == null || s_assetProviders.Length == 0)
					throw new NotInitializedException(typeof(AssetManager));

				return s_assetProviders;
			}
		}

		/// <summary>
		/// Finds the first provider that supports the given object key.
		/// Returns null if none matches.
		/// </summary>
		public static IAssetProvider GetAssetProvider( object _obj )
		{
			foreach (var assetProvider in AssetProviders)
			{
				if (assetProvider.Supports(_obj))
					return assetProvider;
			}

			return null;
		}

		/// <summary>
		/// Tries to find a provider that supports the given object key.
		/// </summary>
		public static bool TryGetAssetProvider( object _obj, out IAssetProvider _assetProvider )
		{
			_assetProvider = GetAssetProvider(_obj);
			return _assetProvider != null;
		}

		/// <summary>
		/// Asynchronously loads an asset without instantiating it.
		/// Fire-and-forget variant with callbacks.
		/// </summary>
		public static void LoadAssetAsync<T>(
			CanonicalAssetKey _key,
			Action<T> _onSuccess = null,
			Action<T> _onFail = null,
			CancellationToken _cancellationToken = default
		)
		where T : Object
		{
			_ = LoadAssetAsyncImpl(_key, _onSuccess, _onFail, _cancellationToken);
		}

		/// <summary>
		/// Asynchronously instantiates an asset.
		/// Fire-and-forget variant with callbacks.
		/// </summary>
		public static void InstantiateAsync<T>(
			CanonicalAssetKey _key,
			Action<T> _onSuccess = null,
			Action<T> _onFail = null,
			CancellationToken _cancellationToken = default
		)
		where T : Object
		{
			_ = InstantiateAsyncImpl(_key, _onSuccess, _onFail, _cancellationToken);
		}

		/// <summary>
		/// Direct task-based asset load without instantiation.
		/// Optional, may be no-op in some providers.
		/// </summary>
		public static Task<IAssetHandle<GameObject>> LoadAssetAsync(
			object _key,
			CancellationToken _cancellationToken = default
		)
		{
			return GetAssetProviderOrThrow(_key).LoadAssetAsync<GameObject>(_key, _cancellationToken);
		}

		/// <summary>
		/// Direct task-based instantiation with optional parent transform.
		/// Returns an instance handle that can be released.
		/// </summary>
		public static Task<IInstanceHandle> InstantiateAsync(
			object _key,
			Transform _parent = null,
			CancellationToken _cancellationToken = default
		)
		{
			return GetAssetProviderOrThrow(_key).InstantiateAsync(_key, _parent, _cancellationToken);
		}

		/// <summary>
		/// Release all assets and instances associated with the given key.
		/// Returns true if anything was released.
		/// </summary>
		public static bool Release( CanonicalAssetKey _key )
		{
			bool result = false;

			lock (s_instanceReleases)
			{
				if (s_instanceReleases.TryGetValue(_key, out List<Action> rels))
				{
					for (int i = rels.Count - 1; i >= 0; i--)
						try { rels[i]?.Invoke(); } catch { /* swallow */ }

					s_instanceReleases.Remove(_key);
					result = true;
				}
			}

			lock (s_assetReleases)
			{
				if (s_assetReleases.TryGetValue(_key, out List<Action> rels))
				{
					for (int i = rels.Count - 1; i >= 0; i--)
						try { rels[i]?.Invoke(); } catch { /* swallow */ }

					s_assetReleases.Remove(_key);
					result = true;
				}
			}

			return result;
		}

		/// <summary>
		/// Release a loaded prefab handle (not the instance).
		/// </summary>
		public static void Release( IAssetHandle<GameObject> _handle ) => _handle.Release();

		/// <summary>
		/// Housekeeping hook. Lets providers clean up unused assets.
		/// </summary>
		public static void ReleaseUnused()
		{
			foreach (var assetProvider in AssetProviders)
				assetProvider.ReleaseUnused();
		}

		/// <summary>
		/// Returns true if any assets or instances are tracked for the given key.
		/// </summary>
		public static bool HasAny( CanonicalAssetKey _key )
		{
			lock (s_instanceReleases)
			{
				if (s_instanceReleases.ContainsKey(_key))
					return true;
			}
			lock (s_assetReleases)
			{
				if (s_assetReleases.ContainsKey(_key))
					return true;
			}
			return false;
		}

		/// <summary>
		/// Releases all tracked assets and instances.
		/// Returns the number of keys that were released.
		/// </summary>
		public static int ReleaseAll()
		{
			int count = 0;

			// Instances
			List<CanonicalAssetKey> keys;
			lock (s_instanceReleases)
			{
				keys = new List<CanonicalAssetKey>(s_instanceReleases.Keys);
			}

			foreach (var k in keys)
				if (Release(k)) 
					count++;

			// Assets (remaining keys without instances)
			lock (s_assetReleases)
			{
				keys = new List<CanonicalAssetKey>(s_assetReleases.Keys);
			}
			foreach (var k in keys)
				if (Release(k)) 
					count++;

			return count;
		}

		/// <summary>
		/// Finds a provider for the given key or throws if none is found.
		/// </summary>
		public static IAssetProvider GetAssetProviderOrThrow( object _obj )
		{
			if (!TryGetAssetProvider(_obj, out IAssetProvider result))
				throw new InvalidOperationException($"Could not handle key '{_obj}'. No matching asset provider found.");
			return result;
		}

#if UNITY_INCLUDE_TESTS
		/// <summary>
		/// Override active providers for testing purposes.
		/// </summary>
		public static void OverrideProvidersForTests( params IAssetProvider[] _providers )
		{
			s_assetProviders = _providers ?? Array.Empty<IAssetProvider>();
		}
#endif

		// =====================================================================
		// Private helpers
		// =====================================================================

		private static async Task LoadAssetAsyncImpl<T>(
			CanonicalAssetKey _key,
			Action<T> _onSuccess,
			Action<T> _onFail,
			CancellationToken _cancellationToken
		)
		where T : Object
		{
			try
			{
				IAssetProvider provider = GetAssetProviderOrThrow(_key);
				IAssetHandle<T> handle = await provider.LoadAssetAsync<T>(_key, _cancellationToken);

				lock (s_assetReleases)
				{
					GetOrCreateList(s_assetReleases, _key).Add(handle.Release);
				}

				_onSuccess?.Invoke(handle.Asset);
			}
			catch
			{
				_onFail?.Invoke(null);
			}
		}

		private static async Task InstantiateAsyncImpl<T>(
			CanonicalAssetKey _key,
			Action<T> _onSuccess,
			Action<T> _onFail,
			CancellationToken _cancellationToken
		)
		where T : Object
		{
			try
			{
				IAssetProvider provider = GetAssetProviderOrThrow(_key);
				IInstanceHandle instHandle = await provider.InstantiateAsync(_key, null, _cancellationToken);

				lock (s_instanceReleases)
				{
					GetOrCreateList(s_instanceReleases, _key).Add(instHandle.Release);
				}

				GameObject go = instHandle.Instance;

				T result = null;
				if (typeof(T) == typeof(GameObject))
					result = go as T;
				else if (typeof(Component).IsAssignableFrom(typeof(T)))
					result = go != null ? go.GetComponent<T>() : null;
				else
					result = go as T;

				if (!result)
				{
					_onFail?.Invoke(null);
					return;
				}

				_onSuccess?.Invoke(result);
			}
			catch
			{
				_onFail?.Invoke(null);
			}
		}

		private static List<Action> GetOrCreateList(
			Dictionary<CanonicalAssetKey, List<Action>> _map,
			CanonicalAssetKey _key
		)
		{
			if (!_map.TryGetValue(_key, out List<Action> list))
			{
				list = new List<Action>();
				_map[_key] = list;
			}
			return list;
		}

#if UNITY_EDITOR
		[UnityEditor.InitializeOnLoadMethod]
#endif
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void Init()
		{
			AssetReadyGate.WhenReady
			(
				() =>
				{
					List<IAssetProvider> providers = new();
					var factories = UiToolkitConfiguration.Instance.AssetProviderFactories;
					foreach (var factory in factories)
					{
						var provider = factory.CreateProvider();
						provider.Init();
						providers.Add(provider);
					}

					var defaultProvider = new DefaultAssetProvider();
					defaultProvider.Init();
					providers.Add(defaultProvider);
					s_assetProviders = providers.ToArray();
				}
			);
		}
	}
}
