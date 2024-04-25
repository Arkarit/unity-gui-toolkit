using System;
using System.Collections.Generic;
using UnityEngine;

namespace GuiToolkit.Style
{
	[Serializable]
	public class UiSkin
	{
		[SerializeField] private string m_name;
		[SerializeField] [SerializeReference] private List<UiAbstractStyle> m_styles = new();

		public string Name => m_name;
		public List<UiAbstractStyle> Styles => m_styles;
	}
}