using TMPro;
using UnityEngine;

namespace GuiToolkit
{
	public class UiPlayerSettingKeyBinding : UiPlayerSettingButton
	{
		[SerializeField] protected TMP_Text m_keyDisplay;

		protected override void OnValueChanged()
		{
			UiMain.Instance.KeyPressRequester((KeyCode keyCode) =>
			{
				Debug.Log($"KeyCode: {keyCode}");
				m_keyDisplay.text = keyCode.ToString();
			});
		}
	}
}