using GuiToolkit;
using UnityEngine;
using UnityEngine.UI;

public class DemoHud : UiView
{
	public UiButton m_showSettings;
	
	protected override void Start()
	{
		base.Start();
		Application.targetFrameRate = 60;

		DemoSettings.Create();
		DemoCheats.Create();
		m_showSettings.OnClick.AddListener(OnShowSettings);
	}
	
	private void OnShowSettings()
	{
		UiMain.Instance.ShowSettingsDialog();
	}
	
}
