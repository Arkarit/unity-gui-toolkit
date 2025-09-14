using UnityEngine;

namespace GuiToolkit.AssetHandling
{
	/// <summary>
	/// Base class for factories that create <see cref="IAssetProvider"/> instances.
	/// Typically implemented as ScriptableObject so providers can be registered
	/// via project assets and used in the <see cref="AssetManager"/>.
	/// Additional AssetProviderFactories need to be registered in UiToolkitConfiguration.m_assetProviderFactories
	/// </summary>
	public abstract class AbstractAssetProviderFactory : ScriptableObject
	{
		/// <summary>
		/// Create a new asset provider instance.
		/// </summary>
		/// <returns>A new <see cref="IAssetProvider"/>.</returns>
		public abstract IAssetProvider CreateProvider();
	}
}
