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
	/// In release builds, "dev" is never shown. In the Unity Editor and Development Builds it is
	/// included so developers can verify un-translated keys.
	/// </summary>
	[RequireComponent(typeof(TMP_Dropdown))]
	public class UiLanguageSelectDropdown : MonoBehaviour
	{
		private TMP_Dropdown m_dropdown;
		private readonly List<string> m_languageIds = new List<string>();

		private void Awake()
		{
			m_dropdown = GetComponent<TMP_Dropdown>();
		}

		private void OnEnable()
		{
			PopulateDropdown();
			UiEventDefinitions.EvLanguageChanged.AddListener(OnLanguageChanged);
		}

		private void OnDisable()
		{
			UiEventDefinitions.EvLanguageChanged.RemoveListener(OnLanguageChanged);
			m_dropdown.onValueChanged.RemoveListener(OnDropdownValueChanged);
		}

		private void PopulateDropdown()
		{
			m_languageIds.Clear();
			m_dropdown.onValueChanged.RemoveListener(OnDropdownValueChanged);
			m_dropdown.ClearOptions();

			string[] available = LocaManager.Instance.GetAvailableLanguages();
			var options = new List<TMP_Dropdown.OptionData>();

			var config = UiToolkitConfiguration.Instance;
			bool useWhitelist = config != null && config.LanguageWhitelistEnabled;
			HashSet<string> whitelist = useWhitelist ? new HashSet<string>(config.LanguageWhitelist) : null;

			foreach (string langId in available)
			{
#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
				if (langId == "dev")
					continue;
#endif
				// Apply whitelist filter; "dev" is always exempt
				if (useWhitelist && langId != "dev" && !whitelist.Contains(langId))
					continue;

				m_languageIds.Add(langId);
				options.Add(new TMP_Dropdown.OptionData(LocaLanguageNames.GetNativeName(langId)));
			}

			m_dropdown.AddOptions(options);
			SyncSelectionToCurrentLanguage();
			m_dropdown.onValueChanged.AddListener(OnDropdownValueChanged);
		}

		private void SyncSelectionToCurrentLanguage()
		{
			string current = LocaManager.Instance.Language;
			int index = m_languageIds.IndexOf(current);
			if (index >= 0)
				m_dropdown.SetValueWithoutNotify(index);
		}

		private void OnDropdownValueChanged( int _index )
		{
			if (_index >= 0 && _index < m_languageIds.Count)
				LocaManager.Instance.ChangeLanguage(m_languageIds[_index]);
		}

		private void OnLanguageChanged( string _languageId )
		{
			SyncSelectionToCurrentLanguage();
		}
	}
}
