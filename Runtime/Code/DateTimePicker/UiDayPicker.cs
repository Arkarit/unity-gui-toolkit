using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace GuiToolkit
{
	public class UiDayPicker : UiIncDecGridPicker
	{
		[SerializeField] private UiButton m_optionalNowButton;
		[SerializeField] private UiYearPicker m_yearPicker;
		[FormerlySerializedAs("m_monthPicker")] [SerializeField] private UiMonthPicker mUiMonthPicker;

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

			mUiMonthPicker.OnValueChanged.AddListener(OnChanged);
			m_yearPicker.OnValueChanged.AddListener(OnChanged);

			base.OnEnable();
		}

		protected override void OnDisable()
		{
			base.OnDisable();
			mUiMonthPicker.OnValueChanged.RemoveListener(OnChanged);
			m_yearPicker.OnValueChanged.RemoveListener(OnChanged);
		}

		private void OnChanged(string _, int __) => RebuildStrings();

		private void OnNowButton() => SetDay(DateTime.Now.Day-1);

		private void RebuildStrings()
		{
			m_strings.Clear();
			var year = m_yearPicker.Year;
			var month = mUiMonthPicker.Month + 1;
			var daysInMonth = DateTime.DaysInMonth(year, month);
			if (m_index >= daysInMonth)
				m_index = daysInMonth - 1;

			for (int i = 0; i < daysInMonth; i++)
				m_strings.Add((i+1).ToString());

			UpdateText();
		}

		private void SetDay(int _day)
		{
			m_index = _day;
			RebuildStrings();
		}
	}
}