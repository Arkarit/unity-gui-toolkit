using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace GuiToolkit
{
	public class UiSplashMessage : UiView
	{
		public float m_duration = 3f;
		public TextMeshProUGUI m_textComponent;

protected override void OnEnable()
{
	base.OnEnable();
	Show("Test");
}

		public void Show(string _message)
		{
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