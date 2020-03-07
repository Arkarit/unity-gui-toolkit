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
		public UiButton m_okButton;
		public UiButton m_cancelButton;
		public UiButton m_retryButton;
		public UiButton m_closeButton;

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
			{
				m_okButton.OnClick.AddListener(OnOk);
				m_okButton.Text = "OK";
			}

			if (m_cancelButton != null)
				m_cancelButton.gameObject.SetActive(false);
			if (m_retryButton != null)
				m_retryButton.gameObject.SetActive(false);
			if (m_closeButton != null)
				m_closeButton.OnClick.AddListener(OnOk);
			OnClickCatcher = OnOk;
			gameObject.SetActive(true);
			Show();
		}

		public void YesNoRequester(string _title, string _text, Action _onOk, Action _onCancel = null, Options _options = null)
		{
			m_title.text = _title;
			m_text.text = _text;
			m_onOk = _onOk;
			m_onCancel = _onCancel;

			if (m_okButton != null)
			{
				m_okButton.OnClick.AddListener(OnOk);
				m_okButton.Text = "Yes";
			}
			if (m_cancelButton != null)
			{
				m_cancelButton.OnClick.AddListener(OnCancel);
				m_cancelButton.Text = "No";
			}
			if (m_retryButton != null)
				m_retryButton.gameObject.SetActive(false);
			if (m_closeButton != null)
				m_closeButton.OnClick.AddListener(OnCancel);
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

		private void RemoveButtonListeners( UiButton _button )
		{
			_button.OnClick.RemoveListener(OnOk);
			_button.OnClick.RemoveListener(OnCancel);
			_button.OnClick.RemoveListener(OnRetry);
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