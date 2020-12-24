using TMPro;
using UnityEngine;

namespace GuiToolkit
{
	public class UiPlayerSettingKeyBinding : UiPlayerSettingButton
	{
		[SerializeField] protected TMP_Text m_keyDisplay;

		private KeyCode m_keyCode;

		public KeyCode Value
		{
			get => m_keyCode;
			set
			{
				m_keyCode = value;
				m_keyDisplay.text = value.ToString();
			}
		}

		protected override void OnValueChanged()
		{
			UiMain.Instance.KeyPressRequester((KeyCode keyCode) =>
			{
				m_keyDisplay.text = keyCode.ToString();
			});
		}
	}
}