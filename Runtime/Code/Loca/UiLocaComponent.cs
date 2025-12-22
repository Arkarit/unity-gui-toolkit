using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace GuiToolkit
{
	[RequireComponent(typeof(TMP_Text))]
	public class UiLocaComponent : UiThing, ILocaKeyProvider
	{
		[SerializeField] bool m_autoTranslate = true;
		[SerializeField] private string m_group = string.Empty;

		private TMP_Text m_text;
		private string m_locaKey;
		private bool m_translationDirty;
		private string m_lastTranslation;

		private LocaManager m_locaManager;

		public bool AutoTranslate
		{
			get => m_autoTranslate;
			set
			{
				if (m_autoTranslate == value)
					return;
				m_autoTranslate = value;
				m_translationDirty = true;
				Translate();
			}
		}

		public string Group
		{
			get => m_group;
			set => m_group = value;
		}

		protected override bool NeedsLanguageChangeCallback => true;
		protected override void OnLanguageChanged(string _languageId)
		{
			base.OnLanguageChanged(_languageId);
			m_translationDirty = true;
			Translate();
		}

		public string Text => TextComponent.text;

		public string LocaKey
		{
			get
			{
				if (m_autoTranslate && string.IsNullOrEmpty(m_locaKey))
					m_locaKey = TextComponent.text;

				return m_locaKey;
			}
			
			set
			{
				m_translationDirty = true;
				m_locaKey = value;
				Translate();
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
			if (!Application.isPlaying)
				return;
			
			// Edge case: Text has already been translated, but changed in the meantime
			if (!m_translationDirty && !string.IsNullOrEmpty(m_lastTranslation) && Text != m_lastTranslation)
			{
				m_locaKey = null;
				m_translationDirty = true;
			}
			
			if (!m_autoTranslate && !m_translationDirty)
				return;
			
			var _ = LocaKey;
			if (string.IsNullOrWhiteSpace(m_locaKey))
			{
				TextComponent.text = String.Empty;
				return;
			}

			m_translationDirty = false;
			
			TextComponent.text = LocaManager.Translate(m_locaKey, m_group);
			m_lastTranslation = TextComponent.text;
		}

		protected override void OnEnable()
		{
			base.OnEnable();
			Translate();
		}

#if UNITY_EDITOR
		public bool UsesMultipleLocaKeys => false;
		public List<string> LocaKeys => null;
#endif
	}
}