using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace GuiToolkit
{
	[RequireComponent(typeof(TMP_Text))]
	public class UiLocaComponent : UiThing, ILocaKeyProvider
	{
		public const bool AutoTranslate = false;
		[SerializeField] private string m_group = string.Empty;

		private TMP_Text m_text;
		private string m_locaKey;
		private string m_translatedText;
		private bool m_keyHasBeenSet;

		private LocaManager m_locaManager;

		public string Group
		{
			get => m_group;
			set => m_group = value;
		}

		protected override bool NeedsLanguageChangeCallback => true;
		protected override void OnLanguageChanged(string _languageId)
		{
			base.OnLanguageChanged(_languageId);
			if (AutoTranslate || m_keyHasBeenSet)
				Translate();
		}

		public string Text
		{
			get => TextComponent.text;
			set
			{
				m_keyHasBeenSet = true;
				m_locaKey = value;
				Translate();
			}
		}

		public string LocaKey
		{
			get
			{
				if (!AutoTranslate)
					return null;

				if (string.IsNullOrEmpty(m_locaKey))
					m_locaKey = TextComponent.text;

				return m_locaKey;
			}
		}

		private LocaManager LocaManager
		{
			get
			{
				if (m_locaManager == null)
					m_locaManager = LocaManager.Instance;

				return m_locaManager;
			}
		}

		public TMP_Text TextComponent
		{
			get
			{
				if (m_text == null)
					m_text = GetComponent<TMP_Text>();
				
				return m_text;
			}
		}

		private void Translate()
		{
			m_translatedText = LocaManager.Translate(m_locaKey, m_group);
			TextComponent.text = m_translatedText;
		}

		protected override void OnEnable()
		{
			base.OnEnable();

			if (!Application.isPlaying)
				return;

			if (!AutoTranslate && !m_keyHasBeenSet)
				return;

			if (string.IsNullOrEmpty(m_locaKey))
				m_locaKey = TextComponent.text;
			if (string.IsNullOrEmpty(m_locaKey))
				return;

			Translate();
		}

#if UNITY_EDITOR
		public bool UsesMultipleLocaKeys => false;
		public List<string> LocaKeys => null;
#endif
	}
}