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

		protected override void OnEnable()
		{
			base.OnEnable();
			m_slider.OnValueChanged.AddListener(OnSliderValueChanged);
		}

		protected override void OnDisable()
		{
			m_slider.OnValueChanged.RemoveListener(OnSliderValueChanged);
			base.OnDisable();
		}

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

		private void OnSliderValueChanged( float _value )
		{
			base.Value = _value;
		}

		public override void ApplyIcons(List<string> _assetPaths)
		{
			if (_assetPaths.Count == 1)
			{
				_assetPaths = _assetPaths.Clone();
				_assetPaths.Add(_assetPaths[0]);
				m_slider.FirstIconSmall = true;
			}
			else
				m_slider.FirstIconSmall = false;

			m_slider.Icons = _assetPaths;
		}
	}
}