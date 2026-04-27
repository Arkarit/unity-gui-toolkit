using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace GuiToolkit
{
	/// <summary>
	/// Populates a <see cref="UiDropdown"/> with all available languages and keeps it in sync with
	/// the active language.
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
		private readonly List<string> m_languageDisplayNames = new List<string>();

		protected override bool NeedsLanguageChangeCallback => true;

		protected override void OnLanguageChanged( string _languageId )
		{
			SyncSelectionToCurrentLanguage();
		}

		protected override void OnEnable()
		{
			base.OnEnable();
			PopulateLanguageData();
		}

		private void PopulateLanguageData()
		{
			m_languageIds.Clear();
			m_languageDisplayNames.Clear();

			string[] available = LocaManager.Instance?.GetAvailableLanguages();
			if (available == null || available.Length == 0)
			{
				Debug.LogWarning("[UiLanguageSelectDropdown] No available languages found. " +
					"Run 'Gui Toolkit > Localization > Process Loca' to generate the language list.");
				return;
			}

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
				m_languageDisplayNames.Add(LocaLanguageNames.GetNativeName(normalized));
			}

			SyncSelectionToCurrentLanguage();
		}

		protected override void PopulatePopup( UiPopup.Options options )
		{
			options.StringItems = m_languageDisplayNames.ToArray();
		}

		protected override void OnDropdownValueChanged( int _index )
		{
			base.OnDropdownValueChanged(_index);
			if (_index >= 0 && _index < m_languageIds.Count)
				LocaManager.Instance.ChangeLanguage(m_languageIds[_index]);
		}

		protected override void UpdateLabel()
		{
			if (m_selectedLabel == null)
				return;

			if (m_selectedIndex >= 0 && m_selectedIndex < m_languageDisplayNames.Count)
				m_selectedLabel.text = m_languageDisplayNames[m_selectedIndex];
		}

		private void SyncSelectionToCurrentLanguage()
		{
			string language = LocaManager.Instance?.Language;
			if (string.IsNullOrEmpty(language))
				return;

			string current = LocaManager.NormalizeLanguageId(language);
			int index = m_languageIds.IndexOf(current);
			if (index >= 0)
			{
				m_selectedIndex = index;
				UpdateLabel();
			}
		}
	}
}

