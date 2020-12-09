using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit
{
	// A modal component
	// Can be used for an overlay or full screen dialog.
	public class UiModal : MonoBehaviour
	{
		public Button m_clickCatcher;
		public Action OnClickCatcher;

		protected void OnEnable()
		{
			if (m_clickCatcher != null && OnClickCatcher != null)
				m_clickCatcher.onClick.AddListener(ClickCatcherClicked);
		}

		protected void OnDisable()
		{
			if (m_clickCatcher != null && OnClickCatcher != null)
				m_clickCatcher.onClick.RemoveListener(ClickCatcherClicked);
		}

		private void ClickCatcherClicked()
		{
			if (OnClickCatcher != null)
				OnClickCatcher?.Invoke();
		}
	}
}