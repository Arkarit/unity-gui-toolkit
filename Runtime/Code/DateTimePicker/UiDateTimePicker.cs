using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace GuiToolkit
{
	public class UiDateTimePicker : UiView
	{
		[SerializeField] private UiButton m_nowButton;
		[SerializeField] private UiDatePicker m_datePicker;
		[SerializeField] private UiTimePicker m_timePicker;

		public CEvent<DateTime> OnValueChanged = new();

		protected override void Awake()
		{
			AddOnEnableButtonListeners((m_nowButton, OnNowButton));
			base.Awake();
		}

		protected override void OnEnable()
		{
			base.OnEnable();
			m_datePicker.OnValueChanged.AddListener(OnDateTimeChanged);
			m_timePicker.OnValueChanged.AddListener(OnDateTimeChanged);
		}

		protected override void OnDisable()
		{
			base.OnDisable();
			m_datePicker.OnValueChanged.RemoveListener(OnDateTimeChanged);
			m_timePicker.OnValueChanged.RemoveListener(OnDateTimeChanged);
		}

		private void OnDateTimeChanged(DateTime _value)
		{
Debug.Log($"---::: OnDateTimeChanged");
			SelectedDateTime = _value;
			OnValueChanged.InvokeOnce(_value);
		}

		private void OnNowButton() => SelectedDateTime = DateTime.Now;

		public DateTime SelectedDateTime
		{
			get
			{
				var d = m_datePicker.SelectedDate;
				var t = m_timePicker.SelectedTime;
				return new DateTime(d.Year, d.Month, d.Day,
					t.Hour, t.Minute, t.Second);
			}
			set
			{
				m_datePicker.SelectedDate = value;
				m_timePicker.SelectedTime = value;
			}
		}
	}
}