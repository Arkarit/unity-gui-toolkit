using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using GuiToolkit.Exceptions;
using UnityEngine;
using Object = UnityEngine.Object;

namespace GuiToolkit.AssetHandling
{
	[EditorAware]
	public static class AssetManager
	{
		private static IAssetProvider[] s_assetProviders = Array.Empty<IAssetProvider>();

		private static readonly Dictionary<CanonicalAssetKey, List<Action>> s_assetReleases =
			new Dictionary<CanonicalAssetKey, List<Action>>();

		private static readonly Dictionary<CanonicalAssetKey, List<Action>> s_instanceReleases =
			new Dictionary<CanonicalAssetKey, List<Action>>();

		private static List<Action> GetOrCreateList( Dictionary<CanonicalAssetKey, List<Action>> _map,
													 CanonicalAssetKey _key )
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
			AssetReadyGate.WhenReady(() =>
			{
				List<IAssetProvider> providers = new();
				var factories = UiToolkitConfiguration.Instance.AssetProviderFactories;
				foreach (var factory in factories)
					providers.Add(factory.CreateProvider());

				providers.Add(new DefaultAssetProvider());
				s_assetProviders = providers.ToArray();
			});
		}

		public static IAssetProvider[] AssetProviders
		{
			get
			{
				if (s_assetProviders == null || s_assetProviders.Length == 0)
					throw new NotInitializedException(typeof(AssetManager));

				return s_assetProviders;
			}
		}

		public static IAssetProvider GetAssetProvider( object _obj )
		{
			foreach (var assetProvider in AssetProviders)
			{
				if (assetProvider.Supports(_obj))
					return assetProvider;
			}

			return null;
		}

		public static bool TryGetAssetProvider( object _obj, out IAssetProvider _assetProvider )
		{
			_assetProvider = GetAssetProvider(_obj);
			return _assetProvider != null;
		}

		public static void LoadAssetAsync<T>
		(
			CanonicalAssetKey _key,
			Action<T> _onSuccess = null,
			Action<T> _onFail = null,
			CancellationToken _cancellationToken = default
		)
		where T : Object
		{
			_ = LoadAssetAsyncImpl(_key, _onSuccess, _onFail, _cancellationToken);
		}

		private static async Task LoadAssetAsyncImpl<T>
		(
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

		public static void InstantiateAsync<T>
		(
			CanonicalAssetKey _key,
			Action<T> _onSuccess = null,
			Action<T> _onFail = null,
			CancellationToken _cancellationToken = default
		)
		where T : Object
		{
			_ = InstantiateAsyncImpl(_key, _onSuccess, _onFail, _cancellationToken);
		}

		private static async Task InstantiateAsyncImpl<T>
		(
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

		// Load asset without instantiation (optional; can be no-op)
		public static Task<IAssetHandle<GameObject>> LoadAssetAsync( object _key, CancellationToken _cancellationToken = default )
		{
			return GetAssetProviderOrThrow(_key).LoadAssetAsync<GameObject>(_key, _cancellationToken);
		}

		// Instantiate and return a handle that knows how to free itself
		public static Task<IInstanceHandle> InstantiateAsync( object _key, Transform _parent = null, CancellationToken _cancellationToken = default )
		{
			return GetAssetProviderOrThrow(_key).InstantiateAsync(_key, _parent, _cancellationToken);
		}

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

		// Free loaded prefab handle (not the instance)
		public static void Release( IAssetHandle<GameObject> _handle ) => _handle.Release();

		// Housekeeping hook
		public static void ReleaseUnused()
		{
			foreach (var assetProvider in AssetProviders)
				assetProvider.ReleaseUnused();
		}

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
				if (Release(k)) count++;

			// Assets (falls Keys ohne Instanzen uebrig sind)
			lock (s_assetReleases)
			{
				keys = new List<CanonicalAssetKey>(s_assetReleases.Keys);
			}
			foreach (var k in keys)
				if (Release(k)) count++;

			return count;
		}

		public static IAssetProvider GetAssetProviderOrThrow( object _obj )
		{
			if (!TryGetAssetProvider(_obj, out IAssetProvider result))
				throw new InvalidOperationException($"Could not handle key '{_obj}'. No matching asset provider found.");
			return result;
		}

#if UNITY_INCLUDE_TESTS
		public static void OverrideProvidersForTests( params IAssetProvider[] _providers )
		{
			s_assetProviders = _providers ?? Array.Empty<IAssetProvider>();
		}
#endif

	}
}