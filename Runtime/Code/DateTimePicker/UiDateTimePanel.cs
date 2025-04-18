using System;
using UnityEngine;
using UnityEngine.UI;

namespace GuiToolkit
{
	public class UiDateTimePanel : UiPanel
	{
		public class Options
		{
		}

		[SerializeField] private UiButton m_nowButton;
		[SerializeField] private UiDatePicker m_datePicker;
		[SerializeField] private UiTimePicker m_timePicker;
		[SerializeField] private Button m_clickCatcher;

		private Options m_options;

		public CEvent<DateTime> OnValueChanged = new();

		public void DateTimePanel(Options _options)
		{
			m_options = _options;
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

		public override void OnBeginShow()
		{
			base.OnBeginShow();
			InitByOptions();
		}

		private void InitByOptions()
		{
			if (m_options == null)
				m_options = new Options();

		}

		private void OnDateTimeChanged(DateTime _value)
		{
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