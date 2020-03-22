using System;
using UnityEngine;

namespace GuiToolkit
{
	[Flags]
	public enum EDirectionFlags
	{
		Horizontal	= 01,
		Vertical	= 02,
	}

	public enum EDirection
	{
		Horizontal,
		Vertical,
	}

	public enum ESide
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

	public interface ISetDefaultSceneVisibility
	{
		void SetDefaultSceneVisibility();
	}

	public static class Constants
	{
		public const int INVALID = -1;

		public const float HANDLE_SIZE = 0.06f;
		public static Color HANDLE_COLOR = Color.yellow;
		public static Color HANDLE_SUPPORTING_COLOR = Color.yellow * 0.5f;
		public static Color HANDLE_CAGE_LINE_COLOR = Color.yellow * 0.5f;
	}

}