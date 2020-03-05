using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace GuiToolkit
{
	public class UiSplashMessage : UiView
	{
		public float m_duration;
		public TextMeshProUGUI m_textComponent;

		[System.Serializable]
		public class CEvShowSplashMessage : UnityEvent<string,float> {}
		public static CEvShowSplashMessage EvShow = new CEvShowSplashMessage();

		protected override void Awake()
		{
			EvShow.AddListener(Show);
		}

		public void Show(string _message, float _duration = 3)
		{
			StopAllCoroutines();
			gameObject.SetActive(true);
			m_duration = _duration;
			m_textComponent.text = _message;
			Show();
			StartCoroutine(DelayedClose());
		}

		private IEnumerator DelayedClose()
		{
			yield return new WaitForSeconds(m_duration);
			Hide();
		}

	}
}