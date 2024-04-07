using GuiToolkit;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestMain : UiThing
{
	protected override void Start()
	{
		base.Start();

		Application.targetFrameRate = 60;
//PlayerPrefs.DeleteAll();
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
					Titles = new List<string> {"English", "Deutsch", "русский", "Lolspeak"},
					StringValues = new List<string> {"en_us", "de", "ru", "lol" },
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
			new PlayerSetting
			(
				__("Graphics"), __("Overall"), "", "High",
				new PlayerSettingOptions
				{
					Type = EPlayerSettingType.Radio,
					Key = "GraphicsOptionsOverall",
					StringValues = new List<string>{__("Ultra"), __("High"), __("Medium"), __("Low")}
				}
			),

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


			// Key bindings
			new PlayerSetting(__("Key Bindings"), "", __("Move Left"), KeyCode.A),
			new PlayerSetting(__("Key Bindings"), "", __("Move Right"), KeyCode.S),
			new PlayerSetting(__("Key Bindings"), "", __("Move Up"), KeyCode.W),
			new PlayerSetting(__("Key Bindings"), "", __("Move Down"), KeyCode.D)
		});

		UiMain.Instance.LoadScene("DemoScene1");
	}

}
