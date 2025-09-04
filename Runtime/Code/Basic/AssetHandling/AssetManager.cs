using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using UnityEngine;

namespace GuiToolkit.AssetHandling
{
	[EditorAware]
	public static class AssetManager
	{
		private static IAssetProvider s_assetProvider = null;
#if UNITY_EDITOR
		[UnityEditor.InitializeOnLoadMethod]
#endif
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void Init()
		{
			AssetReadyGate.WhenReady(() =>
			{
				var factory = UiToolkitConfiguration.Instance.AssetProviderFactory;
				if (factory != null)
				{
					s_assetProvider = factory.CreateProvider();
					return;
				}
				
				s_assetProvider = new DefaultAssetProvider();
			});
		}
		
		public static IAssetProvider AssetProvider
		{
			get
			{
				if (s_assetProvider == null)
					throw new InvalidOperationException("Called asset provider before initialization. Please delay call.");
				return s_assetProvider;
			}
		}
		
		// Load prefab without instantiation (optional; can be no-op)
		public static Task<IAssetHandle<GameObject>> LoadPrefabAsync( object _key, CancellationToken _cancellationToken = default ) => AssetProvider.LoadPrefabAsync(_key, _cancellationToken);

		// Instantiate and return a handle that knows how to free itself
		public static Task<IInstanceHandle> InstantiateAsync( object _key, Transform _parent = null, CancellationToken _cancellationToken = default ) => AssetProvider.InstantiateAsync(_key, _parent, _cancellationToken);

		// Optional preload by label/set
		public static Task PreloadAsync( IEnumerable<object> _keysOrLabels, CancellationToken _cancellationToken = default ) => AssetProvider.PreloadAsync(_keysOrLabels, _cancellationToken);

		// Free loaded prefab handle (not the instance)
		public static void Release( IAssetHandle<GameObject> _handle ) => AssetProvider.Release( _handle );

		// Housekeeping hook
		public static void ReleaseUnused() => AssetProvider.ReleaseUnused();
		
		
	}
}