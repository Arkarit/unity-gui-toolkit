using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace GuiToolkit
{
	/// <summary>
	/// Populates a <see cref="TMP_Dropdown"/> with all available languages and keeps it in sync with
	/// the active language. Add this component to a GameObject that also has a <see cref="TMP_Dropdown"/>.
	///
	/// Available languages are discovered from the pre-generated <c>uitk_available_languages.txt</c>
	/// resource file (written by the editor's Loca processing pass). Native (endonym) display names
	/// are sourced from <see cref="LocaLanguageNames"/>.
	///
	/// In release builds "dev" is never shown. In the Unity Editor and Development Builds it is
	/// included so developers can verify un-translated keys.
	/// </summary>
	public class UiLanguageSelectDropdown : UiDropdown
	{
		private readonly List<string> m_languageIds = new List<string>();

		protected override bool NeedsLanguageChangeCallback => true;

		protected override void OnLanguageChanged( string _languageId )
		{
			SyncSelectionToCurrentLanguage();
		}

		protected override void PopulateDropdown()
		{
			m_languageIds.Clear();
			m_dropdown.ClearOptions();

			string[] available = LocaManager.Instance?.GetAvailableLanguages();
			if (available == null || available.Length == 0)
			{
				Debug.LogWarning("[UiLanguageSelectDropdown] No available languages found. " +
					"Run 'Gui Toolkit > Localization > Process Loca' to generate the language list.");
				return;
			}

			var options = new List<TMP_Dropdown.OptionData>();

			var config = UiToolkitConfiguration.Instance;
			bool useWhitelist = config != null && config.LanguageWhitelistEnabled;
			HashSet<string> whitelist = useWhitelist ? new HashSet<string>(config.LanguageWhitelist) : null;

			foreach (string langId in available)
			{
				string normalized = LocaManager.NormalizeLanguageId(langId);

#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
				if (normalized == "dev")
					continue;
#endif
				if (useWhitelist && normalized != "dev" && !whitelist.Contains(normalized))
					continue;

				m_languageIds.Add(normalized);
				options.Add(new TMP_Dropdown.OptionData(LocaLanguageNames.GetNativeName(normalized)));
			}

			m_dropdown.AddOptions(options);
			SyncSelectionToCurrentLanguage();
		}

		protected override void OnDropdownValueChanged( int _index )
		{
			base.OnDropdownValueChanged(_index);
			if (_index >= 0 && _index < m_languageIds.Count)
				LocaManager.Instance.ChangeLanguage(m_languageIds[_index]);
		}

		private void SyncSelectionToCurrentLanguage()
		{
			string language = LocaManager.Instance?.Language;
			if (string.IsNullOrEmpty(language))
				return;

			string current = LocaManager.NormalizeLanguageId(language);
			int index = m_languageIds.IndexOf(current);
			if (index >= 0)
				m_dropdown.SetValueWithoutNotify(index);
		}
	}
}

