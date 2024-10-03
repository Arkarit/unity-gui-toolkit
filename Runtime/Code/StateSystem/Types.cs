using System;

namespace GuiToolkit.UiStateSystem
{
	[Flags]
	public enum EStatePropertySupport : long
	{
		SizePosition			= 0x00000001,
		Active					= 0x00000002,
		Rotation				= 0x00000004,
		Scale					= 0x00000008,
		Alpha					= 0x00000010,
		PreferredWidth			= 0x00000020,
		PreferredHeight			= 0x00000040,
		Interactable			= 0x00000080,

		All						= 0xffffffff,
		None					= 0x00000000,
	}

}
