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
	public class TimePicker : MonoBehaviour
	{
		public HourOptionData HourPicker;
		public MinuteSecondOptionData MinutePicker;
		public MinuteSecondOptionData SecondPicker;
		// Start is called before the first frame update

		public DateTime SelectedTime()
		{
			return new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day,
								HourPicker.SelectedValue, MinutePicker.SelectedValue, SecondPicker.SelectedValue);
		}

		public void SetSelectedTime( int hour, int minute, int second )
		{
			HourPicker.SelectedValue = hour;
			MinutePicker.SelectedValue = minute;
			SecondPicker.SelectedValue = second;
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