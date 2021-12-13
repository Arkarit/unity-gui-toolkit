/// <summary>
/// Originally taken from "UnityCalender"
/// https://github.com/n-sundara-pandian/UnityCalender
/// MIT License
/// </summary>

using System;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace GuiToolkit
{
	public class DatePicker : MonoBehaviour
	{
		[FormerlySerializedAs("m_dayToggleTemplate")]
		[SerializeField] protected DayToggle m_dayTogglePrefab;
		[FormerlySerializedAs("m_dayNameLabelTemplate")]
		[SerializeField] protected Text m_dayNameLabelPrefab;
		[SerializeField] protected GridLayoutGroup m_dayContainer;
		[SerializeField] protected Text m_selectedDateText;
		[SerializeField] protected Text m_currentMonth;
		[SerializeField] protected Text m_currentYear;
		[SerializeField] protected string m_dateFormat = "dd-MM-yyyy";
		[SerializeField] protected string m_monthFormat = "MMMMM";
		[SerializeField] protected bool m_forwardPickOnly = false;
		[SerializeField] protected DayOfWeek m_startDayOfWeek;

		private DayToggle[] m_dayToggles = new DayToggle[7 * 6];
		private bool m_dayTogglesGenerated = false;
		private DateTime m_referenceDate = DateTime.Now.AddYears(-100);
		private DateTime m_displayDate = DateTime.Now.AddYears(-101);

		// Null so that it can be deselected(Yet to be implemented)
		private DateTime? m_SelectedDate;

		public DateTime? SelectedDate
		{
			get { return m_SelectedDate; }
			private set
			{
				m_SelectedDate = value;
				if (m_SelectedDate != null)
				{
					m_selectedDateText.text = ((DateTime)m_SelectedDate).ToString(m_dateFormat);
				}
				else
				{
					m_selectedDateText.text = string.Empty;
				}
			}
		}
		public DateTime ReferenceDateTime
		{
			get
			{
				return m_referenceDate;
			}
			set
			{
				m_referenceDate = DateTimeHelpers.GetYearMonthStart(value);
				m_currentYear.text = m_referenceDate.Year.ToString();
				m_currentMonth.text = m_referenceDate.ToString(m_monthFormat);
			}
		}

		void Start()
		{
			GenerateDaysNames();
			GenerateDaysToggles();
			// Just in case SetSelectedDate is called before the Start function is executed
			if (SelectedDate == null)
			{
				SetSelectedDate(DateTime.Today);
			}
			else
			{
				SwitchToSelectedDate();
			}
		}

		public string Truncate( string _value, int _maxLength )
		{
			if (string.IsNullOrEmpty(_value)) return _value;
			return _value.Length <= _maxLength ? _value : _value.Substring(0, _maxLength);
		}

		public void GenerateDaysNames()
		{
			int dayOfWeek = (int)m_startDayOfWeek;
			for (int d = 1; d <= 7; d++)
			{
				string day_name = Truncate(Enum.GetName(typeof(DayOfWeek), dayOfWeek), 3);
				var DayNameLabel = Instantiate(m_dayNameLabelPrefab);
				DayNameLabel.name = String.Format("Day Name Label ({0})", day_name);
				DayNameLabel.transform.SetParent(m_dayContainer.transform, false);
				DayNameLabel.GetComponentInChildren<Text>().text = day_name;
				dayOfWeek++;
				if (dayOfWeek >= 7)
				{
					dayOfWeek = 0;
				}
			}
		}

		public void GenerateDaysToggles()
		{
			for (int i = 0; i < m_dayToggles.Length; i++)
			{
				var DayToggle = Instantiate(m_dayTogglePrefab);
				DayToggle.transform.SetParent(m_dayContainer.transform, false);
				DayToggle.GetComponentInChildren<Text>().text = string.Empty;
				DayToggle.m_evtOnDateSelected.AddListener(OnDaySelected);
				m_dayToggles[i] = DayToggle;
			}
			m_dayTogglesGenerated = true;
		}

		private void DisplayMonthDays( bool _refresh = false )
		{
			if (!_refresh && m_displayDate.IsSameYearMonth(ReferenceDateTime))
			{
				return;
			}
			m_displayDate = ReferenceDateTime.DuplicateDate(ReferenceDateTime);

			int monthdays = ReferenceDateTime.DaysInMonth();

			DateTime day_datetime = m_displayDate.GetYearMonthStart();

			int dayOffset = (int)day_datetime.DayOfWeek - (int)m_startDayOfWeek;
			if ((int)day_datetime.DayOfWeek < (int)m_startDayOfWeek)
			{
				dayOffset = (7 + dayOffset);
			}
			day_datetime = day_datetime.AddDays(-dayOffset);
			//DayContainer.GetComponent<ToggleGroup>().allowSwitchOff = true;
			for (int i = 0; i < m_dayToggles.Length; i++)
			{
				SetDayToggle(m_dayToggles[i], day_datetime);
				day_datetime = day_datetime.AddDays(1);
			}
			//DayContainer.GetComponent<ToggleGroup>().allowSwitchOff = false;
		}

		void SetDayToggle( DayToggle dayToggle, DateTime toggleDate )
		{
			dayToggle.interactable = ((!m_forwardPickOnly || (m_forwardPickOnly && !toggleDate.IsPast())) && toggleDate.IsSameYearMonth(m_displayDate));
			dayToggle.name = String.Format("Day Toggle ({0} {1})", toggleDate.ToString("MMM"), toggleDate.Day);
			dayToggle.SetText(toggleDate.Day.ToString());
			dayToggle.dateTime = toggleDate;

			dayToggle.isOn = (SelectedDate != null) && ((DateTime)SelectedDate).IsSameDate(toggleDate);
		}

		public void YearInc_onClick()
		{
			ReferenceDateTime = ReferenceDateTime.AddYears(1);
			DisplayMonthDays(false);
		}

		public void YearDec_onClick()
		{
			if (!m_forwardPickOnly || (!ReferenceDateTime.IsCurrentYear() && !ReferenceDateTime.IsPastYearMonth()))
			{
				ReferenceDateTime = ReferenceDateTime.AddYears(-1);
				DisplayMonthDays(false);
			}
		}

		public void MonthInc_onClick()
		{
			ReferenceDateTime = ReferenceDateTime.AddMonths(1);
			DisplayMonthDays(false);
		}

		public void MonthDec_onClick()
		{
			if (!m_forwardPickOnly || (!ReferenceDateTime.IsCurrentYearMonth() && !ReferenceDateTime.IsPastYearMonth()))
			{
				ReferenceDateTime = ReferenceDateTime.AddMonths(-1);
				DisplayMonthDays(false);
			}
		}

		public void SetSelectedDate( DateTime date )
		{
			SelectedDate = date;
			SwitchToSelectedDate();
		}

		void OnDaySelected( DateTime? date )
		{
			SetSelectedDate((DateTime)date);
		}

		public void SwitchToSelectedDate()
		{
			if (SelectedDate != null)
			{
				var sd = (DateTime)SelectedDate;
				if (!sd.IsSameYearMonth(m_displayDate))
				{
					ReferenceDateTime = (DateTime)SelectedDate;
					if (m_dayTogglesGenerated)
					{
						DisplayMonthDays(false);
					}
				}
			}
		}

		public void Today_onClick()
		{
			ReferenceDateTime = DateTime.Today;
			DisplayMonthDays(false);
		}
	}
}