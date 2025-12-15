using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace GuiToolkit
{
	[Serializable]
	public struct TabInfo
	{
		public UiTab Tab;
		public UiPanel Page;
	}

	/// <summary>
	/// A dialog with tabs.
	/// Note: Regarding animation, currently only vertical tabs is supported.
	/// </summary>
	public class UiTabDialog : UiView
	{
		[Tooltip("Close button. Optional.")]
		[SerializeField] protected UiButton m_closeButton;

		[Tooltip("All Tabs are placed in this container. The container should contain a matching layout group to organize the tabs.")]
		[SerializeField] protected RectTransform m_tabContentContainer;

		[Tooltip("All Pages are placed in this container. Pages should be set to match this container (e.g. stretching)")]
		[SerializeField] protected RectTransform m_pageContentContainer;

		[Tooltip("Tab Infos. Note that if you enter prefabs here and don't instantiate them yourself by code, you need to check 'Instantiate Tab Infos On Start'")]
		[SerializeField] protected List<TabInfo> m_tabInfos;

		[Tooltip("Check this if you are using prefabs in your 'Tab Infos' and don't instantiate them yourself by code.")]
		[SerializeField] protected bool m_instantiateTabInfosOnStart = false;

		[Tooltip("Tabs parent object. Used to switch tab display on and off (a tab dialog with only one page doesn't make sense)")]
		[FormerlySerializedAs("m_tabsColumn")]
		[SerializeField] protected GameObject m_tabsParent;

		[Tooltip("If this is checked, the 'Tabs Parent' object is hidden, if there is only one tab")]
		[SerializeField] protected bool m_autoHideTabIfOnlyOne = true;

		public readonly CEvent<int, int> EvOnWillChangeTabs = new ();
		private int m_currentTabIdx;

		public int CurrentTabIdx => m_currentTabIdx;

		public int NumPages
		{
			get
			{
				if (m_tabInfos == null)
					return 0;
				return m_tabInfos.Count;
			}
		}

		public UiTab CurrentTab => m_tabInfos[m_currentTabIdx].Tab;
		public UiPanel CurrentPage => m_tabInfos[m_currentTabIdx].Page;

		public UiTab GetTab(int _idx)
		{
			if (_idx < 0 || _idx >= NumPages)
				return null;

			return m_tabInfos[_idx].Tab;
		}

		public UiPanel GetPage(int _idx)
		{
			if (_idx < 0 || _idx >= NumPages)
				return null;

			return m_tabInfos[_idx].Page;
		}

		protected virtual void OnWillChangeTabs(int _currentTab, int _nextTab) {}

		public override void Show( bool _instant = false, Action _onFinish = null )
		{
			InitPages();
			base.Show(_instant, _onFinish);
			if (m_tabInfos.Count > 0)
				GotoPage(0, true, true);
		}

		public void GotoPage(int _idx, bool _instant = true, bool _force = false)
		{
			if (_idx >= m_tabInfos.Count)
			{
				UiLog.LogError("Page out of bounds");
				return;
			}

			if (!_force && _idx == m_currentTabIdx)
				return;

			OnWillChangeTabs(m_currentTabIdx, _idx);
			EvOnWillChangeTabs.Invoke(m_currentTabIdx, _idx);
			m_tabInfos[m_currentTabIdx].Page.Hide(_instant);
			m_tabInfos[_idx].Page.Show(_instant);
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
			ToggleGroup tabToggleGroup = m_tabContentContainer.GetOrCreateComponent<ToggleGroup>();
			tabToggleGroup.allowSwitchOff = false;

			if (m_instantiateTabInfosOnStart)
			{
				for (int i=0; i<m_tabInfos.Count; i++)
				{
					TabInfo tabInfo = m_tabInfos[i];
					tabInfo.Tab = Instantiate(tabInfo.Tab, m_tabContentContainer);
					tabInfo.Tab.Toggle.group = tabToggleGroup;
					tabInfo.Tab.IsOn = i == 0;
					tabInfo.Page = Instantiate(tabInfo.Page, m_pageContentContainer);
					m_tabInfos[i] = tabInfo; // I hate C#
				}
			}

			m_currentTabIdx = -1;
			for (int i=0; i<m_tabInfos.Count; i++)
			{
				TabInfo tabInfo = m_tabInfos[i];

				int toggleIndex = i;

				tabInfo.Tab.OnValueChanged.AddListener( (isOn) => OnToggleChanged(toggleIndex, isOn) );
				tabInfo.Tab.Toggle.group = tabToggleGroup;

				tabInfo.Page.SetVisible( tabInfo.Tab.Toggle.isOn, true );
				if (tabInfo.Tab.Toggle.isOn)
				{
					Debug.Assert(m_currentTabIdx == -1, "Multiple active tabs in tab dialog! Please use ToggleGroup and set only one to is on!");
					m_currentTabIdx = i;
				}
			}

			m_tabsParent.gameObject.SetActive(!m_autoHideTabIfOnlyOne || m_tabInfos.Count > 1);

			Debug.Assert(m_currentTabIdx != -1, "No active tabs in tab dialog! Please use ToggleGroup and set only one to is on!");
		}

		private void OnToggleChanged(int _idx, bool _isOn)
		{
			if (!_isOn || _idx == m_currentTabIdx)
				return;

			bool upOrLeft = _idx < m_currentTabIdx;

			OnWillChangeTabs(m_currentTabIdx, _idx);
			EvOnWillChangeTabs.Invoke(m_currentTabIdx, _idx);

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