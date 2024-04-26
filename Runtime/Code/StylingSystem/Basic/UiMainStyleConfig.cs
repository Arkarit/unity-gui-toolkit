using System.Collections.Generic;
using UnityEngine;

namespace GuiToolkit.Style
{
	public class UiMainStyleConfig : AbstractSingletonScriptableObject<UiMainStyleConfig>
	{
		[SerializeField] private List<UiSkin> m_skins;

		[SerializeField] private int m_currentSkinIdx;

		public List<UiSkin> Skins => m_skins;

		public UiSkin CurrentSkin 
		{
			get
			{
				if (m_currentSkinIdx < 0 || m_currentSkinIdx >= m_skins.Count)
				{
					return null;
				}

				return m_skins[m_currentSkinIdx];
			}
		}
	}
}