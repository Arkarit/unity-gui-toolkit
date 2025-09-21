using GuiToolkit;
using System.Collections.Generic;
using System.Linq;
using GuiToolkit.Style;
using UnityEngine;

public class DemoMain : LocaMonoBehaviour
{
	protected void Start()
	{
		Application.targetFrameRate = 60;

		DemoSettings.Create();
		DemoCheats.Create();
		
		// Alternative way to listen to player settings changed:
		UiEventDefinitions.EvPlayerSettingChanged.AddListener(OnPlayerSettingChanged);

		UiMain.Instance.LoadScene("DemoScene1");
	}

	private void OnPlayerSettingChanged(PlayerSetting _playerSetting)
	{
		//UiLog.Log($"Player setting '{_playerSetting.Key}' changed to {_playerSetting.Value}");
	}
}
