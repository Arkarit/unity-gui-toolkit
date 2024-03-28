using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace GuiToolkit
{
	/// <summary>
	/// A standalone toast message.
	/// It is created within the "View" system. Once fired, requires no further action.
	/// UiToastMessageView's can however overlap, if multiple at the same time are visible, since
	/// they are hierarchically independent.
	/// If you need multiple toast messages at the same time, consider setting up/using UiToastMessageContainer.
	/// </summary>
	public class UiToastMessageView : UiView
	{
		public float m_duration;
		public TextMeshProUGUI m_textComponent;
		public int m_id;
		public UiSimpleAnimation m_animationWhileVisible;

		private class CEvHideAll : UnityEvent<bool> {}
		private static CEvHideAll EvHide = new CEvHideAll();

		public override bool AutoDestroyOnHide => true;
		public override bool Poolable => true;

		protected override void AddEventListeners()
		{
			base.AddEventListeners();
			EvHide.AddListener(OnEvHide);
		}

		protected override void RemoveEventListeners()
		{
			base.RemoveEventListeners();
			EvHide.RemoveListener(OnEvHide);
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

				m_animationWhileVisible.Reset();
				if (_duration >= 0)
					m_animationWhileVisible.Duration = _duration;

				m_animationWhileVisible.m_onFinishOnce = () => Hide();
				ShowTopmost(false, () => m_animationWhileVisible.Play());
			}
			else
			{
				ShowTopmost();
				StartCoroutine(DelayedClose());
			}
		}

		public static void HideAll(bool _instant = false)
		{
			EvHide.Invoke(_instant);
		}

		private void OnEvHide(bool _instant)
		{
			Hide(_instant);
		}

		private IEnumerator DelayedClose()
		{
			yield return new WaitForSeconds(m_duration);
			Hide();
		}

	}
}