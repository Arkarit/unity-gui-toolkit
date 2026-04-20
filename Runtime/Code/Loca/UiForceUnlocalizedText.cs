using TMPro;
using UnityEngine;

namespace GuiToolkit
{
	/// <summary>
	/// Marker component that prevents the <see cref="TextMeshProUGUI"/> component on the same
	/// GameObject from being converted to <see cref="UiLocalizedTextMeshProUGUI"/> by batch
	/// conversion routines (both Dry Run and Execute) in <c>LocalizedTmpConverterWindow</c>.
	/// <para>
	/// This component is automatically removed when you convert via the component context menu
	/// ("Replace with Localized Text"). When converting <see cref="UiLocalizedTextMeshProUGUI"/>
	/// back to <see cref="TextMeshProUGUI"/> via "Replace with Plain TMP Text", you are asked
	/// whether to add this component (default: yes).
	/// </para>
	/// </summary>
	[AddComponentMenu("UI/Force Unlocalized Text (No Auto-Convert)")]
	[RequireComponent(typeof(TextMeshProUGUI))]
	public class UiForceUnlocalizedText : MonoBehaviour { }
}
