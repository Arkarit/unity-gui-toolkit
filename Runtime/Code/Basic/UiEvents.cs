/// \file UiEvents.cs
/// \brief Definition for common events for the toolkit
/// 
/// This file contains all definitions for common events, like "Language changed", "Orientation changed" etc.

using GuiToolkit.Style;

namespace GuiToolkit
{
	/// \brief Definition for common events for the toolkit
	/// 
	/// All definitions for common events, like "Language changed", "Orientation changed" etc.<BR>
	/// Attach yourself to the events if you need to be informed.
	/// As this class is defined as partial, you can add game specific entries at another place if you like.
	public static partial class UiEvents
	{
		/// \brief Event invoked on language change.
		/// <param name="string">Language token, e.g. "de"</param>
		/// Note that this event is also spawned once on startup.
		public static CEvent<string>									EvLanguageChanged				= new();	

		/// \brief Invoked if a player setting has changed.
		/// <param name="PlayerSetting">Changed player setting class instance</param>
		public static CEvent<PlayerSetting>								EvPlayerSettingChanged			= new();

		/// \brief Invoked if the screen orientation has changed
		/// <param name="EScreenOrientation 0">Screen orientation before change</param>
		/// <param name="EScreenOrientation 1">Screen orientation after change</param>
		public static CEvent<EScreenOrientation,EScreenOrientation>		EvScreenOrientationChange		= new();

		/// \brief Invoked if skin has changed
		public static CEvent<string>									EvSkinRemoved					= new();
		public static CEvent<string>									EvSkinAdded						= new();
		public static CEvent<string>									EvSkinChanged					= new();
	}
}