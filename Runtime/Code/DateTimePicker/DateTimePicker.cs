using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace GuiToolkit
{
	public class DateTimePicker : UiView
	{
		[SerializeField] private DatePicker m_datePicker;
		[SerializeField] private TimePicker m_timePicker;


		public DateTime? SelectedDateTime()
		{
			var d = m_datePicker.SelectedDate;
			var t = m_timePicker.SelectedTime();
			if (d != null)
			{
				return new DateTime(d.Value.Year, d.Value.Month, d.Value.Day,
					t.Hour, t.Minute, t.Second);
			}

			return null;
		}

		public void SetSelectedDateTime(DateTime _value)
		{
			m_datePicker.SetSelectedDate(_value);
			m_timePicker.SetSelectedTime(_value);
		}
	}
}