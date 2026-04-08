using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;

namespace GuiToolkit
{
	/// <summary>
	/// TextMeshProUGUI subclass with automatic localization support.
	/// When <see cref="AutoLocalize"/> is enabled, setting the <see cref="LocaKey"/> property
	/// immediately applies the translated text. Re-translates on language changes.
	/// Replaces the deprecated <see cref="UiAutoLocalize"/> component.
	/// </summary>
	[AddComponentMenu("UI/Localized Text Mesh Pro UGUI")]
	public class UiLocalizedTextMeshProUGUI : TextMeshProUGUI, ILocaKeyProvider
	{
		[SerializeField] private bool m_autoLocalize = true;
		[SerializeField] private string m_group = string.Empty;
		[SerializeField] private string m_locaKey = string.Empty;

		// TMP's text setter does not fire an event we can filter; it is a plain property.
		// Overriding it (see below) creates a direct re-entry path: ApplyTranslation() calls
		// base.text = …, which would call our override again and loop forever.
		// This flag breaks the cycle so only the outermost call reaches base.text.
		private bool m_isSettingInternally;

		/// <summary>
		/// Gets or sets whether automatic localization is enabled.
		/// When enabled, changing <see cref="LocaKey"/> immediately applies the translation.
		/// When disabled, text must be set manually via the <see cref="text"/> property.
		/// </summary>
		public bool AutoLocalize
		{
			get => m_autoLocalize;
			set
			{
				m_autoLocalize = value;
				if (m_autoLocalize && !string.IsNullOrEmpty(m_locaKey))
					ApplyTranslation();
			}
		}

		/// <summary>
		/// Gets or sets the localization group namespace.
		/// Changing this triggers immediate re-translation if <see cref="AutoLocalize"/> is enabled.
		/// </summary>
		public string Group
		{
			get => m_group;
			set { m_group = value; ApplyTranslation(); }
		}

#if UNITY_EDITOR
		bool ILocaKeyProvider.UsesLocaKey => m_autoLocalize && !IsObviouslyRuntimeValue(LocaKey);
		bool ILocaKeyProvider.UsesMultipleLocaKeys => false;
		List<string> ILocaKeyProvider.LocaKeys => null;
		// LocaKey and Group are already public properties; they satisfy the interface implicitly.

		/// <summary>
		/// Heuristic: returns true when the candidate key is clearly a runtime-generated value
		/// (numeric placeholder, icon character, format string) rather than a translatable string.
		/// Used to suppress false positives during loca key extraction.
		/// </summary>
		private static bool IsObviouslyRuntimeValue(string key)
		{
			if (string.IsNullOrEmpty(key))
				return false;

			// Pure numeric / format placeholder: "0", "100", "00/000", "0:00", "1/2"
			if (Regex.IsMatch(key, @"^[\d\s/:.%\-\n\r]+$"))
				return true;

			// Runtime format marker prefix convention (e.g. "#Level 2", "#Score: 100")
			if (key.StartsWith("#"))
				return true;

			// Private-use-area Unicode characters (icon fonts, e.g. Font Awesome \uF03D)
			foreach (char c in key)
			{
				if (c >= '\uE000' && c <= '\uF8FF')
					return true;
			}

			// Rich-text tags wrapping only numeric/whitespace content (e.g. "<color=...>9</color>\n1")
			string stripped = Regex.Replace(key, @"<[^>]+>", string.Empty);
			if (!string.IsNullOrWhiteSpace(stripped) && Regex.IsMatch(stripped, @"^[\d\s/:.%\-\n\r]+$"))
				return true;

			return false;
		}
#endif

		/// <summary>
		/// Gets or sets the localization key.
		/// Setting this immediately re-translates the text if <see cref="AutoLocalize"/> is enabled.
		/// </summary>
		public string LocaKey
		{
			get
			{
				if (string.IsNullOrEmpty(m_locaKey))
					return base.text;
				return m_locaKey;
			}
			set
			{
				m_locaKey = value;
				ApplyTranslation();
			}
		}

