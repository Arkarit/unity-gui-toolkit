using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GuiToolkit
{
	/// <summary>
	/// Sits next to a TMP_Text on the same GameObject (added via RequireComponent) and, on click, opens the URL of
	/// the clicked TextMeshPro link tag via Application.OpenURL.
	/// </summary>
	[RequireComponent(typeof(TMP_Text))]
	public class UiWebLinkTextMeshPro : MonoBehaviour, IPointerClickHandler
	{
		private TMP_Text m_text;
		
		private void Awake()
		{
			m_text = GetComponent<TMP_Text>();
		}

		public void OnPointerClick( PointerEventData _eventData )
		{
			int linkIndex = TMP_TextUtilities.FindIntersectingLink
			(
				m_text,
				_eventData.position,
				_eventData.pressEventCamera
			);

			if (linkIndex == -1)
				return;

			TMP_LinkInfo linkInfo = m_text.textInfo.linkInfo[linkIndex];
			string linkId = linkInfo.GetLinkID();

			Application.OpenURL(linkId);
		}

	}
}
