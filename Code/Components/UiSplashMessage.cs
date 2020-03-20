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
		public int m_id;
		public UiSimpleAnimation m_animationWhileVisible;

		[System.Serializable]
		private class CEvShowSplashMessage : UnityEvent<int,string,float> {}
		private static CEvShowSplashMessage EvShow = new CEvShowSplashMessage();

		protected override void AddEventListeners()
		{
			EvShow.AddListener(OnEvShow);
		}

		protected override void RemoveEventListeners()
		{
			EvShow.RemoveListener(OnEvShow);
		}

		public void Show(string _message, float _duration = 2)
		{
			StopAllCoroutines();
			gameObject.SetActive(true);
			m_duration = _duration;
			m_textComponent.text = _message;
			if (m_animationWhileVisible != null && SimpleShowHideAnimation != null)
			{
				SimpleShowHideAnimation.Stop(false);
				m_animationWhileVisible.Stop(false);

				m_animationWhileVisible.Duration = _duration;
				m_animationWhileVisible.m_onFinishOnce = () => SimpleShowHideAnimation.Play(true);
				Show(false, () => m_animationWhileVisible.Play());
			}
			else
			{
				Show();
				StartCoroutine(DelayedClose());
			}
		}

		public static void InvokeShow(string _message, int _id = 0, float _duration = 2 )
		{
			EvShow.Invoke(_id, _message, _duration);
		}

		private void OnEvShow( int _id, string _message, float _duration )
		{
			if (_id != m_id)
				return;
			Show(_message, _duration);
		}

		private IEnumerator DelayedClose()
		{
			yield return new WaitForSeconds(m_duration);
			Hide();
		}

	}
}