using UnityEngine;

namespace GuiToolkit.Debugging
{
	/// <summary>
	/// A very simple comment class (editor only)
	/// Can be used to comment game objects, but also as a
	/// temporary alternative to tags for editor code.
	/// </summary>
	public class Comment : MonoBehaviour
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