using System.Collections.Generic;
using UnityEngine;

namespace GuiToolkit
{
	/// <summary>
	/// Player-setting UI element for language selection via a dropdown.
	///
	/// An alternative to <see cref="UiPlayerSettingLanguageToggle"/>: instead of one flag-button per
	/// language it shows all languages in a single <see cref="UiDropdown"/> using native (endonym)
	/// display names sourced from <see cref="LocaLanguageNames"/>.
	///
	/// Configure the corresponding <see cref="PlayerSetting"/> with:
	/// <code>
	/// Type          = EPlayerSettingType.LanguageDropdown
	/// StringValues  = list of language IDs ("en", "de", …)
	/// </code>
	/// Optionally provide <c>Titles</c> to override the display names.
	/// </summary>
	public class UiPlayerSettingLanguageDropdown : UiPlayerSettingDropdown
	{
		protected override bool NeedsLanguageChangeCallback => true;

		public override void SetData( string _gameObjectNamePrefix, PlayerSetting _playerSetting, string _subKey )
		{
			base.SetData(_gameObjectNamePrefix, _playerSetting, _subKey);

			// Override PresetStringItems with native language display names unless Titles were supplied.
			if (m_dropdown == null || _playerSetting.Options.Titles != null)
				return;

			var langIds = _playerSetting.Options.StringValues;
			if (langIds == null)
				return;

			var displayNames = new string[langIds.Count];
			for (int i = 0; i < langIds.Count; i++)
				displayNames[i] = LocaLanguageNames.GetNativeName(LocaManager.NormalizeLanguageId(langIds[i]));

			m_dropdown.PresetStringItems = displayNames;
			SyncDropdownToValue();
		}

		protected override void OnDropdownValueChanged( int _index )
		{
			if (!Initialized)
				return;

			var stringValues = m_playerSetting.Options.StringValues;
			if (stringValues == null || _index < 0 || _index >= stringValues.Count)
				return;

			string langId = stringValues[_index];
			LocaManager.Instance.ChangeLanguage(langId);
			base.Value = langId;
		}

		protected override void OnLanguageChanged( string _languageId )
		{
			SyncDropdownToValue();
		}
	}
}
