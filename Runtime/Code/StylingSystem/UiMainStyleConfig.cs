using System.Collections.Generic;
using UnityEngine;

namespace GuiToolkit.Style
{
	public class UiMainStyleConfig : AbstractSingletonScriptableObject<UiMainStyleConfig>
	{
		[SerializeField] private List<UiSkin> m_skins;
	}
}