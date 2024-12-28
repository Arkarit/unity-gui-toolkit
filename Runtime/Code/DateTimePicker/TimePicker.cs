using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace GuiToolkit
{
	public class TimePicker : UiThing
	{
		[SerializeField] private UiDateTimePartPanel m_hourPicker;
		[SerializeField] private UiDateTimePartPanel m_minutePicker;
		[SerializeField] private UiDateTimePartPanel m_secondPicker;

		public DateTime SelectedTime()
		{
			return new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day,
				m_hourPicker.Value, m_minutePicker.Value, m_secondPicker.Value);
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


		public void Now_onClick()
		{
			SetSelectedTime(DateTime.Now);
		}
	}
}