using GuiToolkit.Style;
using UnityEngine;

namespace GuiToolkit
{
	// General event definitions. See counterpart for game specific event definitions.
	public static class UiEventDefinitions
	{
		/// \brief Event invoked on language change.
		/// <param name="string">Language token, e.g. "de"</param>
		/// Note that this event is also spawned once on startup.
		public static CEvent<string>									EvLanguageChanged				= new();
		
		// string: log message
		public static readonly CEvent<string> LogMessage = new ();

		public static readonly CEvent DebugEvent00 = new();
		
		// Vector2Int: previous resolution
		// Vector2Int: current resolution
		public static readonly CEvent<Vector2Int, Vector2Int> OnResolutionChanged = new();
		
		/// \brief Invoked if the screen orientation has changed
		/// <param name="EScreenOrientation 0">Screen orientation before change</param>
		/// <param name="EScreenOrientation 1">Screen orientation after change</param>
		public static CEvent<EScreenOrientation,EScreenOrientation>		EvScreenOrientationChange = new();

		/// \brief Invoked if a player setting has changed.
		/// <param name="PlayerSetting">Changed player setting class instance</param>
		public static CEvent<PlayerSetting>								EvPlayerSettingChanged			= new();

		/// \brief Invoked if skin changes
		/// float: duration
		public static CEvent<float>										EvSkinChanged = new();
		/// \brief Invoked if skin values have changed - the skin itself stays the same
		/// float: normalized amount of change
		public static CEvent<float>										EvSkinValuesChanged = new();

		// Style system
		/// \brief Invoked if applicableness of style has changed.
		/// Used to synchronize styles of same name but members of different skins
		public static CEvent<UiStyleConfig, UiAbstractStyleBase> 			EvStyleApplicableChanged = new();
		public static CEvent<UiStyleConfig, UiAbstractStyleBase>			EvDeleteStyle = new();
		public static CEvent<UiStyleConfig, UiSkin>							EvAddSkin = new();
		public static CEvent<UiStyleConfig, string>							EvDeleteSkin = new();
		public static CEvent<UiStyleConfig, UiSkin, string>					EvSetSkinAlias = new();
		public static CEvent<UiStyleConfig, UiAbstractStyleBase, string>	EvSetStyleAlias = new();
		public static CEvent<UiAbstractApplyStyleBase>						EvStyleApplierCreated = new();	
		public static CEvent<UiAbstractApplyStyleBase>						EvStyleApplierChangedParent = new();	
		public static CEvent<UiAbstractApplyStyleBase>						EvStyleApplierDestroyed = new();
		

		// Timer events ---------------------------------------------------------------------
		// Not yet implemented: unscaled time. Implement/test on demand please.

		// This is called every frame. Use this if you need an update for standalone classes or disabled game objects.
		// For regular and active game objects consider using Update() instead (less overhead)
		public static readonly CEvent OnTickPerFrame = new(true);
		// events for second, 10 seconds and minute (scaled game time)
		public static readonly CEvent OnTickPerSecond = new(true);
		public static readonly CEvent OnTickPerMinute = new(true);
	}
}
