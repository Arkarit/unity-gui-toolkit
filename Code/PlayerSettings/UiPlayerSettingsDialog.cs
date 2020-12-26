using System;
using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit
{
	public class UiPlayerSettingsDialog : UiTabDialog
	{
		[SerializeField] protected UiToggle m_tabPrefab;
		[SerializeField] protected UiPanel m_tabPagePrefab;
		[SerializeField] protected UiTextContainer m_groupPrefab;
		[SerializeField] protected UiPlayerSettingKeyBinding m_keyBindingsPrefab;
		[SerializeField] protected UiPlayerSettingLanguageToggle m_languagePrefab;
		[SerializeField] protected UiPlayerSettingSlider m_sliderPrefab;
		[SerializeField] protected UiPlayerSettingToggle m_togglePrefab;
		[SerializeField] protected UiPlayerSettingToggle m_radioPrefab;

		public override bool AutoDestroyOnHide => true;

		public override void Show( bool _instant = false, Action _onFinish = null )
		{
			Build();
			base.Show(_instant, _onFinish);
			if (m_tabInfos.Count > 0)
				GotoPage(0);
		}

		private void Build()
		{
			ClearDialogEntries();

			PlayerSettings playerSettings = UiMain.Instance.PlayerSettings;
			var categorized = playerSettings.GetCategorized();
			ToggleGroup tabToggleGroup = m_tabContentContainer.GetOrCreateComponent<ToggleGroup>();
			tabToggleGroup.allowSwitchOff = false;

			foreach (var category in categorized)
			{
				UiToggle tab = Instantiate(m_tabPrefab, m_tabContentContainer);
				tab.Text = category.Key;
				tab.Toggle.group = tabToggleGroup;
				tab.gameObject.name = "Tab_" + category.Key;
				// TODO translate

				UiPanel page = Instantiate(m_tabPagePrefab, m_pageContentContainer);
				RectTransform pageContainer = page.RectTransform;
				UiContentContainer contentContainer = pageContainer.GetComponent<UiContentContainer>();
				if (contentContainer != null)
					pageContainer = contentContainer.ContentContainer;
				page.gameObject.name = "Page_" + category.Key;

				m_tabInfos.Add(new TabInfo { Tab = tab, Page = page });

				foreach (var group in category.Value)
				{
					UiTextContainer textContainer = Instantiate(m_groupPrefab, pageContainer);
					textContainer.Text = group.Key;
					textContainer.gameObject.name = "TextContainer_" + group.Key;

					ToggleGroup toggleGroup = null;
					if (group.Value.Count > 0 && group.Value[0].IsRadio)
					{
						toggleGroup = textContainer.GetOrCreateComponent<ToggleGroup>();
						toggleGroup.allowSwitchOff = false;
					}

					foreach (var entry in group.Value)
					{
						InstantiateMatchingEntry(entry, textContainer.RectTransform, toggleGroup);
					}
				}
			}
		}

		private void InstantiateMatchingEntry( PlayerSetting _playerSetting, RectTransform _transform, ToggleGroup _toggleGroup )
		{
			if (_playerSetting.IsLanguage)
			{
				UiPlayerSettingLanguageToggle languageToggle = Instantiate(m_languagePrefab, _transform);
				string languageToken = _playerSetting.GetValue<string>();
				languageToggle.SetData("PlayerSettingLanguage_", _playerSetting.Key, _playerSetting);
				languageToggle.Language = languageToken;
				languageToggle.Toggle.group = _toggleGroup;
				if (_playerSetting.HasIcon)
					languageToggle.Icon = _playerSetting.Icon;
				return;
			}

			if (_playerSetting.IsFloat)
			{
				UiPlayerSettingSlider slider = Instantiate(m_sliderPrefab, _transform);
				slider.SetData("PlayerSettingSlider_", _playerSetting.Key, _playerSetting);
				slider.Value = _playerSetting.GetValue<float>();
				if (_playerSetting.HasIcon)
					slider.Icon = _playerSetting.Icon;
				return;
			}

			if (_playerSetting.IsRadio)
			{
				UiPlayerSettingToggle toggle = Instantiate(m_radioPrefab, _transform);
				toggle.SetData("PlayerSettingRadio_", _playerSetting.Key, _playerSetting);
				toggle.Value = _playerSetting.GetValue<bool>();
				toggle.Toggle.group = _toggleGroup;
				return;
			}

			if (_playerSetting.IsBool)
			{
				UiPlayerSettingToggle toggle = Instantiate(m_togglePrefab, _transform);
				toggle.SetData("PlayerSettingCheck_", _playerSetting.Key, _playerSetting);
				toggle.Value = _playerSetting.GetValue<bool>();
				return;
			}

			if (_playerSetting.IsKeyCode)
			{
				UiPlayerSettingKeyBinding keyBinding = Instantiate(m_keyBindingsPrefab, _transform);
				keyBinding.SetData("PlayerSettingKeyBinding_", _playerSetting.Key, _playerSetting);
				keyBinding.Value = _playerSetting.GetValue<KeyCode>();
			}
		}

		private void ClearDialogEntries()
		{
			m_tabContentContainer.DestroyAllChildren();
			m_pageContentContainer.DestroyAllChildren();
			m_tabInfos.Clear();
		}

	}
}