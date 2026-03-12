using System.Collections;
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
	public class UiLocalizedTextMeshProUGUI : TextMeshProUGUI
	{
		[SerializeField] private bool m_autoLocalize = true;
		[SerializeField] private string m_group = string.Empty;

		private string m_locaKey;

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

		/// <summary>
		/// Gets or sets the localization key.
		/// Setting this immediately re-translates the text if <see cref="AutoLocalize"/> is enabled.
		/// </summary>
		public string LocaKey
		{
			get => m_locaKey;
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

#if UNITY_EDITOR
				if (m_autoLocalize && Application.isPlaying && !string.IsNullOrEmpty(m_locaKey))
					UnityEngine.Debug.LogWarning($"[Loca] External write to '{(gameObject != null ? gameObject.name : "?")}' " +
						$"while Auto Localize is active. Use LocaKey property instead.");
#endif

				if (m_autoLocalize)
				{
					m_locaKey = value;
					ApplyTranslation();
				}
				else
				{
					base.text = value;
				}
			}
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
			{
				m_isSettingInternally = true;
				try { base.text = string.Empty; }
				finally { m_isSettingInternally = false; }
				return;
			}

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
	}
}
