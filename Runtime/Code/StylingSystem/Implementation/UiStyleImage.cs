using System;
using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit.Style
{
	[Serializable]
	public class UiStyleImage : UiAbstractStyle<Image>
	{
		[SerializeField] private ApplicableValue<Color> m_color = new();

		public Color Color
		{
			get => m_color.Value;
			set => m_color.Value = value;
		}
	}
}