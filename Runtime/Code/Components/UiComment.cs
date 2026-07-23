using UnityEngine;

namespace GuiToolkit
{
	/// <summary>
	/// A lightweight, editor-only annotation you can drop on any GameObject to leave a note —
	/// for humans and for AI screen authoring alike.
	///
	/// Two roles in the authoring workflow:
	/// <list type="bullet">
	/// <item>On a <b>palette prefab root</b>, the text becomes the prefab's "flavor" description in
	/// the screen-authoring catalog (e.g. "OkButton — green confirm button"), complementing the
	/// per-type description harvested from the component's <c>&lt;summary&gt;</c>.</item>
	/// <item>On <b>child objects</b>, it is a contextual note: not part of the catalog, but useful
	/// when a human or the AI inspects a prefab or a baked screen.</item>
	/// </list>
	///
	/// The text is compiled only in the editor, so it never ships in a player build.
	/// </summary>
	public class UiComment : MonoBehaviour
	{
#if UNITY_EDITOR
		[TextArea(3, 10)]
		[SerializeField] private string m_comment;

		public string Text
		{
			get => m_comment;
			set => m_comment = value;
		}
#endif
	}
}
