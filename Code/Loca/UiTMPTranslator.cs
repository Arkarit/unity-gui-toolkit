using System;
using TMPro;
using UnityEngine;

namespace GuiToolkit
{
	[RequireComponent(typeof(TMP_Text))]
	[ExecuteAlways]
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
				m_text = value;
			}
		}

		private void OnEnable()
		{
#if UNITY_EDITOR
			if (!Application.isPlaying)
				UiLocaManager.Instance.ChangeKey(m_key, Text.text);
#endif

			m_key = Text.text;

			if (Application.isPlaying)
			{
				UiLocaManager.Instance.AddListener(this);
				if (m_autoTranslate)
					Text.text = UiLocaManager.Instance.Translate(m_key);
				return;
			}

#if UNITY_EDITOR
			TMPro_EventManager.TEXT_CHANGED_EVENT.Add(OnTMProTextChanged);
#endif
		}

		private void OnDisable()
		{
			if (Application.isPlaying)
			{
				UiLocaManager.Instance.RemoveListener(this);
				Text.text = m_key;
				return;
			}

#if UNITY_EDITOR
			TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(OnTMProTextChanged);
#endif
		}

#if UNITY_EDITOR
		private void OnTMProTextChanged( UnityEngine.Object _obj )
		{
			if ((TMP_Text) _obj != m_text || m_text.text == m_key)
				return;

			UiLocaManager.Instance.ChangeKey(m_key, m_text.text);
		}
#endif

	}
}