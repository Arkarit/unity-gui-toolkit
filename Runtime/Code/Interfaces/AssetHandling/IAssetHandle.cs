namespace GuiToolkit.AssetHandling
{
	public interface IAssetHandle<T>
	{
		T Asset { get; }
		// Optional meta for diagnostics
		object Key { get; }
	}
}