using System;

namespace GuiToolkit
{
	/// <summary>
	/// Marks a <see cref="UnityEngine.UI.Text"/> field so that
	/// <c>LegacyTextToLocalizedTmpConverter</c> skips it during automated conversion.
	/// Use this on fields that intentionally keep the legacy Text component.
	/// </summary>
	[AttributeUsage(AttributeTargets.Field)]
	public sealed class KeepLegacyTextAttribute : Attribute { }
}
