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

		// 1-based: 1..12
		public int Month
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

		private void OnNowButton() => SetIndex(DateTime.Now.Month-1);

		protected override void OnEnable()
		{
			m_isLocalizable = true;
			m_strings.Clear();
			m_strings.AddRange(Months);
			SetMonthByName(m_currentMonth);

			base.OnEnable();
		}

		// Zero based
		protected void SetIndex(int _index)
		{
			if (_index < 0 || _index > 11)
			{
				Debug.LogError($"Month '{_index + 1}' is outside of range '1' to '12'");
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
