using TMPro;
using UnityEngine;

namespace GuiToolkit
{
	[AddComponentMenu("UI/Localized Text Mesh Pro UGUI")]
	public class UiLocalizedTextMeshProUGUI : TextMeshProUGUI
	{
		[SerializeField] private bool m_autoLocalize = true;
		[SerializeField] private string m_group = string.Empty;

		private string m_locaKey;
		private bool m_isSettingInternally;

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

		public string Group
		{
			get => m_group;
			set { m_group = value; ApplyTranslation(); }
		}

		/// <summary>
		/// The localization key. Setting this re-translates immediately.
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
			if (!m_autoLocalize || !Application.isPlaying || string.IsNullOrEmpty(m_locaKey))
				return;

			var manager = LocaManager.Instance;
			if (manager == null)
				return;

			m_isSettingInternally = true;
			try
			{
				base.text = manager.Translate(m_locaKey, m_group);
			}
			finally
			{
				m_isSettingInternally = false;
			}
		}
	}
}
