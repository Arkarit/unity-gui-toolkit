using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GuiToolkit
{
	[RequireComponent(typeof(UiButton))]
	public class UiGridPickerCell : UiThing
	{
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
	}
}