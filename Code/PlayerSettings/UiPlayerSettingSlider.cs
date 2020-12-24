using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit
{
	public class UiPlayerSettingSlider : UiPlayerSettingBase
	{
		[SerializeField] protected UiSlider m_slider;

		public float Value
		{
			get => m_slider.Value;
			set => m_slider.Value = value;
		}
	}
}