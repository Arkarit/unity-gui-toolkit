using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GuiToolkit
{
	public class UiButtonStyleSwitcher : UiStyleSwitcherEnumBase<EButtonStyle>
	{
		[SerializeField]
		private EButtonStyle m_buttonStyle;

		protected void Start()
		{
			CurrentStyle = m_buttonStyle;
		}

		private void OnValidate()
		{
			CurrentStyle = m_buttonStyle;
		}
	}
}