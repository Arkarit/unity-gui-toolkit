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
						// We need to allow switch off while creating the entries; 
						// otherwise Unity will automatically select the first entry, which confuses
						// all radio toggles (because they get their on/off state from player prefs)
						toggleGroup.allowSwitchOff = true;
					}

					foreach (var entry in group.Value)
					{
						InstantiateMatchingEntry(entry, textContainer.RectTransform, toggleGroup);
					}

					// Only after all toggle groups elements have been added, we need to disallow switch off
					if (toggleGroup != null)
						toggleGroup.allowSwitchOff = false;
				}
			}
		}

		private UiPlayerSettingBase InstantiateMatchingEntry( PlayerSetting _playerSetting, RectTransform _parent, ToggleGroup _toggleGroup )
		{
			if (_playerSetting.IsLanguage)
				return InstantiateMatchingEntry(_playerSetting, m_languagePrefab, _parent, _toggleGroup, "PlayerSettingLanguage_", _playerSetting.Key);

			if (_playerSetting.IsFloat)
				return InstantiateMatchingEntry(_playerSetting, m_sliderPrefab, _parent, _toggleGroup, "PlayerSettingSlider_", _playerSetting.Key);

			if (_playerSetting.IsRadio)
				return InstantiateMatchingEntry(_playerSetting, m_radioPrefab, _parent, _toggleGroup, "PlayerSettingRadio_", _playerSetting.Key);

			if (_playerSetting.IsBool)
				return InstantiateMatchingEntry(_playerSetting, m_togglePrefab, _parent, _toggleGroup, "PlayerSettingCheck_", _playerSetting.Key);

			if (_playerSetting.IsKeyCode)
				return InstantiateMatchingEntry(_playerSetting, m_keyBindingsPrefab, _parent, _toggleGroup, "PlayerSettingKeyBinding_", _playerSetting.Key);

			Debug.LogError("Unknown player setting type");
			return null;
		}

		private UiPlayerSettingBase InstantiateMatchingEntry(PlayerSetting _playerSetting, UiPlayerSettingBase _prefab, RectTransform _parent, ToggleGroup _toggleGroup, string _gameObjectNamePrefix, string _title )
		{
			UiPlayerSettingBase result = Instantiate(_prefab, _parent);
			result.SetData(_gameObjectNamePrefix, _playerSetting.Key, _playerSetting );
			if (_toggleGroup && result.Toggle != null)
				result.Toggle.group = _toggleGroup;
			return result;
		}

		private void ClearDialogEntries()
		{
			m_tabContentContainer.DestroyAllChildren();
			m_pageContentContainer.DestroyAllChildren();
			m_tabInfos.Clear();
		}

	}
}