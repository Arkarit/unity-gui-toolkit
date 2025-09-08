namespace GuiToolkit.AssetHandling
{
	public interface IAssetHandle
	{
		UnityEngine.Object Asset { get; }
		// Optional meta for diagnostics
		object Key { get; }
		bool IsLoaded { get; }
		T GetAsset<T>() where T : UnityEngine.Object;
	}
}