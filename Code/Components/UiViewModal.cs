using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit
{

	public class UiViewModal : UiView
	{
		public Button m_clickCatcher;

		protected Action OnClickCatcher;

		protected override void OnEnable()
		{
			base.OnEnable();
			if (m_clickCatcher != null)
				m_clickCatcher.onClick.AddListener(ClickCatcherClicked);
		}

		protected override void OnDisable()
		{
			base.OnDisable();
			if (m_clickCatcher != null)
				m_clickCatcher.onClick.RemoveListener(ClickCatcherClicked);
		}

		private void ClickCatcherClicked()
		{
			if (OnClickCatcher != null)
				OnClickCatcher.Invoke();
		}
	}
}