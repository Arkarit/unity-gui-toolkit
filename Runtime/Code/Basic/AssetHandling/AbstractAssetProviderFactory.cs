using UnityEngine;

namespace GuiToolkit.AssetHandling
{
	public abstract class AbstractAssetProviderFactory : ScriptableObject
	{
		public abstract IAssetProvider CreateProvider();
	}
}