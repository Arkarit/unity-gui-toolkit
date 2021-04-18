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
		[SerializeField] protected UiButton m_closeButton;
		[SerializeField] protected GameObject m_tabsColumn;
		[SerializeField] protected RectTransform m_tabContentContainer;
		[SerializeField] protected RectTransform m_pageContentContainer;
		[SerializeField] protected List<TabInfo> m_tabInfos;
		[SerializeField] protected bool m_autoHideTabIfOnlyOne = true;

		private int m_currentTabIdx;

		public override void Show( bool _instant = false, Action _onFinish = null )
		{
			InitPages();
			base.Show(_instant, _onFinish);
		}

		public void GotoPage(int _idx)
		{
			if (_idx >= m_tabInfos.Count)
			{
				Debug.LogError("Page out of bounds");
				return;
			}

			if (_idx == m_currentTabIdx)
				return;

			m_tabInfos[m_currentTabIdx].Page.Hide(true);
			m_tabInfos[_idx].Page.Show(true);
			m_tabInfos[_idx].Tab.Toggle.isOn = true;
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

			m_tabsColumn.gameObject.SetActive(!m_autoHideTabIfOnlyOne || m_tabInfos.Count > 1);

			Debug.Assert(m_currentTabIdx != -1, "No active tabs in tab dialog! Please use ToggleGroup and set only one to is on!");
		}

		private void OnToggleChanged(int _idx, bool _isOn)
		{
			if (!_isOn || _idx == m_currentTabIdx)
				return;

			bool upOrLeft = _idx < m_currentTabIdx;

			UiPanel oldPanel = m_tabInfos[m_currentTabIdx].Page;
			UiPanel newPanel = m_tabInfos[_idx].Page;

			UiSimpleAnimation oldAnim = oldPanel.SimpleShowHideAnimation as UiSimpleAnimation;
			UiSimpleAnimation newAnim = newPanel.SimpleShowHideAnimation as UiSimpleAnimation;

			// We need to set the game object active to allow UiResolutionDependentSwitcher to copy its values 
			// prior to actually setting the values
			newPanel.gameObject.SetActive(true);

			bool isY = (oldAnim.Support & UiSimpleAnimation.ESupport.PositionY) != 0;

			if (isY)
			{
				if (oldAnim != null)
					oldAnim.SetSlideY(m_pageContentContainer, true, upOrLeft);

				if (newAnim != null)
					newAnim.SetSlideY(m_pageContentContainer, true, !upOrLeft);
			}
			else
			{
				if (oldAnim != null)
					oldAnim.SetSlideX(m_pageContentContainer, true, !upOrLeft);

				if (newAnim != null)
					newAnim.SetSlideX(m_pageContentContainer, true, upOrLeft);
			}

			oldPanel.Hide();
			newPanel.Show();

			m_currentTabIdx = _idx;
		}
	}
}