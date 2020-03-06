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
			m_clickCatcher?.onClick.AddListener(ClickCatcherClicked);
		}

		protected override void OnDisable()
		{
			base.OnDisable();
			m_clickCatcher?.onClick.RemoveListener(ClickCatcherClicked);
		}

		private void ClickCatcherClicked()
		{
			OnClickCatcher?.Invoke();
		}
	}
}