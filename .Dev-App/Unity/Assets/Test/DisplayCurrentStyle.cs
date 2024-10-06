using GuiToolkit.Style;
using TMPro;
using UnityEngine;

public class DisplayCurrentStyle : MonoBehaviour
{
	public UiAbstractApplyStyleBase m_StyleApplier;
	public TMP_Text m_Text;

	protected void Start()
	{
		var style = m_StyleApplier.Style;
		if (style == null)
		{
			m_Text.text = "Style: <none>";
			return;
		}

		m_Text.text = $"Style:\n'{m_StyleApplier.Style.Alias}'";
	}
}
