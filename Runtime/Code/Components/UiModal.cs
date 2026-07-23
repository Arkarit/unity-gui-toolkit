using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit
{
	/// <summary>
	/// A MonoBehaviour modal helper for overlays or full-screen dialogs that wires a referenced click-catcher Button
	/// so clicks outside the content invoke the OnClickCatcher callback.
	/// </summary>
	public class UiModal : MonoBehaviour
	{
		[SerializeField]
		private Button m_clickCatcher;

		public Action OnClickCatcher;

		protected void OnEnable()
		{
			if (m_clickCatcher != null)
				m_clickCatcher.onClick.AddListener(ClickCatcherClicked);
		}

		protected void OnDisable()
		{
			if (m_clickCatcher != null)
				m_clickCatcher.onClick.RemoveListener(ClickCatcherClicked);
		}

		private void ClickCatcherClicked()
		{
			if (OnClickCatcher != null)
				OnClickCatcher?.Invoke();
		}
	}
}