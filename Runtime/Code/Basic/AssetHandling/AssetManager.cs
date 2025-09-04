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
		[RuntimeInitializeOnLoadMethod]
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
			}, null);
		}
	}
}