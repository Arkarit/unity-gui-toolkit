using UnityEngine;

namespace GuiToolkit.Test
{
	/// <summary>
	/// Minimal ScriptableObject used as a test asset for <see cref="TestCloneFolder"/>.
	/// Must be in its own file so Unity's MonoScript lookup resolves it reliably.
	/// </summary>
	public class CloneFolderTestAsset : ScriptableObject
	{
		public CloneFolderTestAsset InternalRef;
		public Object               ExternalRef;
	}
}
