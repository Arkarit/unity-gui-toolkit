/// <summary>
/// Originally taken from "UnityCalender"
/// https://github.com/n-sundara-pandian/UnityCalender
/// MIT License
/// </summary>
/// 
using System;
using UnityEngine;

namespace GuiToolkit
{
	public class DateTimePicker : MonoBehaviour
	{
		[SerializeField] protected CalendarPicker m_datePicker;
		[SerializeField] protected TimePicker m_timePicker;

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

		public void SetSelectedDateTime( DateTime value )
		{
			m_datePicker.SetSelectedDate(value);
			m_timePicker.SetSelectedTime(value);
		}
	}
}