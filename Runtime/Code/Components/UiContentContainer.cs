using UnityEngine;

namespace GuiToolkit
{
	/// <summary>
	/// A simple MonoBehaviour that exposes a designated child RectTransform as ContentContainer (and its own
	/// RectTransform as RectTransform), serving as the insertion point where other code parents dynamically created content.
	/// </summary>
	public class UiContentContainer : MonoBehaviour
	{
		[SerializeField] protected RectTransform m_contentContainer;

		public RectTransform ContentContainer => m_contentContainer;
		public RectTransform RectTransform => (RectTransform) transform;
	}
}