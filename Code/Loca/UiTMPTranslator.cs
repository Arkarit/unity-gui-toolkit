using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace GuiToolkit
{
	[RequireComponent(typeof(TMP_Text))]
	public class UiTMPTranslator : MonoBehaviour, ILocaClient, ILocaListener
	{
		public bool m_autoTranslate = true;

		private TMP_Text m_text;
		private string m_key;

		private UiLocaManager m_locaManager;
		private UiLocaManager LocaManager
		{
			get
			{
				if (m_locaManager == null)
					m_locaManager = UiMain.LocaManager;
				return m_locaManager;
			}
		}

		public void OnLanguageChanged(string _languageId)
		{
			if (m_autoTranslate)
				Text.text = LocaManager.Translate(m_key);
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
				m_text = value;
			}
		}

#if UNITY_EDITOR
		public bool UsesMultipleLocaKeys => false;
		public string LocaKey => Text.text;
		public List<string> LocaKeys => null;
#endif

		private void OnEnable()
		{
			if (!Application.isPlaying)
				return;

			m_key = Text.text;
			LocaManager.AddListener(this);
			if (m_autoTranslate)
				Text.text = LocaManager.Translate(m_key);
		}

		private void OnDisable()
		{
			if (!Application.isPlaying)
				return;

			LocaManager.RemoveListener(this);
			Text.text = m_key;
		}
	}
}