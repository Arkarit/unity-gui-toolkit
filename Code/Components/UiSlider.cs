using System;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.UI.Slider;

namespace GuiToolkit
{
	public class UiSlider : UiThing
	{
		[SerializeField] protected UiToggle m_optionalOnOffToggle;
		[SerializeField] protected UiButton m_optionalFullVolumeButton;
		[SerializeField] protected Slider m_slider;
		[SerializeField] protected UiImage[] m_uiImagesToDisable;

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

			m_slider.interactable = _value;
			if (m_optionalFullVolumeButton != null)
				m_optionalFullVolumeButton.Enabled = _value;

			if (m_uiImagesToDisable != null)
			{
				foreach (var uiImage in m_uiImagesToDisable)
					uiImage.Enabled = _value;
			}

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