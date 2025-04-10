using System;
using UnityEngine;
using UnityEngine.Events;

namespace GuiToolkit
{
	public class YearPicker : UiIncDecGridPicker
	{
		[SerializeField] private int m_startYear = 1950;
		[SerializeField] private int m_endYear = 2079;
		[SerializeField] private int m_currentYear = -1;
		[SerializeField] private UiButton m_optionalNowButton;

		protected override void Awake()
		{
			AddOnEnableButtonListeners
			(
				(m_optionalNowButton, OnNowButton)
			);
			base.Awake();
		}

		protected void OnNowButton() => SetYear(DateTime.Today.Year);

		protected void SetYear(int _year)
		{
			var index = m_strings.IndexOf(_year.ToString());
			if (index == -1)
			{
				Debug.LogError($"Year '{_year}' is outside of range '{m_startYear}' to '{m_endYear}'");
				return;
			}

			m_index = index;
			UpdateText();
		}

		protected override void OnEnable()
		{
			m_isLocalizable = false;
			m_strings.Clear();

			int endIdx = m_endYear - m_startYear;
			for (int i = 0; i <= endIdx; i++)
			{
				int year = i + m_startYear;
				if (year == DateTime.Today.Year)
					m_index = i;

				m_strings.Add(year.ToString());
			}

			base.OnEnable();
		}
	}
}
