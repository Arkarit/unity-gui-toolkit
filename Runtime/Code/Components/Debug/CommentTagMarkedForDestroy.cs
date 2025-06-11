using UnityEngine;

namespace GuiToolkit.Debugging
{
	/// <summary>
	/// Tag alternative for marking a game object as scheduled for destruction
	/// Using regular Unity tags for that is not appropriate, since we don't want to force the user to introduce a tag - we're a library after all.
	/// </summary>
	public class CommentTagMarkedForDestroy : MonoBehaviour
	{
	}
}
