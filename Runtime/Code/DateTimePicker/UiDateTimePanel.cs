using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace GuiToolkit
{
	public class UiDateTimePanel : UiPanel
	{
		[Serializable]
		public class Options
		{
			public DateTime StartDateTime = DateTime.Now;
			public bool ShowCurrentDate = true;
			public bool ShowDate = true;
			public bool ShowTime = true;
			public bool LongDateFormat = true;
			public bool LongTimeFormat = true;
			public UnityAction<DateTime> OnDateTimeChanged = null;
		}

		[SerializeField] private UiButton m_nowButton;
		[FormerlySerializedAs("m_dateDisplay")] 
		[SerializeField] private UiDateTimeDisplay m_dateTimeDisplay;
		[SerializeField] private UiDatePicker m_datePicker;
		[SerializeField] private UiTimePicker m_timePicker;

		private Options m_options;

		public CEvent<DateTime> OnValueChanged = new();

		public void SetOptions(Options _options)
		{
			m_options = _options;
			InitByOptions();
		}

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

		private void InitByOptions()
		{
			if (m_options == null)
				m_options = new Options();

			if (!m_options.ShowDate && !m_options.ShowTime)
				throw new ArgumentException("Either time or date or both needs to be set");

			m_dateTimeDisplay.gameObject.SetActive(m_options.ShowCurrentDate);
			if (m_options.ShowCurrentDate)
			{
				m_dateTimeDisplay.SetOptions(new UiDateTimeDisplay.Options()
				{
					ShowDate = m_options.ShowDate,
					ShowTime = m_options.ShowTime,
					LongDateFormat = m_options.LongDateFormat,
					LongTimeFormat = m_options.LongTimeFormat,
				});
			}

			m_datePicker.gameObject.SetActive(m_options.ShowDate);
			m_timePicker.gameObject.SetActive(m_options.ShowTime);
			SelectedDateTime = m_options.StartDateTime;
		}

		private void OnDateTimeChanged(DateTime _value)
		{
			SelectedDateTime = _value;
			OnValueChanged.InvokeOnce(_value);
			m_options.OnDateTimeChanged?.Invoke(_value);
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
				OnValueChanged.InvokeOnce(value);
			}
		}
	}
}