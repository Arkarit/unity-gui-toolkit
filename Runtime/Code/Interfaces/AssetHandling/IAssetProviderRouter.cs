namespace GuiToolkit.AssetHandling
{
	public interface IAssetProviderRouter
	{
		// Return true and set provider if you can handle the given id (e.g., "addr:...", "res:...", "guid:...").
		bool TryGetProviderForId( string _id, out IAssetProvider _provider );
	}
}
