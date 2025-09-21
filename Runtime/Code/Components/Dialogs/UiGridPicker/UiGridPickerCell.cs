using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace GuiToolkit
{
	[RequireComponent(typeof(UiButton))]
	public class UiGridPickerCell : UiThing
	{
		[SerializeField] private TMP_Text m_optionalText;


		private UiButton m_button;

		public UiButton Button
		{
			get
			{
				if (m_button == null)
					m_button = GetComponent<UiButton>();

				return m_button; 
			}
		}

		public string OptionalCaption
		{
			get => m_optionalText ? m_optionalText.text : string.Empty;
			set
			{
				if (!m_optionalText)
				{
					UiLog.LogWarning($"Attempt to set caption for '{gameObject.name}', but {nameof(m_optionalText)} is not set!\n{gameObject.GetPath()}");
					return;
				}

				m_optionalText.text = value;
			}
		}
	}
}