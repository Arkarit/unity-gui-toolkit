using UnityEngine;

namespace GuiToolkit.AssetHandling
{
	public interface IInstanceHandle
	{
		GameObject Instance { get; }
		void Release(); // Addressables: ReleaseInstance; Direct: Destroy
	}
}