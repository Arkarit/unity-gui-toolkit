using System;
using System.Collections;
using System.Collections.Generic;
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

		private Button m_button;

		private void Awake()
		{
			m_button = GetComponent<Button>();
		}

		public void OnPointerDown( PointerEventData eventData )
		{
			m_simpleAnimation?.Play();
			m_audioSource?.Play();
		}

		public void OnPointerUp( PointerEventData eventData )
		{
			m_simpleAnimation?.Play(true);
		}
	}
}