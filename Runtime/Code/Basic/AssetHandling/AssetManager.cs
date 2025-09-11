using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using GuiToolkit.Exceptions;
using UnityEngine;

namespace GuiToolkit.AssetHandling
{
	[EditorAware]
	public static class AssetManager
	{
		private static IAssetProvider[] s_assetProviders = Array.Empty<IAssetProvider>();
		
#if UNITY_EDITOR
		[UnityEditor.InitializeOnLoadMethod]
#endif
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void Init()
		{
			AssetReadyGate.WhenReady(() =>
			{
				List<IAssetProvider> providers = new ();
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
		
		public static IAssetProvider GetAssetProvider(object _obj)
		{
			foreach (var assetProvider in AssetProviders)
			{
				if (assetProvider.Supports(_obj))
					return assetProvider;
			}
			
			return null;
		}
		
		public static bool TryGetAssetProvider(object _obj,  out IAssetProvider _assetProvider)
		{
			_assetProvider = GetAssetProvider(_obj);
			return _assetProvider != null;
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

		// Free loaded prefab handle (not the instance)
		public static void Release( IAssetHandle<GameObject> _handle ) => _handle.Release();

		// Housekeeping hook
		public static void ReleaseUnused()
		{
			foreach (var assetProvider in AssetProviders)
				assetProvider.ReleaseUnused();
		}

		public static IAssetProvider GetAssetProviderOrThrow( object _obj )
		{
			if (!TryGetAssetProvider(_obj, out IAssetProvider result))
				throw new InvalidOperationException($"Could not handle key '{_obj}'. No matching asset provider found.");
			return result;
		}
	}
}