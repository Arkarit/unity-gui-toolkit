using System;
using UnityEngine;

namespace GuiToolkit
{
	public class UiDateTimePickerDialog : UiView
	{
		[SerializeField] protected UiButton m_closeButton;
		[SerializeField] protected UiButton m_okButton;
		[SerializeField] protected UiButton m_cancelButton;

		public override bool AutoDestroyOnHide => true;

		protected override void OnEnable()
		{
			base.OnEnable();

			if (m_closeButton)
			{
				m_closeButton.gameObject.SetActive(true);
				m_closeButton.OnClick.AddListener(OnCloseButton);
				OnClickCatcher = OnCloseButton;
			}

			m_okButton.OnClick.AddListener(OnOk);
			m_cancelButton.OnClick.AddListener(OnCloseButton);
		}

		private void OnOk()
		{
			Hide();
		}

		protected override void OnDisable()
		{
			base.OnDisable();

			if (m_closeButton)
				m_closeButton.OnClick.RemoveListener(OnCloseButton);
		}

		protected virtual void OnCloseButton()
		{
			Hide();
		}
	}
}