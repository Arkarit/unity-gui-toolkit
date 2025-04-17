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
			RebuildStrings();

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

		private void OnChanged(string _, int __) => RebuildStrings();

		private void OnNowButton() => SetIndex(DateTime.Now.Day-1);

		private void RebuildStrings()
		{
			m_strings.Clear();
			var year = m_yearPicker.Year;
			var month = m_monthPicker.Month;
			var daysInMonth = DateTime.DaysInMonth(year, month);
			if (m_index >= daysInMonth)
				m_index = daysInMonth - 1;

			for (int i = 0; i < daysInMonth; i++)
				m_strings.Add((i+1).ToString());

			UpdateText();
		}

		private void SetIndex(int _day)
		{
			var year = m_yearPicker.Year;
			var month = m_monthPicker.Month;
			var daysInMonth = DateTime.DaysInMonth(year, month);
			if (_day < 0 || _day >= daysInMonth)
			{
				Debug.LogError($"day '{_day + 1}' is out of range '1' - '{daysInMonth}'");
				return;
			}

			RebuildStrings();
			m_index = _day;
		}
	}
}