using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit
{
	/// <summary>
	/// Marker component that prevents the <see cref="Text"/> component on the same
	/// GameObject from being converted by <c>LegacyTextToLocalizedTmpConverter</c>
	/// (both Dry Run and Execute).
	/// <para>
	/// Attach this to any UI element that must keep its legacy <see cref="Text"/>
	/// component — e.g. components whose text is set exclusively at runtime via
	/// TMP-incompatible paths, or UI elements intentionally kept as legacy widgets.
	/// </para>
	/// </summary>
	[AddComponentMenu("UI/Force Legacy Text (No Auto-Convert)")]
	[RequireComponent(typeof(Text))]
	public class UiForceLegacyText : MonoBehaviour { }
}
