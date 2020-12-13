using System;
using UnityEngine;

namespace GuiToolkit
{
	[Flags]
	public enum EAxis2DFlags
	{
		Horizontal	= 01,
		Vertical	= 02,
	}

	public enum EAxis2D
	{
		Horizontal,
		Vertical,
	}

	public enum ESide2D
	{
		Top,
		Bottom,
		Left,
		Right
	}

	// Note: the lower the layer definition number, the higher (more occluding)
	// it is regarding the visibility
	public enum EUiLayerDefinition
	{
		Top = 200,
		Tooltip = 400,
		ModalStack = 600,
		Popup = 800,
		Dialog = 1000,
		Hud = 1200,
		Background = 1400,
		Back = 1600,
	}

	public enum EDefaultSceneVisibility
	{
		DontCare,
		Visible,
		Invisible,
		VisibleInDevBuild,
		VisibleWhen_DEFAULT_SCENE_VISIBLE_defined,
	}

	public enum EStackAnimationType
	{
		None,
		LeftToRight,
		RightToLeft,
		TopToBottom,
		BottomToTop,
	}

	public interface ISetDefaultSceneVisibility
	{
		void SetDefaultSceneVisibility();
	}

	public interface IExcludeFromFrustumCulling
	{
		Mesh GetMesh();
	}

	public static class Constants
	{
		public const int INVALID = -1;

		public const float HANDLE_SIZE = 0.06f;
		public static Color HANDLE_COLOR = Color.yellow;
		public static Color HANDLE_SUPPORTING_COLOR = Color.yellow * 0.5f;
		public static Color HANDLE_CAGE_LINE_COLOR = Color.yellow * 0.5f;

		public const int SETTINGS_MENU_PRIORITY = -1;
		public const int GAME_SPEED_MENU_PRIORITY = 500;
		public const int LOCA_PROCESSOR_MENU_PRIORITY = 100;
		public const int LOCA_PLURAL_PROCESSOR_MENU_PRIORITY = 110;
	}

	public static class StringConstants
	{
		public const string TOOLKIT_NAME = "UI Toolkit";
		public const string MENU_HEADER = TOOLKIT_NAME + "/";

		public const string SETTINGS_MENU_NAME = MENU_HEADER + "Settings";
		public const string GAME_SPEED_MENU_NAME = MENU_HEADER + "Game speed";
		public const string LOCA_PROCESSOR_MENU_NAME = MENU_HEADER + "Process Loca (Update pot file)";
		public const string LOCA_PLURAL_PROCESSOR_MENU_NAME = MENU_HEADER + "Process Loca (Update plurals when added a new language)";
	}
}