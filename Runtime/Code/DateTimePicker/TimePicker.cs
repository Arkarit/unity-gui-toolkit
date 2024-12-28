using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace GuiToolkit
{
	public class TimePicker : UiThing
	{
		[SerializeField] private TimeDatePanel m_hourPicker;
		[SerializeField] private MinuteSecondOptionData m_minutePicker;
		[SerializeField] private MinuteSecondOptionData m_secondPicker;

		public DateTime SelectedTime()
		{
			return new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day,
				m_hourPicker.SelectedValue, m_minutePicker.SelectedValue, m_secondPicker.SelectedValue);
		}

		public void SetSelectedTime(int _hour, int _minute, int _second)
		{
			m_hourPicker.SelectedValue = _hour;
			m_minutePicker.SelectedValue = _minute;
			m_secondPicker.SelectedValue = _second;
		}

		public void SetSelectedTime(DateTime _value)
		{
			SetSelectedTime(_value.Hour, _value.Minute, _value.Second);
		}


		public void Now_onClick()
		{
			SetSelectedTime(DateTime.Now);
		}
	}
}