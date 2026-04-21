using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

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
	public class UiLanguageSelectDropdown : MonoBehaviour, IPointerClickHandler
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
				// Always normalize IDs so lookups work regardless of resource file age
				string normalized = LocaManager.NormalizeLanguageId(langId);

#if !UNITY_EDITOR && !DEVELOPMENT_BUILD
				if (normalized == "dev")
					continue;
#endif
				// Apply whitelist filter; "dev" is always exempt
				if (useWhitelist && normalized != "dev" && !whitelist.Contains(normalized))
					continue;

				m_languageIds.Add(normalized);
				options.Add(new TMP_Dropdown.OptionData(LocaLanguageNames.GetNativeName(normalized)));
			}

			m_dropdown.AddOptions(options);
			SyncSelectionToCurrentLanguage();
			m_dropdown.onValueChanged.AddListener(OnDropdownValueChanged);
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

		private void OnDropdownValueChanged( int _index )
		{
			if (_index >= 0 && _index < m_languageIds.Count)
				LocaManager.Instance.ChangeLanguage(m_languageIds[_index]);
		}

		private void OnLanguageChanged( string _languageId )
		{
			SyncSelectionToCurrentLanguage();
		}

		/// <summary>
		/// TMP_Dropdown is earlier in the component list, so its OnPointerClick (which calls Show())
		/// runs before ours. By the time we execute, the "Dropdown List" popup already exists and has
		/// its Canvas set to sortingOrder 30000 (hardcoded by TMP_Dropdown). We bump it to the maximum
		/// value so the popup always renders on top of any other Override-Sorting Canvas in the scene.
		/// </summary>
		public void OnPointerClick( PointerEventData _ )
		{
			EnsurePopupOnTop();
		}

		private void EnsurePopupOnTop()
		{
			Transform popup = m_dropdown.transform.Find("Dropdown List");
			if (popup == null)
				return;
			Canvas popupCanvas = popup.GetComponent<Canvas>();
			if (popupCanvas != null)
				popupCanvas.sortingOrder = short.MaxValue;
		}
	}
}
