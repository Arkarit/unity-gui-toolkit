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
		public UiButton[] m_buttons = new UiButton[3];

		public UiButton m_closeButton;

		public TextMeshProUGUI m_title;
		public TextMeshProUGUI m_text;

		private Action m_onOk;
		private Action m_onCancel;
		private Action m_onRetry;

		private bool m_allowOutsideTap;

		public class Options
		{
			public string[] ButtonText = new string[0];
			public bool AllowOutsideTap = true;
		}

		public override bool AutoDestroyOnHide => true;

		private void SetButton( int _idx, UnityEngine.Events.UnityAction _onClick, string _text )
		{
			if (_idx >= m_buttons.Length || m_buttons[_idx] == null)
				return;
			m_buttons[_idx].OnClick.AddListener( _onClick );
			m_buttons[_idx].Text = _text;
		}

		private void ShowButtons( int _numberOfButtons )
		{
			UiButton foundButton = null;

			for (int i=0; i<m_buttons.Length; i++)
			{
				if(m_buttons[i] != null)
				{
					m_buttons[i].gameObject.SetActive(i < _numberOfButtons);
					foundButton = m_buttons[i];
				}
			}

			if (foundButton)
			{
				UiDistortGroup distortGroup = foundButton.transform.parent.GetComponent<UiDistortGroup>();
				if (distortGroup)
					distortGroup.Refresh();
			}
		}

		private void SetClickCatcher( Action _action )
		{
			if (m_allowOutsideTap)
				OnClickCatcher = _action;
			else
				OnClickCatcher = Wiggle;
		}

		public void OkRequester(string _title, string _text, Action _onClosed = null, Options _options = null)
		{
			m_title.text = _title;
			m_text.text = _text;
			m_onOk = _onClosed;

			ShowButtons(1);
			SetButton(0, OnOk, "OK");

			EvaluateOptions(_options);

			if (m_closeButton != null)
				m_closeButton.OnClick.AddListener(OnOk);

			SetClickCatcher(OnOk);

			gameObject.SetActive(true);
			Show();
		}

		private void Wiggle()
		{
			foreach(var button in m_buttons)
				if (button != null && button.gameObject.activeInHierarchy)
					button.Wiggle();
		}

		public void YesNoRequester(string _title, string _text, Action _onOk, Action _onCancel = null, Options _options = null)
		{
			m_title.text = _title;
			m_text.text = _text;
			m_onOk = _onOk;
			m_onCancel = _onCancel;

			ShowButtons(2);
			SetButton(0, OnOk, "Yes");
			SetButton(1, OnCancel, "No");

			EvaluateOptions(_options);

			if (m_closeButton != null)
				m_closeButton.OnClick.AddListener(OnCancel);

			SetClickCatcher( OnCancel );

			gameObject.SetActive(true);
			Show();
		}

		private void EvaluateOptions( Options _options )
		{
			if ( _options == null )
			{
				m_allowOutsideTap = true;
				return;
			}

			for (int i=0; i<m_buttons.Length && i<_options.ButtonText.Length; i++)
				if (m_buttons[i] != null)
					m_buttons[i].Text = _options.ButtonText[i];

			m_allowOutsideTap = _options.AllowOutsideTap;
		}

		protected override void OnDisable()
		{
			base.OnDisable();
			RemoveAllButtonListeners();
		}

		private void RemoveAllButtonListeners()
		{
			foreach(var button in m_buttons)
				RemoveButtonListeners(button);
		}

		private void RemoveButtonListeners( UiButton _button )
		{
			if (_button == null)
				return;

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