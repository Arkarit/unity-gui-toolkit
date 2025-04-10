using System;
using UnityEngine;

namespace GuiToolkit
{
	public class MonthPicker : UiIncDecGridPicker
	{
		private static readonly string[] Months = new string[]
		{
			"January",
			"February",
			"March",
			"April",
			"May",
			"June",
			"July",
			"August",
			"September",
			"October",
			"November",
			"December"
		};

		[SerializeField] private string m_currentMonth = Months[0];

		protected override void OnEnable()
		{
			m_isLocalizable = true;
			m_strings.Clear();
			m_strings.AddRange(Months);
			SetMonthByName(m_currentMonth);

			base.OnEnable();
		}

		protected void SetMonthByName(string _monthName)
		{
			int index = m_strings.IndexOf(_monthName);
			if (index == -1)
			{
				Debug.LogWarning($"Month name '{_monthName}' unknown, setting '{Months[0]}'");
				m_index = 0;
				return;
			}

			m_index = 0;
			UpdateText();
		}
	}
}
