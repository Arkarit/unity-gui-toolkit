using System;

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
}