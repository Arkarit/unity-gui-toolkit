using System;
using UnityEngine;

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

	/// \brief General tri-state enum
	public enum ETriState
	{
		False,
		True,
		Indeterminate,
	}

	/// \brief Visibilities of elements when scene is opened
	public enum EDefaultSceneVisibility
	{
		DontCare,									///< Don't care. Visibility is left untouched
		Visible,									///< Visible
		Invisible,									///< Invisible
		Legacy										///< Uses old panel/dialog visibility
	}

	/// \brief Panel animation types
	public enum EPanelAnimationType
	{
		Instant,
		Animated,
	}

	/// \brief General constants
	public static class Constants
	{
		public const int INVALID = -1;												///< General "Invalid" value definition
																					  
		public const float HANDLE_SIZE = 0.08f;										///< Size for handles
		public static Color HANDLE_COLOR = Color.yellow;							///< Handle color
		public static Color HANDLE_SUPPORTING_COLOR = Color.yellow * 0.5f;		///< Handles "2nd order"
		public static Color HANDLE_CAGE_LINE_COLOR = Color.yellow * 0.5f;			///< Handle cage color
	}
}
