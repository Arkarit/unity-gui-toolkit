using System;
using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit.Style
{
	[Serializable]
	public class UiStyleImage : UiAbstractStyle<Image>
	{
		[SerializeField] private ApplicableValue<Color> m_color = new();
		[SerializeField] private ApplicableValue<Sprite> m_sprite = new();

		public Color Color
		{
			get => m_color.Value;
			set => m_color.Value = value;
		}

		public Sprite Sprite
		{
			get => m_sprite.Value;
			set => m_sprite.Value = value;
		}
	}
}