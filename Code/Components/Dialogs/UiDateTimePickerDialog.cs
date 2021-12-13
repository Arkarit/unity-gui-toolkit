using UnityEngine;

namespace GuiToolkit
{
	public class UiDateTimePickerDialog : UiView
	{
		[SerializeField] protected UiButton m_closeButton;

		protected override void OnEnable()
		{
			base.OnEnable();

			if (m_closeButton)
			{
				m_closeButton.gameObject.SetActive(true);
				m_closeButton.OnClick.AddListener(OnCloseButton);
			}
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