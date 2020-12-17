using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace GuiToolkit
{
	[RequireComponent(typeof(TMP_Text))]
	public class UiTMPTranslator : MonoBehaviour, ILocaClient, ILocaListener
	{
		public LocaGroup m_locaGroup;

		private TMP_Text m_text;
		private string m_groupToken = "";
		private string m_key;

		private bool m_isInitialized;

		private LocaManager m_locaManager;
		private LocaManager LocaManager
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
			Text.text = LocaManager.Translate(m_key, m_groupToken);
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

			if (!m_isInitialized)
			{
				if (m_locaGroup == null)
				{
					UiLocaGroup uiLocaGroup = GetComponentInParent<UiLocaGroup>();
					if (uiLocaGroup != null)
						m_locaGroup = uiLocaGroup.m_locaGroup;
				}

				if (m_locaGroup != null)
					m_groupToken = m_locaGroup.Token;


				m_isInitialized = true;
			}

			m_key = Text.text;
			LocaManager.AddListener(this);
			Text.text = LocaManager.Translate(m_key, m_groupToken);
		}

		private void OnDisable()
		{
			if (!Application.isPlaying)
				return;

			LocaManager.RemoveListener(this);
			Text.text = m_key;
		}

#if UNITY_EDITOR
		public LocaGroup LocaGroup => m_locaGroup;
		public bool UsesMultipleLocaKeys => false;
		public string LocaKey => Text.text;
		public List<string> LocaKeys => null;
#endif
	}
}