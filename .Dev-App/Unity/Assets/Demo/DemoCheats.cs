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
				"Cheats", "Example", "Checkbox", true,
				new PlayerSettingOptions
				{
					OnChanged = setting =>
					{
						Debug.Log($"Checkbox checked:{(bool) setting.Value}");
					}
				}
			),
			new
			(
				"Cheats", "Example", "Button", null,
				new PlayerSettingOptions
				{
					OnChanged = _ =>
					{
						Debug.Log($"Button clicked");
					}
				}
			),
		});
	}
}
