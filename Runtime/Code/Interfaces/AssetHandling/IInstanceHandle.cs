using UnityEngine;

namespace GuiToolkit.AssetHandling
{
	public interface IInstanceHandle
	{
		Object Instance { get; }
		void Release(); // Addressables: ReleaseInstance; Direct: Destroy
		T GetInstance<T>() where T : Object;
	}
}