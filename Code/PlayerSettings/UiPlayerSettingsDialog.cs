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
		}

		private void Build()
		{
			ClearDialogEntries();

			PlayerSettings playerSettings = UiMain.Instance.PlayerSettings;
			var categorized = playerSettings.GetCategorized();

			foreach (var category in categorized)
			{
				UiToggle tab = Instantiate(m_tabPrefab, m_tabContentContainer);
				tab.Text = category.Key;
				// TODO translate

				UiPanel page = Instantiate(m_tabPagePrefab, m_pageContentContainer);
				RectTransform pageContainer = page.RectTransform;
				UiContentContainer contentContainer = pageContainer.GetComponent<UiContentContainer>();
				if (contentContainer != null)
					pageContainer = contentContainer.ContentContainer;

				m_tabInfos.Add(new TabInfo { Tab = tab, Page = page });

				foreach (var group in category.Value)
				{
					UiTextContainer textContainer = Instantiate(m_groupPrefab, pageContainer);
					textContainer.Text = group.Key;
					//TODO translate

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
				languageToggle.Title = _playerSetting.Key;
				languageToggle.Language = languageToken;
				languageToggle.Toggle.group = _toggleGroup;
				if (_playerSetting.HasIcon)
					languageToggle.Icon = _playerSetting.Icon;
				return;
			}

			if (_playerSetting.IsFloat)
			{
				UiPlayerSettingSlider slider = Instantiate(m_sliderPrefab, _transform);
				slider.Title = _playerSetting.Key;
				slider.Value = _playerSetting.GetValue<float>();
				if (_playerSetting.HasIcon)
					slider.Icon = _playerSetting.Icon;
				return;
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