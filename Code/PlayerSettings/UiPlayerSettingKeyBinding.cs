using TMPro;
using UnityEngine;

namespace GuiToolkit
{
	public class UiPlayerSettingKeyBinding : UiPlayerSettingButton
	{
		[SerializeField] protected TMP_Text m_keyDisplay;

		public override object Value
		{
			get => base.Value;
			set
			{
				base.Value = value;
				m_keyDisplay.text = value.ToString();
			}
		}

		protected override void OnValueChanged()
		{
			UiMain.Instance.KeyPressRequester((KeyCode _keyCode) =>
			{
				Value = _keyCode;
			});
		}
	}
}