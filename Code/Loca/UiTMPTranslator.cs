using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace GuiToolkit
{
	[RequireComponent(typeof(TMP_Text))]
	public class UiTMPTranslator : MonoBehaviour, ILocaClient, ILocaListener
	{
		[SerializeField] bool m_autoTranslate = true;

		private TMP_Text m_text;
		private string m_locaKey;
		private string m_translatedText;
		private bool m_firstEnabled = true;

		private UiLocaManager m_locaManager;

		public bool AutoTranslate => m_autoTranslate;

		public void OnLanguageChanged(string _languageId)
		{
			Translate();
		}

		public string Text
		{
			get => TextComponent.text;
			set
			{
				m_locaKey = value;
				Translate();
			}
		}

		public string LocaKey
		{
			get
			{
				if (string.IsNullOrEmpty(m_locaKey))
					m_locaKey = TextComponent.text;
				return m_locaKey;
			}
		}

		private UiLocaManager LocaManager
		{
			get
			{
				if (m_locaManager == null)
					m_locaManager = UiMain.Instance.LocaManager;
				return m_locaManager;
			}
		}

		private TMP_Text TextComponent
		{
			get
			{
				if (m_text == null)
					m_text = GetComponent<TMP_Text>();
				return m_text;
			}
			set
			{
				m_text = value;
			}
		}

		private void Translate()
		{
			m_translatedText = LocaManager.Translate(m_locaKey);
			TextComponent.text = m_translatedText;
		}

		private void OnEnable()
		{
			if (!Application.isPlaying)
				return;

			LocaManager.AddListener(this);
			if (string.IsNullOrEmpty(m_locaKey))
				m_locaKey = TextComponent.text;
			if (string.IsNullOrEmpty(m_locaKey))
				return;

			if (m_autoTranslate || !m_firstEnabled)
				Translate();

			m_firstEnabled = false;
		}

		private void OnDisable()
		{
			if (!Application.isPlaying)
				return;

			LocaManager.RemoveListener(this);
		}

#if UNITY_EDITOR
		public bool UsesMultipleLocaKeys => false;
		public List<string> LocaKeys => null;
#endif
	}
}