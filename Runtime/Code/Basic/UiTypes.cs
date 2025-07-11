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
		None = 0,
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

	/// \brief Layer definition
	/// 
	/// The UI Toolkit makes use of layers to order UI elements visually.<BR>
	/// E.g. a tooltip occludes a modal dialog occludes a Hud element occludes the background image.<BR>
	/// The lower the layer definition number, the higher (more occluding)<BR>
	/// it is regarding the visibility
	public enum EUiLayerDefinition
	{
		Top = 200,				///< Topmost layer
		Tooltip = Top * 2,		///< Use this for tool tips
		ModalStack = Top * 3,	///< All modal dialogs
		Popup = Top * 4,		///< Popup dialogs
		Dialog = Top * 5,		///< Common dialogs
		Hud = Top * 6,			///< HUD
		Background = Top * 7,	///< Background (e.g. background image)
		Back = Top * 8,			///< Bottommost layer
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

	/// \brief Visibilities of elements when scene is opened
	public enum EDefaultSceneVisibility
	{
		DontCare,										///< Don't care. Determined by Unity active flag.
		Visible,										///< Visible
		Invisible,										///< Invisible
		Legacy,											///< from external project, unused in toolkit itself
		VisibleInDevBuild,								///< Only visible in dev build
		VisibleWhen_DEFAULT_SCENE_VISIBLE_defined,		///< Only visible if DEFAULT_SCENE_VISIBLE is defined
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

	/// \brief Panel animation types
	public enum EPanelAnimationType
	{
		Instant,
		Animated,
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
		public const int INVALID = -1;												///< General "Invalid" value definition
																					  
		public const float HANDLE_SIZE = 0.08f;										///< Size for handles
		public static Color HANDLE_COLOR = Color.yellow;							///< Handle color
		public static Color HANDLE_SUPPORTING_COLOR = Color.yellow * 0.5f;		///< Handles "2nd order"
		public static Color HANDLE_CAGE_LINE_COLOR = Color.yellow * 0.5f;			///< Handle cage color

		public const int SETTINGS_MENU_PRIORITY = -1;
		public const int MISC_MENU_PRIORITY = 1000;
		public const int GAME_SPEED_MENU_PRIORITY = 500;
		public const int KERNING_TABLE_TOOL_MENU_PRIORITY = 510;
		public const int LOCA_PROCESSOR_MENU_PRIORITY = 100;
		public const int LOCA_PLURAL_PROCESSOR_MENU_PRIORITY = 110;
		public const int LOCA_PO_FIXER_MENU_PRIORITY = 120;

		public const int CONFIG_MANAGER_MENU_PRIORITY = 0;
		public const int SAVE_PROJECT_ON_LOSE_FOCUS_MENU_PRIORITY = 1000;
		public const int SCENE_CHANGE_TRACKER_MENU_PRIORITY = 1010;
	}

	/// \brief General string constants
	public static class StringConstants
	{
		public const string TOOLKIT_NAME = "Gui Toolkit";																				///< General Toolkit name
		public const string MENU_HEADER = TOOLKIT_NAME + "/";																			///< Menu header "folder" definition
		public const string STYLES_HEADER = MENU_HEADER + "Styles/";
		public const string MISC_TOOLS_MENU_HEADER = MENU_HEADER + "Miscellaneous Tools/";											    ///< Menu header for miscellaneous tools

		public const string CONFIGURATION_NAME = "Ui Toolkit Configuration...";
		public const string CONFIGURATION_MENU_NAME = MENU_HEADER + CONFIGURATION_NAME;
		public const string APPLY_STYLE_GENERATOR_MENU_NAME = STYLES_HEADER + "'Ui Apply Style' Generator...";
		public const string GAME_SPEED_MENU_NAME = MENU_HEADER + "Game speed...";
		public const string KERNING_TABLE_TOOL_MENU_NAME = MENU_HEADER + "Kerning Table Tool...";
		public const string LOCA_PROCESSOR_MENU_NAME = MENU_HEADER + "Process Loca (Update pot file)";
		public const string LOCA_PLURAL_PROCESSOR_MENU_NAME = MENU_HEADER + "Process Loca (Update plurals when added a new language)";
		public const string LOCA_PO_FIXER_MENU_NAME = MENU_HEADER + "Loca PO File Fixer (Sync .po <-> .po.txt files)";
		public const string SCENE_CHANGE_TRACKER_MENU_NAME = MISC_TOOLS_MENU_HEADER + "Scene Change Tracker";
		public const string SAVE_PROJECT_ON_LOSE_FOCUS_MENU_NAME = MISC_TOOLS_MENU_HEADER + "Save Project on lose focus";
		public const string CREATE_MAIN_STYLE_CONFIG = STYLES_HEADER + "Main Style Config";
		public const string CREATE_ORIENTATION_DEPENDENT_STYLE_CONFIG = STYLES_HEADER + "Orientation Dependent Style Config";
		public const string CREATE_STYLE_CONFIG = STYLES_HEADER + "Style Config";
		public const string CREATE_DOXYGEN_CONFIG = MENU_HEADER + "Doxygen Config";

		public const string PLAYER_PREFS_PREFIX = "UiToolkit_";																			///< Prefix for all player prefs entries
		public const string CREATE_TEST_DATA = MENU_HEADER + "Test Data";

		public const string CONFIG_MANAGER_MENU_NAME = MISC_TOOLS_MENU_HEADER + "Config Manager...";
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
		public const string INFINITY		= "Icons/UITK_Infinity";
		public const string INVISIBLE		= "Icons/UITK_Invisible";
	}
}