/// \file UiEvents.cs
/// \brief Definition for common events for the toolkit
/// 
/// This file contains all definitions for common events, like "Language changed", "Orientation changed" etc.

namespace GuiToolkit
{
	/// \brief Definition for common events for the toolkit
	/// 
	/// All definitions for common events, like "Language changed", "Orientation changed" etc.<BR>
	/// Attach yourself to the events if you need to be informed.
	public static class UiEvents
	{
		/// \brief Event invoked on language change.
		/// <param name="string">Language token, e.g. "de"</param>
		/// Note that this event is also spawned once on startup.
		public static CEvent<string>									OnLanguageChanged				= new CEvent<string>();	

		/// \brief Invoked if a player setting has changed.
		/// <param name="PlayerSetting">Changed player setting class instance</param>
		public static CEvent<PlayerSetting>								OnPlayerSettingChanged			= new CEvent<PlayerSetting>();

		/// \brief Invoked if the screen orientation has changed
		/// <param name="EScreenOrientation 0">Screen orientation before change</param>
		/// <param name="EScreenOrientation 1">Screen orientation after change</param>
		public static CEvent<EScreenOrientation,EScreenOrientation>		OnScreenOrientationChange		= new CEvent<EScreenOrientation, EScreenOrientation>();
	}
}