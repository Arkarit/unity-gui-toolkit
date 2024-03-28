using System;
using UnityEngine;

/// \file Types.cs
/// \brief All common Types of the toolkit.
/// 
/// In this file, all common and basic type definitions of the toolkit are collected.
namespace GuiToolkit
{

	/// \brief Axis flags definition (for routines, which support multiple axis
	[Flags]
	public enum EAxis2DFlags
	{
		Horizontal	= 01,
		Vertical	= 02,
	}

	/// \brief Axis enum
	public enum EAxis2D
	{
		Horizontal,
		Vertical,
	}

	/// \brief Side enum
	public enum ESide2D
	{
		Top,
		Bottom,
		Left,
		Right
	}

	/// \brief Screen orientation enum.
	/// Screen orientation landscape or portrait.
	public enum EScreenOrientation
	{
		Invalid = -1,
		Landscape,
		Portrait,

		Count
	}

	/// \brief Layer definition
	/// 
	/// The UI Toolkit makes use of layers to order UI elements visually.<BR>
	/// E.g. a tooltip occludes a modal dialog occludes a Hud element occludes the background image.<BR>
	/// The lower the layer definition number, the higher (more occluding)<BR>
	/// it is regarding the visibility
	public enum EUiLayerDefinition
	{
		Top = 200,			///< Topmost layer
		Tooltip = 400,		///< Use this for tool tips
		ModalStack = 600,	///< All modal dialogs
		Popup = 800,		///< Popup dialogs
		Dialog = 1000,		///< Common dialogs
		Hud = 1200,			///< HUD
		Background = 1400,	///< Background (e.g. background image)
		Back = 1600,		///< Bottommost layer
	}

	/// \brief Visibilities of elements when scene is opened
	public enum EDefaultSceneVisibility
	{
		DontCare,									 ///< Don't care. Determined by Unity active flag.
		Visible,									 ///< Visible
		Invisible,									 ///< Invisible
		VisibleInDevBuild,							 ///< Only visible in dev build
		VisibleWhen_DEFAULT_SCENE_VISIBLE_defined,	 ///< Only visible if DEFAULT_SCENE_VISIBLE is defined
	}

	/// \brief Stack animation type enum
	public enum EStackAnimationType
	{
		None,
		LeftToRight,
		RightToLeft,
		TopToBottom,
		BottomToTop,
	}

	/// \brief General tri-state enum
	public enum ETriState
	{
		False,
		True,
		Indeterminate,
	}

	/// \brief Implement this to set visibility when loaded in a scene
	/// \sa EDefaultSceneVisibility
	/// \sa UiPanel
	public interface ISetDefaultSceneVisibility
	{
		void SetDefaultSceneVisibility();
	}

	/// \brief Implement this to be excluded from frustum culling
	/// \sa UiMain.RegisterExcludeFrustumCulling()
	/// \sa UiMain.UnregisterExcludeFrustumCulling()
	public interface IExcludeFromFrustumCulling
	{
		Mesh GetMesh();
	}

	/// \brief General constants
	public static class Constants
	{
		public const int INVALID = -1;												  ///< General "Invalid" value definition
																					  
		public const float HANDLE_SIZE = 0.06f;										  ///< Size for handles
		public static Color HANDLE_COLOR = Color.yellow;							  ///< Handle color
		public static Color HANDLE_SUPPORTING_COLOR = Color.yellow * 0.5f;			  ///< Handles "2nd order"
		public static Color HANDLE_CAGE_LINE_COLOR = Color.yellow * 0.5f;			  ///< Handle cage color
																					  
		public const int SETTINGS_MENU_PRIORITY = -1;								  ///< Menu priority "Settings"
		public const int GAME_SPEED_MENU_PRIORITY = 500;							  ///< Menu priority "Game speed"
		public const int KERNING_TABLE_TOOL_MENU_PRIORITY = 510;					  ///< Menu priority "Clean Kerning Table"
		public const int LOCA_PROCESSOR_MENU_PRIORITY = 100;						  ///< Menu priority "Process Loca"
		public const int LOCA_PLURAL_PROCESSOR_MENU_PRIORITY = 110;					  ///< Menu priority "Update plurals"
	}

	/// \brief General string constants
	public static class StringConstants
	{
		public const string TOOLKIT_NAME = "UI Toolkit";																				///< General Toolkit name
		public const string MENU_HEADER = TOOLKIT_NAME + "/";																			///< Menu header "folder" definition

		public const string CONFIGURATION_NAME = "Ui Toolkit Configuration";															///< Friendly name for configuration
		public const string CONFIGURATION_MENU_NAME = MENU_HEADER + CONFIGURATION_NAME;													///< Configuration menu entry
		public const string GAME_SPEED_MENU_NAME = MENU_HEADER + "Game speed";															///< "Game Speed" menu entry
		public const string KERNING_TABLE_TOOL_MENU_NAME = MENU_HEADER + "Kerning Table Tool";											///< "Kerning Table Tool" menu entry
		public const string LOCA_PROCESSOR_MENU_NAME = MENU_HEADER + "Process Loca (Update pot file)";									///< "Process Loca" menu entry
		public const string LOCA_PLURAL_PROCESSOR_MENU_NAME = MENU_HEADER + "Process Loca (Update plurals when added a new language)";	///< "Process Loca plurals" menu entry 

		public const string PLAYER_PREFS_PREFIX = "UiToolkit_";																			///< Prefix for all player prefs entries
	}

	/// \brief Builtin icon definitions
	/// 
	/// The icons are a flat white version, which can be colored by gradients etc;<BR>
	/// The style is very basic, thus it can be suitable for a very wide range of projects.
	public static class BuiltinIcons
	{
		public const string CHECKMARK		= "Icons/UITK_Checkmark";
		public const string FORBIDDEN		= "Icons/UITK_Forbidden";
		public const string GEAR			= "Icons/UITK_Gear";
		public const string LOUDSPEAKER		= "Icons/UITK_Loudspeaker";
		public const string NOTE			= "Icons/UITK_Note";
		public const string RANDOM_DICE		= "Icons/UITK_RandomDice";
		public const string SEND			= "Icons/UITK_Send";
		public const string TARGET			= "Icons/UITK_Target";
		public const string X				= "Icons/UITK_X";
		public const string FAST			= "Icons/UITK_Fast";
		public const string SLOW			= "Icons/UITK_Slow";
		public const string INFO			= "Icons/UITK_Info";
		public const string PHONE			= "Icons/UITK_Phone";
		public const string SEARCH			= "Icons/UITK_Search";
		public const string SHARE			= "Icons/UITK_Share";
		public const string TEXT			= "Icons/UITK_Text";

	}

	/// \brief Builtin logos
	/// 
	/// Colored logos of various companies and institutions
	public static class BuiltinLogos
	{
		public const string ZOOM		= "Icons/UITK_Logo_Zoom";
	}
}