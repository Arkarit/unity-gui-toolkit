using System;
using System.Collections.Generic;
using System.ComponentModel;
using TMPro;
using UnityEngine;

namespace GuiToolkit
{
	public abstract class UiPlayerSettingBase : UiThing
	{
		[SerializeField]
		protected TMP_Text m_text;

		[SerializeField]
		protected PlayerSetting m_playerSetting;

		public string Title
		{
			get
			{
				if (m_text != null)
					return m_text.text;
				return "";
			}
			set
			{
				if (m_text != null)
					m_text.text = value;
			}
		}
	}
}