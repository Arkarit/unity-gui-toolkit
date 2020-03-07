using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GuiToolkit
{
	[RequireComponent(typeof(Button))]
	public class UiButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
	{
		[Tooltip("Simple animation (optional)")]
		public UiSimpleAnimation m_simpleAnimation;
		[Tooltip("Audio source (optional)")]
		public AudioSource m_audioSource;
		
		private TextMeshProUGUI m_tmpText;
		private Text m_text;
		private Button m_button;
		private bool m_initialized = false;

		public Button Button
		{
			get
			{
				InitIfNecessary();
				return m_button;
			}
		}

		public Button.ButtonClickedEvent OnClick => Button.onClick;

		public string Text
		{
			get
			{
				InitIfNecessary();
				if (m_tmpText)
					return m_tmpText.text;
				if (m_text)
					return m_text.text;
				return "";
			}

			set
			{
				InitIfNecessary();
				if (m_tmpText)
					m_tmpText.text = value;
				else if (m_text)
					m_text.text = value;
				else
					Debug.LogError($"No button text found for Button '{gameObject.name}', can not set string '{value}'");
			}
		}

		protected virtual void Awake()
		{
			InitIfNecessary();
		}

		public virtual void OnPointerDown( PointerEventData eventData )
		{
			if (m_simpleAnimation != null)
				m_simpleAnimation.Play();
			if (m_audioSource != null)
				m_audioSource.Play();
		}

		public virtual void OnPointerUp( PointerEventData eventData )
		{
			if (m_simpleAnimation != null)
				m_simpleAnimation.Play(true);
		}

		private void InitIfNecessary()
		{
			if (m_initialized)
				return;

			m_button = GetComponent<Button>();
			m_tmpText = m_button.GetComponentInChildren<TextMeshProUGUI>();
			m_text = m_button.GetComponentInChildren<Text>();
			if (m_tmpText == null && m_text == null)
				Debug.LogError($"No button text found for Button '{gameObject.name}'");
		}

	}
}