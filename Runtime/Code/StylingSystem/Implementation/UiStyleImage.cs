using System;
using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit.Style
{
	[Serializable]
	public class UiStyleImage : UiAbstractStyle<Image>
	{
		[SerializeField] private Color m_color;

		public Color Color
		{
			get => m_color;
			set => m_color = value;
		}
	}
}