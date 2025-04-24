using GuiToolkit;
using System.Collections.Generic;
using GuiToolkit.Style;
using UnityEngine;

public class TestMain : LocaMonoBehaviour
{
	protected const string KeyUiSkin = "UiSkin";
	
	protected void Start()
	{
		Application.targetFrameRate = 60;

		GuiToolkit.PlayerSettings.Instance.Add( new List<PlayerSetting>
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


			// Key bindings
			new PlayerSetting(__("Key Bindings"), "", __("Move Left"), KeyCode.A),
			new PlayerSetting(__("Key Bindings"), "", __("Move Right"), KeyCode.S),
			new PlayerSetting(__("Key Bindings"), "", __("Move Up"), KeyCode.W),
			new PlayerSetting(__("Key Bindings"), "", __("Move Down"), KeyCode.D)
		});

		// Alternative way to listen to player settings changed:
		UiEventDefinitions.EvPlayerSettingChanged.AddListener(OnPlayerSettingChanged);

		UiMain.Instance.LoadScene("DemoScene1");
	}

	private void OnPlayerSettingChanged(PlayerSetting _playerSetting)
	{
		Debug.Log($"Player setting '{_playerSetting.Key}' changed to {_playerSetting.Value}");
	}
}
