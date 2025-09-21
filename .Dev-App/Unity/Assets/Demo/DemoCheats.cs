using GuiToolkit;
using System.Collections.Generic;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

public static class DemoCheats
{
	[Conditional("CHEATS_ALLOWED")]
	public static void Create()
	{
		PlayerSettings.Instance.Add( new List<PlayerSetting>
		{
			// Demo cheats
			new
			(
				"Cheats", "Example", "Checkbox", false,
				new PlayerSettingOptions
				{
					IsSaveable = false,
					OnChanged = setting =>
					{
						UiLog.Log($"Checkbox checked:{(bool) setting.Value}");
					}
				}
			),
			new
			(
				"Cheats", "Example", "Button", null,
				new PlayerSettingOptions
				{
					IsSaveable = false,
					Titles = new List<string> {"Main Button Text", "Button Text"},
					OnChanged = _ =>
					{
						UiLog.Log($"Button clicked");
					}
				}
			),
		});
	}
}
