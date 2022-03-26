using System;
using UnityEngine;

namespace GuiToolkit
{
	public class UiScrollSnapDateController : UiThing
	{
		[SerializeField] protected UiScrollRectText m_day;
		[SerializeField] protected UiScrollRectText m_month;
		[SerializeField] protected UiScrollRectText m_year;

		public DateTime Date
		{
			get => GetDate();
			set => SetDate(value);
		}

		protected override void Start()
		{
			base.Start();
			SetDate(DateTime.Now);
		}

		private DateTime GetDate()
		{
			throw new NotImplementedException();
		}

		private void SetDate( DateTime _date )
		{
//			m_day.SetNumbers(1, DateTime.DaysInMonth(_date.Year, _date.Month));
//			m_day.SetCurrentPage(_date.Day);
		}
	}
}