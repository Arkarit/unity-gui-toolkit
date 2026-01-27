using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit
{
	public class UiPlayerSettingKeyBinding : UiPlayerSettingButton
	{
		[SerializeField] protected TMP_Text m_keyDisplay;
		[SerializeField] protected bool m_markIfNone = true;
		[SerializeField] protected Color m_backgroundColorIfNone = new Color(0x80/255.0f, 0x19/255.0f, 0x00/255.0f);
		[SerializeField] protected Color m_textColorIfNone = Color.red;
		[SerializeField] protected Image m_optionalBackgroundImage;
		[SerializeField] protected UiGradientSimple m_optionalBackgroundGradient;
		
		public override object Value
		{
			get => base.Value;
			set
			{
				base.Value = value;
				m_keyDisplay.text = value.ToString();
			}
		}

		protected override void OnClick()
		{
			UiMain.Instance.KeyPressRequester(m_playerSetting.Options, (KeyCode _keyCode) =>
			{
				if (m_markIfNone)
					MarkIfNone(_keyCode);
				Value = _keyCode;
			});
		}

		private void MarkIfNone(KeyCode _keyCode)
		{
			bool mark = _keyCode == KeyCode.None;
			m_keyDisplay.color = mark ? m_textColorIfNone : Color.white;
			if (m_optionalBackgroundImage != null)
				m_optionalBackgroundImage.color = mark ? m_backgroundColorIfNone : Color.white;
			if (m_optionalBackgroundGradient)
				m_optionalBackgroundGradient.enabled = !mark;
		}

		protected override void OnPlayerSettingChanged( PlayerSetting _playerSetting )
		{
			base.OnPlayerSettingChanged(_playerSetting);
			if (!Initialized || !m_markIfNone || !_playerSetting.IsKeyBinding)
				return;
			if (m_playerSetting.GetDefaultValue<KeyCode>() != _playerSetting.GetDefaultValue<KeyCode>())
				return;
			MarkIfNone(_playerSetting.GetValue<KeyCode>());
		}
	}
}