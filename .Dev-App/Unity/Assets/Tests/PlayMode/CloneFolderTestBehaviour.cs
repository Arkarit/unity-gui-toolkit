using UnityEngine;

namespace GuiToolkit.Test
{
	/// <summary>
	/// Minimal MonoBehaviour used by editor tests to verify that MonoBehaviour
	/// references on prefabs are rewired correctly after a folder clone.
	/// Must live in a non-editor assembly so Unity can serialize it into prefabs.
	/// </summary>
	public class CloneFolderTestBehaviour : MonoBehaviour
	{
		public Object InternalRef;
		public Object ExternalRef;
	}
}
