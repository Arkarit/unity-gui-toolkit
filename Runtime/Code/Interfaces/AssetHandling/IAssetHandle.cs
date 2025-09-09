using UnityEngine;

namespace GuiToolkit.AssetHandling
{
	public interface IAssetHandle<T> where T : Object
	{
		T Asset { get; }
		// Optional meta for diagnostics
		AssetKey Key { get; }
		bool IsLoaded { get; }
		void Release();
	}
}