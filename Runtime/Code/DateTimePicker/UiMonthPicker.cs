using System;
using UnityEngine;

namespace GuiToolkit
{
	public class UiMonthPicker : UiIncDecGridPicker
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

		[SerializeField] private UiButton m_optionalNowButton;

		// Zero based
		public int Month => m_index;

		protected override void Awake()
		{
			AddOnEnableButtonListeners
			(
				(m_optionalNowButton, OnNowButton)
			);
			base.Awake();
		}

		private void OnNowButton() => SetMonth(DateTime.Now.Month-1);

		protected override void OnEnable()
		{
			m_isLocalizable = true;
			m_strings.Clear();
			m_strings.AddRange(Months);
			SetMonthByName(m_currentMonth);

			base.OnEnable();
		}

		// Zero based
		protected void SetMonth(int _index)
		{
			if (_index < 0 || _index > 11)
			{
				Debug.LogError($"Month '{_index}' is outside of range '0' to '11'");
				_index = 0;
			}

			m_index = _index;
			UpdateText();
		}

		protected void SetMonthByName(string _monthName)
		{
			int index = m_strings.IndexOf(_monthName);
			if (index == -1)
			{
				Debug.LogWarning($"Month name '{_monthName}' unknown, setting '{Months[0]}'");
				index = 0;
			}

			m_index = index;
			UpdateText();
		}
	}
}
