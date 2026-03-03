using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace GuiToolkit
{
	public class UiTimePicker : UiThing
	{
		[SerializeField] private UiDateTimePartPanel m_hourPicker;
		[SerializeField] private UiDateTimePartPanel m_minutePicker;
		[SerializeField] private UiDateTimePartPanel m_secondPicker;
		[SerializeField] private UiButton m_optionalNowButton;

		public CEvent<DateTime> OnValueChanged = new();

		protected override void OnEnable()
		{
			base.OnEnable();
			if (m_optionalNowButton)
				m_optionalNowButton.OnClick.AddListener(OnNowClicked);
			m_hourPicker.OnValueChanged.AddListener(OnPartChanged);
			m_minutePicker.OnValueChanged.AddListener(OnPartChanged);
			m_secondPicker.OnValueChanged.AddListener(OnPartChanged);
		}

		protected override void OnDisable()
		{
			base.OnDisable();
			if (m_optionalNowButton)
				m_optionalNowButton.OnClick.RemoveListener(OnNowClicked);
			m_hourPicker.OnValueChanged.RemoveListener(OnPartChanged);
			m_minutePicker.OnValueChanged.RemoveListener(OnPartChanged);
			m_secondPicker.OnValueChanged.RemoveListener(OnPartChanged);
		}

		private void OnPartChanged(int _) => OnValueChanged.InvokeOnce(SelectedTime);

		public DateTime SelectedTime
		{
			get
			{
				return new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day,
					m_hourPicker.Value, m_minutePicker.Value, m_secondPicker.Value);
			}

			set => SetSelectedTime(value);
		}

		public void SetSelectedTime(int _hour, int _minute, int _second)
		{
			m_hourPicker.Value = _hour;
			m_minutePicker.Value = _minute;
			m_secondPicker.Value = _second;
		}

		public void SetSelectedTime(DateTime _value)
		{
			SetSelectedTime(_value.Hour, _value.Minute, _value.Second);
		}

		public void OnNowClicked()
		{
			SetSelectedTime(DateTime.Now);
		}
	}
}