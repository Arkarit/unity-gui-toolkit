using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;

namespace GuiToolkit
{
	/// <summary>
	/// TextMeshProUGUI subclass with automatic localization support.
	/// Setting the <see cref="LocaKey"/> property immediately applies the translated text.
	/// Re-translates on language changes.
	/// Replaces the deprecated <see cref="UiAutoLocalize"/> component.
	/// <para>
	/// A component is considered "not translatable" when its effective key is empty or a
	/// placeholder of the form <c>[AnyText]</c> (square brackets).  In that case
	/// <see cref="LocaKey"/> returns an empty string and no translation is applied.
	/// </para>
	/// </summary>
	[AddComponentMenu("UI/Localized Text Mesh Pro UGUI")]
	public class UiLocalizedTextMeshProUGUI : TextMeshProUGUI, ILocaKeyProvider
	{
		[SerializeField] private bool m_isTranslated = true;
		[SerializeField] private string m_group = string.Empty;
		[SerializeField] private string m_locaKey = string.Empty;

		// TMP's text setter does not fire an event we can filter; it is a plain property.
		// Overriding it (see below) creates a direct re-entry path: ApplyTranslation() calls
		// base.text = …, which would call our override again and loop forever.
		// This flag breaks the cycle so only the outermost call reaches base.text.
		private bool m_isSettingInternally;

		/// <summary>
		/// When <c>true</c> (default), the component automatically translates its key via <see cref="LocaManager"/>.
		/// When <c>false</c>, the component behaves exactly like a plain <see cref="TextMeshProUGUI"/>;
		/// no translation is applied and any attempt to set <see cref="LocaKey"/> is rejected with an error.
		/// Can only be changed in the Inspector (no public setter).
		/// </summary>
		public bool IsTranslated => m_isTranslated;

		/// <summary>
		/// Returns <c>true</c> when <paramref name="s"/> is empty or has the placeholder form
		/// <c>[AnyText]</c>, meaning it should not be used as a localization key.
		/// </summary>
		private static bool IsPlaceholderText(string s)
			=> string.IsNullOrEmpty(s) || (s.Length >= 2 && s[0] == '[' && s[s.Length - 1] == ']');

		/// <summary>
		/// Gets or sets the localization group namespace.
		/// Changing this triggers immediate re-translation.
		/// </summary>
		public string Group
		{
			get => m_group;
			set { m_group = value; ApplyTranslation(); }
		}

#if UNITY_EDITOR
		bool ILocaKeyProvider.UsesLocaKey => m_isTranslated && !IsPlaceholderText(LocaKey) && !IsObviouslyRuntimeValue(LocaKey);
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
		/// Setting this immediately re-translates the text.
		/// Returns an empty string when no key is set or the effective key is a placeholder
		/// (<c>[AnyText]</c> form), in which case the raw <see cref="TextMeshProUGUI.text"/>
		/// is displayed unchanged.
		/// </summary>
		public string LocaKey
		{
			get
			{
				// Prefer explicit stored key; fall back to the raw TMP text.
				string effectiveKey = string.IsNullOrEmpty(m_locaKey) ? base.text : m_locaKey;
				return IsPlaceholderText(effectiveKey) ? string.Empty : effectiveKey;
			}
			set
			{
				if (!m_isTranslated)
				{
					UiLog.LogError(
						$"[Loca] Attempted to set LocaKey on '{this.GetAssetPathAndPath()}' " +
						$"but m_isTranslated is false. Key assignment ignored.",
						this);
					return;
				}
				m_locaKey = value;
				ApplyTranslation();
			}
		}

		/// <summary>
		/// Overrides the base <see cref="TextMeshProUGUI.text"/> property to intercept external writes.
		/// External writes are treated as new <see cref="LocaKey"/> assignments and trigger translation.
		/// In the Editor during play mode a verbose log is written when the value is not a registered key.
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
				// When translation is disabled behave exactly like plain TextMeshProUGUI.
				if (!m_isTranslated)
				{
					base.text = value;
					return;
				}

				if (m_isSettingInternally)
				{
					base.text = value;
					return;
				}

#if UNITY_EDITOR
				if (Application.isPlaying)
				{
					var mgr = LocaManager.Instance;
					if (mgr != null && !mgr.HasKey(value, m_group))
					{
						UiLog.LogVerbose(
							$"[Loca] '.text' was set to '{value}' but '{value}' is not a registered loca key. " +
							$"Use the LocaKey property for key assignments.\n" +
							$"Path: {this.GetAssetPathAndPath()}",
							this);
					}
				}
#endif
				m_locaKey = value;
				ApplyTranslation();
			}
		}

		protected override void Awake()
		{
			base.Awake();
			if (!m_isTranslated)
			{
				// Clear any stored key so the component is fully inert.
				m_locaKey = string.Empty;
				return;
			}
			// After a YAML swap the m_locaKey field may be empty while base.text still holds
			// the design-time placeholder. Seed m_locaKey from base.text unless it is a placeholder.
			if (string.IsNullOrEmpty(m_locaKey) && !IsPlaceholderText(base.text))
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
			if (!m_isTranslated)
				return;

			if (!Application.isPlaying)
				return;

			if (IsPlaceholderText(m_locaKey))
				return; // Placeholder or empty — leave the existing displayed text unchanged.

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
}
