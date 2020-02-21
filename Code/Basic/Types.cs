using System;
using UnityEngine;

namespace GuiToolkit
{
	[Flags]
	public enum EDirection
	{
		Horizontal	= 01,
		Vertical	= 02,
	}

	[Flags]
	public enum ESide
	{
		Top,
		Bottom,
		Left,
		Right
	}

//	public static class CSharpNeedsAClassForEveryFuckingBullshit
	public static class Constants
	{
		public const float HANDLE_SIZE = 0.08f;
		public static Color HANDLE_COLOR = Color.yellow;
		public static Color HANDLE_SUPPORTING_COLOR = Color.yellow * 0.5f;
		public static Color HANDLE_CAGE_LINE_COLOR = Color.yellow * 0.5f;
	}

}