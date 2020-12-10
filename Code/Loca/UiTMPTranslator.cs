using System;
using TMPro;
using UnityEngine;

namespace GuiToolkit
{
	[RequireComponent(typeof(TMP_Text))]
	public class UiTMPTranslator : MonoBehaviour, ILocaListener
	{
		public bool m_autoTranslate = true;

		private TMP_Text m_text;
		private string m_key;

		public void OnLanguageChanged(string _languageId)
		{
			if (m_autoTranslate)
				Text.text = UiLocaManager.Instance.Translate(m_key);
		}

		private TMP_Text Text
		{
			get
			{
				if (m_text == null)
					m_text = GetComponent<TMP_Text>();
				return m_text;
			}
			set
			{
				if (m_text == null)
					return;
				m_text = value;
			}
		}

		private void OnEnable()
		{
			UiLocaManager.Instance.AddListener(this);
			m_key = Text.text;
			if (m_autoTranslate)
				Text.text = UiLocaManager.Instance.Translate(m_key);
		}

		private void OnDisable()
		{
			UiLocaManager.Instance.RemoveListener(this);
			Text.text = m_key;
		}
	}
}