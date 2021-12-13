/// <summary>
/// Originally taken from "UnityCalender"
/// https://github.com/n-sundara-pandian/UnityCalender
/// MIT License
/// </summary>
/// 
using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace GuiToolkit
{
	public class TimePicker : MonoBehaviour
	{
		[FormerlySerializedAs("HourPicker")]
		public HourOptionData m_hourPicker;
		[FormerlySerializedAs("MinutePicker")]
		public MinuteSecondOptionData m_minutePicker;
		[FormerlySerializedAs("SecondPicker")]
		public MinuteSecondOptionData m_secondPicker;

		public DateTime SelectedTime()
		{
			return new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day,
								m_hourPicker.SelectedValue, m_minutePicker.SelectedValue, m_secondPicker.SelectedValue);
		}

		public void SetSelectedTime( int hour, int minute, int second )
		{
			m_hourPicker.SelectedValue = hour;
			m_minutePicker.SelectedValue = minute;
			m_secondPicker.SelectedValue = second;
		}
		public void SetSelectedTime( DateTime value )
		{
			SetSelectedTime(value.Hour, value.Minute, value.Second);
		}


		public void Now_onClick()
		{
			SetSelectedTime(DateTime.Now);
		}
	}
}