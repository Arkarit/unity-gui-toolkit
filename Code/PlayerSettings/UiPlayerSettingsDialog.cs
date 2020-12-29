using System;
using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit
{
	public class UiPlayerSettingsDialog : UiTabDialog
	{
		[SerializeField] protected UiButton m_okButton;
		[SerializeField] protected UiButton m_cancelButton;
		[SerializeField] protected UiButton m_restoreDefaultsButton;
		[SerializeField] protected UiToggle m_tabPrefab;
		[SerializeField] protected UiPanel m_tabPagePrefab;
		[SerializeField] protected UiTextContainer m_groupPrefab;
		[SerializeField] protected UiPlayerSettingKeyBinding m_keyBindingsPrefab;
		[SerializeField] protected UiPlayerSettingLanguageToggle m_languagePrefab;
		[SerializeField] protected UiPlayerSettingSlider m_sliderPrefab;
		[SerializeField] protected UiPlayerSettingToggle m_togglePrefab;
		[SerializeField] protected UiPlayerSettingRadio m_radioPrefab;

		protected PlayerSettings m_playerSettings;

		public override bool AutoDestroyOnHide => true;

		protected override void OnEnable()
		{
			m_okButton.OnClick.AddListener(OnOkButton);
			m_cancelButton.OnClick.AddListener(OnCloseButton);
			m_restoreDefaultsButton.OnClick.AddListener(OnRestoreDefaultsButton);
			base.OnEnable();
		}

		protected override void OnDisable()
		{
			base.OnDisable();
			m_okButton.OnClick.RemoveListener(OnOkButton);
			m_cancelButton.OnClick.RemoveListener(OnCloseButton);
			m_restoreDefaultsButton.OnClick.RemoveListener(OnRestoreDefaultsButton);
		}

		public override void Show( bool _instant = false, Action _onFinish = null )
		{
			m_playerSettings = PlayerSettings.Instance;
			m_playerSettings.TempSaveValues();
			Build();
			base.Show(_instant, _onFinish);
			if (m_tabInfos.Count > 0)
				GotoPage(0);
		}

		private void Build()
		{
			ClearDialogEntries();

			var categorized = m_playerSettings.GetCategorized();
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

		private void InstantiateMatchingEntry( PlayerSetting _playerSetting, RectTransform _parent, ToggleGroup _toggleGroup )
		{
			if (_playerSetting.IsRadio)
			{
				foreach (string s in _playerSetting.Options.StringValues)
				{
					if (_playerSetting.IsLanguage)
						InstantiateMatchingEntry(_playerSetting, m_languagePrefab, _parent, _toggleGroup, "PlayerSettingLanguage_", s);
					else
						InstantiateMatchingEntry(_playerSetting, m_radioPrefab, _parent, _toggleGroup, "PlayerSettingRadio_", s);
				}
			}
			else if (_playerSetting.IsFloat)
				InstantiateMatchingEntry(_playerSetting, m_sliderPrefab, _parent, _toggleGroup, "PlayerSettingSlider_");
			else if (_playerSetting.IsBool)
				InstantiateMatchingEntry(_playerSetting, m_togglePrefab, _parent, _toggleGroup, "PlayerSettingCheck_");
			else if (_playerSetting.IsKeyCode)
				InstantiateMatchingEntry(_playerSetting, m_keyBindingsPrefab, _parent, _toggleGroup, "PlayerSettingKeyBinding_");
			else
				Debug.LogError("Unknown player setting type");
		}

		private void InstantiateMatchingEntry(PlayerSetting _playerSetting, UiPlayerSettingBase _prefab, RectTransform _parent, ToggleGroup _toggleGroup, string _gameObjectNamePrefix, string _subKey = null )
		{
			UiPlayerSettingBase result = Instantiate(_prefab, _parent);
			result.SetData(_gameObjectNamePrefix, _playerSetting, _subKey );
			if (_toggleGroup && result.Toggle != null)
				result.Toggle.group = _toggleGroup;
		}

		private void ClearDialogEntries()
		{
			m_tabContentContainer.DestroyAllChildren();
			m_pageContentContainer.DestroyAllChildren();
			m_tabInfos.Clear();
		}

		protected virtual void OnRestoreDefaultsButton()
		{
			m_playerSettings.RestoreDefaults();
		}

		protected virtual void OnOkButton()
		{
			Hide();
		}

		protected override void OnCloseButton()
		{
			m_playerSettings.TempRestoreValues();
			base.OnCloseButton();
		}

	}
}