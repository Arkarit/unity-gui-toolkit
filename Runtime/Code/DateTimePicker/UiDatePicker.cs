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

		public CEvent<DateTime> OnValueChanged = new();

		public DateTime SelectedDate
		{
			get => new DateTime(m_yearPicker.Year, m_monthPicker.Month, m_dayPicker.Day, 0,0,0);
			set
			{
Debug.Log($"---::: {value}");
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
			var dateTime = new DateTime(m_yearPicker.Year, m_monthPicker.Month, m_dayPicker.Day, 0,0,0);
			OnValueChanged.InvokeOnce(dateTime);
		}
	}
}