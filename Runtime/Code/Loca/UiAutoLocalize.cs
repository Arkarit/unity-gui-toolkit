using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace GuiToolkit
{
	/// <summary>
	/// Automatic localization component for TMP_Text.
	/// 
	/// This component owns the associated TMP_Text content at runtime and replaces it with the
	/// localized translation of a key.
	/// 
	/// The localization key is derived from the current TMP_Text content when no explicit key
	/// was provided. An explicit key can be assigned via LocaKey.
	/// 
	/// External modifications of TMP_Text are tolerated as a fallback: if the text differs from
	/// the last translation applied by this component, the new text is treated as a new key on
	/// the next Translate() call. Prefer updating localization via UiAutoLocalize / LocaKey
	/// instead of writing to TMP_Text directly.
	/// </summary>

	[DisallowMultipleComponent]
	[RequireComponent(typeof(TMP_Text))]
	public class UiAutoLocalize : UiThing, ILocaKeyProvider
	{
		[SerializeField] private string m_group = string.Empty;

		private TMP_Text m_text;
		private string m_locaKey;
		private string m_lastTranslation;

		private LocaManager m_locaManager;

		/// <summary>
		/// Optional localization group used during translation.
		/// </summary>
		public string Group
		{
			get => m_group;
			set => m_group = value;
		}

		/// <inheritdoc/>
		protected override bool NeedsLanguageChangeCallback => true;

		/// <summary>
		/// Called when the active language changes.
		/// Forces a re-translation using the current localization key.
		/// </summary>
		protected override void OnLanguageChanged( string _languageId )
		{
			base.OnLanguageChanged(_languageId);
			Translate();
		}

		/// <summary>
		/// Current text of the underlying TMP_Text component.
		/// This property is read-only; prefer changing localization via LocaKey instead.
		/// </summary>
		public string Text => TextComponent.text;

		/// <summary>
		/// Localization key used for translation.
		/// 
		/// If no explicit key is set, the key is derived from the current TMP_Text content.
		/// 
		/// Note: This getter may have side effects by assigning the derived key.
		/// </summary>
		public string LocaKey
		{
			get
			{
				if (string.IsNullOrEmpty(m_locaKey))
					m_locaKey = TextComponent.text;

				return m_locaKey;
			}

			set
			{
				m_locaKey = value;
				m_lastTranslation = null;
				Translate();
			}
		}

		/// <summary>
		/// Cached access to the global LocaManager instance.
		/// </summary>
		private LocaManager LocaManager
		{
			get
			{
				if (m_locaManager == null)
					m_locaManager = LocaManager.Instance;

				return m_locaManager;
			}
		}

		/// <summary>
		/// Cached access to the associated TMP_Text component.
		/// </summary>
		public TMP_Text TextComponent
		{
			get
			{
				if (m_text == null)
					m_text = GetComponent<TMP_Text>();

				return m_text;
			}
		}

		/// <summary>
		/// Applies localization at runtime.
		/// 
		/// If the TMP_Text has been modified externally since the last translation was applied,
		/// the modified text is treated as a new localization key (fallback behavior).
		/// </summary>
		private void Translate()
		{
			if (!Application.isPlaying)
				return;

			// Fallback:
			// The text was translated previously, but has been modified externally in the meantime.
			// Treat the modified text as a new localization key.
			if (!string.IsNullOrEmpty(m_lastTranslation) && Text != m_lastTranslation)
				m_locaKey = null;

			// Intentionally trigger the LocaKey getter to ensure a key is derived when none is set.
			var _ = LocaKey;

			if (string.IsNullOrWhiteSpace(m_locaKey))
			{
				TextComponent.text = string.Empty;
				return;
			}

			var translatedText = LocaManager.Translate(m_locaKey, m_group);
			m_lastTranslation = translatedText;
			TextComponent.text = translatedText;
		}

		/// <summary>
		/// Ensures translation is applied when the component becomes enabled.
		/// </summary>
		protected override void OnEnable()
		{
			base.OnEnable();
			Translate();
		}

#if UNITY_EDITOR
		/// <inheritdoc/>
		public bool UsesMultipleLocaKeys => false;

		/// <inheritdoc/>
		public List<string> LocaKeys => null;
#endif
	}
}
