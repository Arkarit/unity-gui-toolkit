using System;
using UnityEngine;

namespace GuiToolkit
{
	public class YearPicker : UiIncDecGridPicker
	{
		[SerializeField] private int m_startYear = 1950;
		[SerializeField] private int m_endYear = 2070;
		[SerializeField] private int m_currentYear = -1;

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
