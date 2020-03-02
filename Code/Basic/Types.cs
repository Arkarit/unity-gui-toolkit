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

	public enum ESide
	{
		Top,
		Bottom,
		Left,
		Right
	}

	public enum EUiLayerDefinition
	{
		Top = 20,
		Tooltip = 50,
		Popup = 80,
		Dialog = 130,
		Background = 180,
		Back = 200,
	}

	public static class Constants
	{
		public const float HANDLE_SIZE = 0.06f;
		public static Color HANDLE_COLOR = Color.yellow;
		public static Color HANDLE_SUPPORTING_COLOR = Color.yellow * 0.5f;
		public static Color HANDLE_CAGE_LINE_COLOR = Color.yellow * 0.5f;
	}

}