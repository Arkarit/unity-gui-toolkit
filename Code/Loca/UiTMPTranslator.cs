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
		private TMP_Text m_text;
		private string m_locaKey;
		private string m_translatedText;

		private UiLocaManager m_locaManager;
		private bool m_textChangeGuard;

		public string LocaKey
		{
			get
			{
				if (string.IsNullOrEmpty(m_locaKey))
					m_locaKey = Text.text;
				return m_locaKey;
			}
		}

		public void OnLanguageChanged(string _languageId)
		{
			Translate();
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
			m_locaKey = Text.text;
			Translate();
			TMPro_EventManager.TEXT_CHANGED_EVENT.Add(OnTMProTextChanged);
		}

		private void OnDisable()
		{
			if (!Application.isPlaying)
				return;

			LocaManager.RemoveListener(this);
			TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(OnTMProTextChanged);
			Text.text = m_locaKey;
		}

		private void OnTMProTextChanged( UnityEngine.Object _obj )
		{
			if (_obj != Text || this == null || !enabled || m_textChangeGuard)
				return;

			if (Text.text == m_translatedText)
				return;

			m_locaKey = Text.text;

			// TMP doesn't like it at all, if the text is changed in the on text changed callback, even when using guard.
			// Gives completely blurry text. So lets do it delayed. 
			TranslateDelayed();
		}

		private void TranslateDelayed()
		{
			StartCoroutine(TranslateCoroutine());
		}

		private IEnumerator TranslateCoroutine()
		{
			yield return 0;
			Translate();
		}

		private void Translate()
		{
			m_textChangeGuard = true;
			m_translatedText = LocaManager.Translate(m_locaKey);
			Text.text = m_translatedText;
			m_textChangeGuard = false;
		}

#if UNITY_EDITOR
		public bool UsesMultipleLocaKeys => false;
		public List<string> LocaKeys => null;
#endif
	}
}