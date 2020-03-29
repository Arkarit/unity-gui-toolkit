using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GuiToolkit
{
	public class UiButtonBase : UiThing, IPointerDownHandler, IPointerUpHandler
	{
		[Tooltip("Simple animation (optional)")]
		public UiSimpleAnimation m_simpleAnimation;
		[Tooltip("Audio source (optional)")]
		public AudioSource m_audioSource;
		
		private TextMeshProUGUI m_tmpText;
		private Text m_text;
		private bool m_initialized = false;

#if UNITY_EDITOR
		[SerializeField]
		private string m_textContent;
#endif

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
				if (value == null)
					return;

				InitIfNecessary();
				if (m_tmpText)
					m_tmpText.text = value;
				else if (m_text)
					m_text.text = value;
				else
					Debug.LogError($"No button text found for Button '{gameObject.name}', can not set string '{value}'");
			}
		}

		protected virtual void Init() { }

		protected override void Awake()
		{
			base.Awake();
			InitIfNecessary();
#if UNITY_EDITOR
			if (m_tmpText)
				m_textContent = m_tmpText.text;
			if (m_text)
				m_textContent = m_text.text;
#endif
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

		protected void InitIfNecessary()
		{
			if (m_initialized)
				return;

			m_tmpText = GetComponentInChildren<TextMeshProUGUI>();
			m_text = GetComponentInChildren<Text>();

			Init();
		}

#if UNITY_EDITOR
		private void OnValidate()
		{
			Text = m_textContent;
		}
#endif
	}
}