		/// <summary>
		/// Overrides the base <see cref="TextMeshProUGUI.text"/> property to intercept external writes.
		/// When <see cref="AutoLocalize"/> is enabled:
		/// - Writes from outside this class are treated as new <see cref="LocaKey"/> assignments and trigger translation.
		/// - A warning is logged in the editor if a key is already set (suggests using <see cref="LocaKey"/> instead).
		/// When disabled, behaves identically to the base property.
		/// </summary>
		/// <remarks>
		/// The re-entry guard (<c>m_isSettingInternally</c>) is required because <c>ApplyTranslation</c>
		/// writes through <c>base.text</c>, which routes back into this same override. Without the guard
		/// that call would be misidentified as an external write and loop indefinitely.
		/// </remarks>
		public override string text
		{
			get => base.text;
			set
			{
				if (m_isSettingInternally)
				{
					base.text = value;
					return;
				}

				if (m_autoLocalize)
				{
#if UNITY_EDITOR
					if (Application.isPlaying)
					{
						var mgr = LocaManager.Instance;
						if (mgr != null && mgr.HasKey(value, m_group))
						{
							// Valid loca key — accept as a new key assignment and translate.
							m_locaKey = value;
							ApplyTranslation();
						}
						else
						{
							UnityEngine.Debug.LogError(
								$"[Loca] '.text' was set to '{value}' while AutoLocalize is active, " +
								$"but '{value}' is not a valid loca key. " +
								$"Use the LocaKey property instead of setting .text directly, or disable AutoLocalize.\n" +
								$"Path: {this.GetAssetPathAndPath()}",
								this);
							
							base.text = value; // Still apply the text so the user sees the result of their action, even if it's not a valid key.
							m_autoLocalize = false; // Disable auto-localization to prevent further confusion until the user explicitly re-enables it.
						}
						
						return;
					}
#endif
					m_locaKey = value;
					ApplyTranslation();
				}
				else
				{
					base.text = value;
				}
			}
		}

		protected override void Awake()
		{
			base.Awake();
			// After a "Replace with Localized Text" YAML swap the m_locaKey field will be empty
			// because the original TextMeshProUGUI did not have it. In that case, seed the key
			// from the existing TMP text so the component behaves correctly immediately.
			if (m_autoLocalize && string.IsNullOrEmpty(m_locaKey) && !string.IsNullOrEmpty(base.text))
				m_locaKey = base.text;
		}

		protected override void OnEnable()
		{
			base.OnEnable();
			UiEventDefinitions.EvLanguageChanged.AddListener(OnLanguageChanged);
			ApplyTranslation();
		}

		protected override void OnDisable()
		{
			base.OnDisable();
			UiEventDefinitions.EvLanguageChanged.RemoveListener(OnLanguageChanged);
		}

		private void OnLanguageChanged(string _languageId)
		{
			ApplyTranslation();
		}

		private void ApplyTranslation()
		{
			if (!m_autoLocalize || !Application.isPlaying)
				return;

			if (string.IsNullOrEmpty(m_locaKey))
				return; // No key set — leave the existing displayed text unchanged.

			var manager = LocaManager.Instance;
			if (manager == null)
			{
				// LocaManager is not yet initialized (e.g. Bootstrap still running at scene load).
				// Retry next frame so the translation is applied as soon as the manager is ready.
				StartCoroutine(RetryNextFrame());
				return;
			}

			m_isSettingInternally = true;
			try
			{
				base.text = manager.Translate(m_locaKey, _group: m_group);
			}
			finally
			{
				m_isSettingInternally = false;
			}
		}

		private IEnumerator RetryNextFrame()
		{
			yield return null;
			ApplyTranslation();
		}

		/// <summary>
		/// Triggers a translation immediately. Useful for non-auto-localized components that
		/// need their initial translation applied by an external controller (e.g. UiTextContainer).
		/// Has no effect outside play mode or when no localization key is set.
		/// </summary>
		public void Translate() => ApplyTranslation();
}
