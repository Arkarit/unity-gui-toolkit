using UnityEngine;

namespace GuiToolkit
{
	// This also could be an interface. It however is much simpler and more efficient to search
	// for mono behaviours in a scene.
	public abstract class UiLocaClientBase : MonoBehaviour
	{
#if UNITY_EDITOR
		public abstract string Key {get;}
#endif
	}
}