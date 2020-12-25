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
		protected UiTMPTranslator m_titleTranslator;

		[SerializeField]
		protected PlayerSetting m_playerSetting;

		public string Title
		{
			set => m_titleTranslator.Text = value;
		}

		public virtual string Icon {get; set;}
	}
}