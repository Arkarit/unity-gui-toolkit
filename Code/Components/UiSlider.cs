using System;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.UI.Slider;

namespace GuiToolkit
{
	public class UiSlider : UiThing
	{
		public UiToggle m_optionalOnOffToggle;
		public UiButton m_optionalFullVolumeButton;
		public Slider m_slider;

		private float m_savedSliderVal;

		public SliderEvent OnValueChanged
		{
			get => m_slider.onValueChanged;
			set => m_slider.onValueChanged = value;
		}

		protected override void OnEnable()
		{
			base.OnEnable();

			m_savedSliderVal = m_slider.value;

			if (m_optionalOnOffToggle != null)
				m_optionalOnOffToggle.OnValueChanged.AddListener(OnOnOffValueChanged);
			if (m_optionalFullVolumeButton != null)
				m_optionalFullVolumeButton.OnClick.AddListener(OnFullVolumeClick);
		}

		protected override void OnDisable()
		{
			if (m_optionalOnOffToggle != null)
				m_optionalOnOffToggle.OnValueChanged.RemoveListener(OnOnOffValueChanged);
			if (m_optionalFullVolumeButton != null)
				m_optionalFullVolumeButton.OnClick.RemoveListener(OnFullVolumeClick);
			base.OnDisable();
		}

		protected virtual void OnOnOffValueChanged( bool _value )
		{
			// The on/off toggle is inverted - when it's on, the slider is off and vice versa
			_value = !_value;

			if (_value)
			{
				m_slider.value = m_savedSliderVal;
				return;
			}

			m_savedSliderVal = m_slider.value;
			m_slider.value = 0;
		}

		protected virtual void OnFullVolumeClick()
		{
			m_slider.value = 1;
		}

	}
}