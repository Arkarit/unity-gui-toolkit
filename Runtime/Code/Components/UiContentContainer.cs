using UnityEngine;

namespace GuiToolkit
{
	public class UiContentContainer : MonoBehaviour
	{
		[SerializeField] protected RectTransform m_contentContainer;

		public RectTransform ContentContainer => m_contentContainer;
		public RectTransform RectTransform => (RectTransform) transform;
	}
}