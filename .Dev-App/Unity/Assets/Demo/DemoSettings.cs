using GuiToolkit;
using System.Collections.Generic;
using GuiToolkit.Style;
using UnityEngine;

public static class DemoSettings
{
	private const string KeyUiSkin = "UiSkin";

	public static void Create()
	{
		PlayerSettings.Instance.Add( new List<PlayerSetting>
		{
			// Language
			new PlayerSetting
			(
				__("General"), __("Language"), "", "en_us", 
				new PlayerSettingOptions
				{
					Type = EPlayerSettingType.Language,
					Key = LocaManager.PLAYER_PREFS_KEY,
					Titles = new List<string> {"Dev", "English", "Deutsch", "русский", "Lolspeak"},
					StringValues = new List<string> {"dev", "en_us", "de", "ru", "lol" },
					IsLocalized = false,
				}
			),

			// Sound
			new PlayerSetting(__("General"), __("Sound"), __("Music Volume"), .5f),
 			new PlayerSetting
			(
				__("General"), __("Sound"), __("Effect Volume"), .5f, 
				new PlayerSettingOptions
				{
					Icons = new List<string> {BuiltinIcons.LOUDSPEAKER}
				}
			),

			// Graphics
			new PlayerSettingQuality(),
			new PlayerSettingFPS(),

			// Graphics details
			new PlayerSetting(__("Graphics"), __("Details"), __("Ambient Occlusion"), true),
			new PlayerSetting
			(
				__("Graphics"), __("Details"), __("Animation Speed"), .5f,
				new PlayerSettingOptions
				{
					Icons = new List<string>{ BuiltinIcons.SLOW, BuiltinIcons.FAST }
				}
			),
			new PlayerSetting
			(
				__("Graphics"), __("UI Skin"), "", __("Default"), 
				new PlayerSettingOptions()
				{
					Type = EPlayerSettingType.Radio,
					Key = KeyUiSkin,
					StringValues = new List<string>{__("Default"), __("Light")},
					OnChanged = playerSetting =>
					{
						var val = playerSetting.GetValue<string>();
						UiMainStyleConfig.Instance.CurrentSkinName = val;
					}
				}
			),

			// Key bindings, all keys allowed except Esc
			new PlayerSetting(__("Key Bindings"), "", __("Move Up"), KeyCode.W),
			new PlayerSetting(__("Key Bindings"), "", __("Move Left"), KeyCode.A),
			new PlayerSetting(__("Key Bindings"), "", __("Move Right"), KeyCode.S),
			new PlayerSetting(__("Key Bindings"), "", __("Move Down"), KeyCode.D),
			// An example for forbidden mouse keys
			new PlayerSetting(__("Key Bindings"), "", __("No Mouse Keys"), KeyCode.Space, PlayerSettingOptions.NoMouseKeys),
			new PlayerSetting(__("Key Bindings"), "", __("Only Mouse Keys"), KeyCode.Mouse1, PlayerSettingOptions.OnlyMouseKeys),
			// An example for specified keys
			new PlayerSetting(__("Key Bindings"), "", __("Only specified Keys allowed"), KeyCode.E, new PlayerSettingOptions()
			{
				KeyCodeFilterList = new ()
				{
					KeyCode.E,
					KeyCode.F,
					KeyCode.G,
					KeyCode.Mouse0
				},
				KeyCodeFilterListIsWhitelist = true
			}),
			new PlayerSetting(__("Key Bindings"), "", __("Only specified Keys forbidden"), KeyCode.Y, new PlayerSettingOptions()
			{
				KeyCodeFilterList = new ()
				{
					KeyCode.X,
					KeyCode.Y,
					KeyCode.Z,
					KeyCode.Mouse1
				}
			}),
		});

	}
	
	/// Not translated, only for POT creation
	private static string __(string _s)
	{
		return _s;
	}
}
