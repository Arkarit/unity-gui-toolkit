using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace GuiToolkit
{
	[RequireComponent(typeof(TMP_Text))]
	public class UiTMPTranslator : MonoBehaviour, ILocaClient, ILocaListener
	{
		private TMP_Text m_text;
		private string m_locaKey;

		public string LocaKey
		{
			get
			{
				if (string.IsNullOrEmpty(m_locaKey))
					m_locaKey = m_text.text;
				return m_locaKey;
			}
		}

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
			Text.text = LocaManager.Translate(m_locaKey);
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

		private void OnEnable()
		{
			if (!Application.isPlaying)
				return;

			LocaManager.AddListener(this);
			Text.text = LocaManager.Translate(LocaKey);
		}

		private void OnDisable()
		{
			if (!Application.isPlaying)
				return;

			LocaManager.RemoveListener(this);
			Text.text = LocaKey;
		}

#if UNITY_EDITOR
		public bool UsesMultipleLocaKeys => false;
		public List<string> LocaKeys => null;
#endif
	}
}