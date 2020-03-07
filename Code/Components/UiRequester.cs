using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit
{

	public class UiRequester : UiViewModal
	{
		public Button m_okButton;
		public Button m_cancelButton;
		public Button m_retryButton;
		public Button m_closeButton;

		public TextMeshProUGUI m_title;
		public TextMeshProUGUI m_text;

		private Action m_onOk;
		private Action m_onCancel;
		private Action m_onRetry;

		public class Options
		{
			string TextButtonOk;
			string TextButtonCancel;
			string TextButtonRetry;
			bool AllowOutsideTap = true;
		}

		public override bool AutoDestroyOnHide => true;

		public void OkRequester(string _title, string _text, Action _onClosed = null, Options _options = null)
		{
			m_title.text = _title;
			m_text.text = _text;
			m_onOk = _onClosed;
			if (m_okButton != null)
				m_okButton.onClick.AddListener(OnOk);
			if (m_cancelButton != null)
				m_cancelButton.gameObject.SetActive(false);
			if (m_retryButton != null)
				m_retryButton.gameObject.SetActive(false);
			if (m_closeButton != null)
				m_closeButton.onClick.AddListener(OnOk);
			OnClickCatcher = OnOk;
			gameObject.SetActive(true);
			Show();
		}

		protected override void OnDisable()
		{
			base.OnDisable();
			RemoveAllButtonListeners();
		}

		private void RemoveAllButtonListeners()
		{
			if (m_okButton)
				RemoveButtonListeners(m_okButton);
			if (m_cancelButton)
				RemoveButtonListeners(m_cancelButton);
			if (m_retryButton)
				RemoveButtonListeners(m_retryButton);
			if (m_closeButton)
				RemoveButtonListeners(m_closeButton);
		}

		private void RemoveButtonListeners( Button _button )
		{
			_button.onClick.RemoveListener(OnOk);
			_button.onClick.RemoveListener(OnCancel);
			_button.onClick.RemoveListener(OnRetry);
		}

		private void OnOk()
		{
			if (m_onOk != null)
				m_onOk.Invoke();
			Hide();
		}

		private void OnCancel()
		{
			if (m_onCancel != null)
				m_onCancel.Invoke();
			Hide();
		}

		private void OnRetry()
		{
			if (m_onRetry != null)
				m_onRetry.Invoke();
		}

	}
}