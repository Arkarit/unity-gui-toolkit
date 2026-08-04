using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace GuiToolkit
{
	/// <summary>
	/// A self-dismissing UiPanel toast that shows a message in its TextMeshPro text for a given duration and then
	/// hides (returning to the pool since it is poolable and auto-destroys on hide). It can drive an optional
	/// while-visible UiSimpleAnimation instead of a plain timed close.
	/// </summary>
	public class UiToastMessagePanel : UiPanel
	{
		public float m_duration;
		public TextMeshProUGUI m_textComponent;
		public int m_id;
		public UiSimpleAnimation m_animationWhileVisible;

		public override bool AutoDestroyOnHide => true;
		public override bool Poolable => true;
		public override bool ShowDestroyFieldsInInspector => false;

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

				m_animationWhileVisible.OnFinishOnce.AddListener(() => Hide());
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