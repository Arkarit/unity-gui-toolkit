using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit
{
	public class UiTabDialog : UiView
	{
		public UiButton m_closeButton;

		public List<UiToggle> m_tabs;
		public List<UiPanel> m_pages;

		public override void Show( bool _instant = false, Action _onFinish = null )
		{
			InitPages();
			base.Show(_instant, _onFinish);
		}

		protected override void OnEnable()
		{
			base.OnEnable();

			if (m_closeButton)
			{
				m_closeButton.gameObject.SetActive(true);
				m_closeButton.OnClick.AddListener(OnCloseButton);
			}
		}

		protected override void OnDisable()
		{
			base.OnDisable();

			if (m_closeButton)
				m_closeButton.OnClick.RemoveListener(OnCloseButton);
		}

		private void OnCloseButton()
		{
			Hide();
		}

		private void InitPages()
		{
			Debug.Assert(m_tabs.Count == m_pages.Count);
			for (int i=0; i<m_tabs.Count; i++)
			{
				UiToggle tab = m_tabs[i];
				UiPanel page = m_pages[i];

				tab.OnValueChanged.RemoveAllListeners();

				int toggleIndex = i;
				tab.OnValueChanged.AddListener( (isOn) => m_pages[toggleIndex].SetVisible(isOn) );
			}
		}
	}
}