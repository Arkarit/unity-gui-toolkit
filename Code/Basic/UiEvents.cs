namespace GuiToolkit
{
	public static class UiEvents
	{
		public static CEvent<string>			OnLanguageChanged			= new CEvent<string>();
		public static CEvent<PlayerSetting>		OnPlayerSettingChanged		= new CEvent<PlayerSetting>();
	}
}