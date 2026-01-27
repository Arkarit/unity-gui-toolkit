using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit
{
	public class UiPlayerSettingsDialog : UiTabDialog
	{
		[SerializeField] protected UiButton m_okButton;
		[SerializeField] protected UiButton m_cancelButton;
		[SerializeField] protected UiButton m_restoreDefaultsButton;
		[SerializeField] protected UiTab m_tabPrefab;
		[SerializeField] protected UiPanel m_tabPagePrefab;
		[SerializeField] protected UiTextContainer m_groupPrefab;
		[SerializeField] protected UiPlayerSettingKeyBinding m_keyBindingsPrefab;
		[SerializeField] protected UiPlayerSettingLanguageToggle m_languagePrefab;
		[SerializeField] protected UiPlayerSettingSlider m_sliderPrefab;
		[SerializeField] protected UiPlayerSettingToggle m_togglePrefab;
		[SerializeField] protected UiPlayerSettingRadio m_radioPrefab;
		[SerializeField] protected UiPlayerSettingButton m_buttonPrefab;

		protected PlayerSettings m_playerSettings;
		protected readonly Dictionary<string,List<UiPlayerSettingBase>> m_uiPlayerSettings = new Dictionary<string, List<UiPlayerSettingBase>>();

		public Dictionary<string,List<UiPlayerSettingBase>> Entries => m_uiPlayerSettings;
		public List<UiPlayerSettingBase> GetEntries(string _key)
		{
			List<UiPlayerSettingBase> result = null;
			m_uiPlayerSettings.TryGetValue(_key, out result);
			return result;
		}

		public override bool AutoDestroyOnHide => true;
		public override bool Poolable => true;
		public override bool ShowDestroyFieldsInInspector => false;


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
		}

		private void Build()
		{
			ClearDialogEntries();

			var categorized = m_playerSettings.GetCategorized();
			ToggleGroup tabToggleGroup = m_tabContentContainer.GetOrCreateComponent<ToggleGroup>();
			tabToggleGroup.allowSwitchOff = false;

			foreach (var category in categorized)
			{
				UiTab tab = UiPool.Instance.Get(m_tabPrefab, m_tabContentContainer);
				tab.Text = category.Key;
				tab.Toggle.group = tabToggleGroup;
				tab.gameObject.name = "Tab_" + category.Key;

				UiPanel page = UiPool.Instance.Get(m_tabPagePrefab, m_pageContentContainer);
				RectTransform pageContainer = page.RectTransform;
				UiContentContainer contentContainer = pageContainer.GetComponent<UiContentContainer>();
				if (contentContainer != null)
					pageContainer = contentContainer.ContentContainer;
				page.gameObject.name = "Page_" + category.Key;

				m_tabInfos.Add(new TabInfo { Tab = tab, Page = page });

				foreach (var group in category.Value)
				{
					UiTextContainer textContainer = UiPool.Instance.Get(m_groupPrefab, pageContainer);
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

			// After all player setting elements have been added, invoke a OnPlayerSettingChanged for each element
			// to have a defined state, except for buttons, which haven't got a state/value
			foreach (var kv in m_uiPlayerSettings)
			{
				foreach (UiPlayerSettingBase uiPlayerSetting in kv.Value)
					if (!uiPlayerSetting.PlayerSetting.IsButton)
						uiPlayerSetting.PlayerSetting.InvokeEvents();
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
				InstantiateMatchingEntry(_playerSetting, m_sliderPrefab, _parent, null, "PlayerSettingSlider_");
			else if (_playerSetting.IsBool)
				InstantiateMatchingEntry(_playerSetting, m_togglePrefab, _parent, null, "PlayerSettingCheck_");
			else if (_playerSetting.IsKeyBinding)
				InstantiateMatchingEntry(_playerSetting, m_keyBindingsPrefab, _parent, null, "PlayerSettingKeyBinding_");
			else if (_playerSetting.IsButton)
				InstantiateMatchingEntry(_playerSetting, m_buttonPrefab, _parent, null, "PlayerSettingButton_");
			else
			{
				var customEntry = InstantiateCustomPrefabIfNecessary(_playerSetting, _parent);
				if (customEntry != null)
				{
					InstantiateMatchingEntry(_playerSetting, customEntry, _parent, null, "PlayerSetting_Custom_", null, true);
					return;
				}

				UiLog.LogError("Unknown player setting type");
			}
		}

		private UiPlayerSettingBase InstantiateCustomPrefabIfNecessary(PlayerSetting _playerSetting, RectTransform _parent)
		{
			if (_playerSetting.Options == null || _playerSetting.Options.CustomPrefab == null)
				return null;
				
			GameObject customPrefabGo = _playerSetting.Options.CustomPrefab;
			var playerSettingBase = customPrefabGo.GetComponent<UiPlayerSettingBase>();
			if (playerSettingBase == null)
			{
				UiLog.LogError($"Custom Prefab defined, but doesn't contain a '{nameof(UiPlayerSettingBase)}' component");
				return null;
			}
			
			return UiPool.Instance.Get(playerSettingBase, _parent);
		}
		
		private void InstantiateMatchingEntry
		(
			PlayerSetting _playerSetting, 
			UiPlayerSettingBase _prefab, 
			RectTransform _parent, 
			ToggleGroup _toggleGroup, 
			string _gameObjectNamePrefix, 
			string _subKey = null, 
			bool _preInstantiated = false
		)
		{
			if (!_preInstantiated)
			{
				var customEntry = InstantiateCustomPrefabIfNecessary(_playerSetting, _parent);
				if (customEntry != null)
				{
					_prefab = customEntry;
					_preInstantiated = true;
				}
			}
			
			UiPlayerSettingBase entry = _preInstantiated ? _prefab : UiPool.Instance.Get(_prefab, _parent);
			entry.SetData(_gameObjectNamePrefix, _playerSetting, _subKey );
			if (_toggleGroup && entry.Toggle != null)
				entry.Toggle.group = _toggleGroup;

			List<UiPlayerSettingBase> list;
			if (m_uiPlayerSettings.TryGetValue(_playerSetting.Key, out list))
			{
				list.Add(entry);
				return;
			}

			list = new List<UiPlayerSettingBase> { entry };
			m_uiPlayerSettings.Add(_playerSetting.Key, list);
		}

		private void ClearDialogEntries()
		{
			m_tabContentContainer.DestroyAllChildren(false);
			m_pageContentContainer.DestroyAllChildren(false);
			m_tabInfos.Clear();
			m_uiPlayerSettings.Clear();
		}

		protected virtual void OnRestoreDefaultsButton()
		{
			m_playerSettings.RestoreDefaults();
		}

		protected virtual void OnOkButton()
		{
			if (PlayerSettings.Instance.HasUnboundKeys())
			{
				UiMain.Instance.YesNoRequester(_("Unbound Keys"), _("Some keys remain unbound. Really continue?"), false, SaveAndHide);
				return;
			}

			SaveAndHide();
		}

		protected virtual void SaveAndHide()
		{
			m_playerSettings.Save(
				() =>
				{
					UiLog.LogInternal($"Player settings saved");
				}, 
				ex =>
				{
					UiLog.LogError($"Could not save player settings:{ex}");
				}
			);

			Hide();
		}

		protected override void OnCloseButton()
		{
			m_playerSettings.TempRestoreValues();
			base.OnCloseButton();
		}

	}
}