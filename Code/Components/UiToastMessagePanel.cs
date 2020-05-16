using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace GuiToolkit
{
	public class UiToastMessagePanel : UiPanel
	{
		public float m_duration;
		public TextMeshProUGUI m_textComponent;
		public int m_id;
		public UiSimpleAnimation m_animationWhileVisible;

		public override bool AutoDestroyOnHide => true;
		public override bool Poolable => true;

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
				m_animationWhileVisible.m_onFinishOnce = () => Hide();
				Show(false, () => m_animationWhileVisible.Play());
			}
			else
			{
				Show();
				StartCoroutine(DelayedClose());
			}
		}

		private IEnumerator DelayedClose()
		{
			yield return new WaitForSeconds(m_duration);
			Hide();
		}

	}
}