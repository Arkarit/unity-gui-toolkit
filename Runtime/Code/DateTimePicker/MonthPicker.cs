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
			m_index = 0;

			base.OnEnable();
		}
	}
}
