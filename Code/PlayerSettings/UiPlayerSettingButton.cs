using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace GuiToolkit
{
	public class UiPlayerSettingButton : UiPlayerSettingBase
	{
		[SerializeField]
		protected UiButton m_button;

		protected override void OnEnable()
		{
			base.OnEnable();
			m_button.OnClick.AddListener(OnClick);
		}

		protected override void OnDisable()
		{
			m_button.OnClick.RemoveListener(OnClick);
			base.OnDisable();
		}

		protected virtual void OnClick(){}
	}
}