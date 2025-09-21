using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace GuiToolkit
{
	public class UiDayPicker : UiIncDecGridPicker
	{
		[SerializeField] private UiButton m_optionalNowButton;
		[SerializeField] private UiYearPicker m_yearPicker;
		[SerializeField] private UiMonthPicker m_monthPicker;

		// 1-based, depending on current year/month
		public int Day
		{
			get => m_index + 1;
			set => SetIndex(value - 1);
		}

		public void ValidateDaysInMonth()
		{
			var year = m_yearPicker.Year;
			var month = m_monthPicker.Month;
			var daysInMonth = DateTime.DaysInMonth(year, month);
			if (m_index >= daysInMonth)
				m_index = daysInMonth - 1;

			if (m_strings.Count == daysInMonth)
				return;

			m_strings.Clear();

			for (int i = 0; i < daysInMonth; i++)
				m_strings.Add((i+1).ToString());

			UpdateText();
		}

		protected override void Awake()
		{
			AddOnEnableButtonListeners
			(
				(m_optionalNowButton, OnNowButton)
			);
			base.Awake();
		}

		protected override void OnEnable()
		{
			m_isLocalizable = false;
			ValidateDaysInMonth();

			m_monthPicker.OnValueChanged.AddListener(OnChanged);
			m_yearPicker.OnValueChanged.AddListener(OnChanged);

			base.OnEnable();
		}

		protected override void OnDisable()
		{
			base.OnDisable();
			m_monthPicker.OnValueChanged.RemoveListener(OnChanged);
			m_yearPicker.OnValueChanged.RemoveListener(OnChanged);
		}

		private void OnChanged(string _, int __) => ValidateDaysInMonth();

		private void OnNowButton() => SetIndex(DateTime.Now.Day-1);

		private void SetIndex(int _day)
		{
			var year = m_yearPicker.Year;
			var month = m_monthPicker.Month;
			var daysInMonth = DateTime.DaysInMonth(year, month);
			if (_day < 0 || _day >= daysInMonth)
			{
				UiLog.LogError($"day '{_day + 1}' is out of range '1' - '{daysInMonth}'");
				return;
			}

			m_index = _day;
			ValidateDaysInMonth();
			UpdateText();
		}
	}
}