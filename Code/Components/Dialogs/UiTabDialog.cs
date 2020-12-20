using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit
{
	[Serializable]
	public struct TabInfo
	{
		public UiToggle Tab;
		public UiPanel Page;
	}

	/// <summary>
	/// A dialog with tabs.
	/// Note: Regarding animation, currently only vertical tabs is supported.
	/// </summary>
	public class UiTabDialog : UiView
	{
		public UiButton m_closeButton;

		public RectTransform m_pageContentContainer;

		public List<TabInfo> m_tabInfos;

		private int m_currentTabIdx;

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

		protected virtual void OnCloseButton()
		{
			Hide();
		}

		private void InitPages()
		{
			m_currentTabIdx = -1;
			for (int i=0; i<m_tabInfos.Count; i++)
			{
				TabInfo tabInfo = m_tabInfos[i];

				tabInfo.Tab.OnValueChanged.RemoveAllListeners();

				int toggleIndex = i;
				tabInfo.Tab.OnValueChanged.AddListener( (isOn) => OnToggleChanged(toggleIndex, isOn) );
				tabInfo.Page.SetVisible(tabInfo.Tab.Toggle.isOn, true);
				if (tabInfo.Tab.Toggle.isOn)
				{
					Debug.Assert(m_currentTabIdx == -1, "Multiple active tabs in tab dialog! Please use ToggleGroup and set only one to is on!");
					m_currentTabIdx = i;
				}
			}

			Debug.Assert(m_currentTabIdx != -1, "No active tabs in tab dialog! Please use ToggleGroup and set only one to is on!");
		}

		private void OnToggleChanged(int _idx, bool _isOn)
		{
			if (!_isOn || _idx == m_currentTabIdx)
				return;

			bool up = _idx < m_currentTabIdx;

			UiPanel oldPanel = m_tabInfos[m_currentTabIdx].Page;
			UiPanel newPanel = m_tabInfos[_idx].Page;

			UiSimpleAnimation oldAnim = oldPanel.SimpleShowHideAnimation as UiSimpleAnimation;
			UiSimpleAnimation newAnim = newPanel.SimpleShowHideAnimation as UiSimpleAnimation;

			if (oldAnim != null)
				oldAnim.SetSlideY(m_pageContentContainer, true, up);

			if (newAnim != null)
				newAnim.SetSlideY(m_pageContentContainer, true, !up);

			oldPanel.Hide();
			newPanel.Show();

			m_currentTabIdx = _idx;
		}
	}
}