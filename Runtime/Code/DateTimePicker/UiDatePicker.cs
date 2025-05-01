using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GuiToolkit
{
	public class UiDatePicker : UiPanel
	{
		[SerializeField] protected UiYearPicker m_yearPicker;
		[SerializeField] protected UiMonthPicker m_monthPicker;
		[SerializeField] protected UiDayPicker m_dayPicker;
		[SerializeField] protected UiTimePicker m_optionalTimePicker;

		public CEvent<DateTime> OnValueChanged = new();

		public DateTime SelectedDate
		{
			get => GetDateTime();
			set
			{
				m_yearPicker.Year = value.Year;
				m_monthPicker.Month = value.Month;
				m_dayPicker.Day = value.Day;
				UpdateDate();
			}
		}

		protected override void OnEnable()
		{
			base.OnEnable();
			m_yearPicker.OnValueChanged.AddListener(OnDateChanged);
			m_monthPicker.OnValueChanged.AddListener(OnDateChanged);
			m_dayPicker.OnValueChanged.AddListener(OnDateChanged);
		}

		protected override void OnDisable()
		{
			base.OnDisable();
			m_yearPicker.OnValueChanged.RemoveListener(OnDateChanged);
			m_monthPicker.OnValueChanged.RemoveListener(OnDateChanged);
			m_dayPicker.OnValueChanged.RemoveListener(OnDateChanged);
		}

		private void OnDateChanged(string _, int __) => UpdateDate();

		private void UpdateDate()
		{
			m_dayPicker.ValidateDaysInMonth();
			OnValueChanged.InvokeOnce(GetDateTime());
		}

		private DateTime GetDateTime()
		{
			var hour = 0;
			var minute = 0;
			var second = 0;

			if (m_optionalTimePicker)
			{
				var time = m_optionalTimePicker.SelectedTime;

				hour = time.Hour;
				minute = time.Minute;
				second = time.Second;
			}

			return new DateTime(m_yearPicker.Year, m_monthPicker.Month, m_dayPicker.Day, hour, minute, second);
		}
	}
}