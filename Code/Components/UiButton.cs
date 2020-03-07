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

		public void OnPointerDown( PointerEventData eventData )
		{
			if (m_simpleAnimation != null)
				m_simpleAnimation.Play();
			if (m_audioSource != null)
				m_audioSource.Play();
		}

		public void OnPointerUp( PointerEventData eventData )
		{
			if (m_simpleAnimation != null)
				m_simpleAnimation.Play(true);
		}
	}
}