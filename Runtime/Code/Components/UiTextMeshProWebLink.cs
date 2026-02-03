using System;
using System.Collections.Generic;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GuiToolkit
{
	[RequireComponent(typeof(TMP_Text))]
	public class UiTextMeshProWebLink : MonoBehaviour, IPointerClickHandler
	{
		private TMP_Text m_text;

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

		private void Awake()
		{
			m_text = GetComponent<TMP_Text>();
		}
	}
}
