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

		public override object Value
		{
			get => base.Value;
			set
			{
				base.Value = value;
				float f = GetValue<float>();
				m_slider.Value = f;
			}
		}

		public override void ApplyIcon(string _assetPath, bool _isPlayerSettingIcon)
		{
			if (!_isPlayerSettingIcon)
				return;
			m_slider.Icon = _assetPath;
		}
	}
}