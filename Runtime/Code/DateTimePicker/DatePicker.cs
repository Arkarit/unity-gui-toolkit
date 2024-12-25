using UnityEngine;
using UnityEngine.UI;
using System;
using RSToolkit.Helpers;
using UnityEngine.Serialization;

namespace GuiToolkit
{
	public class DatePicker : MonoBehaviour
	{

		[FormerlySerializedAs("DateFormat")] [SerializeField] private string m_dateFormat = "dd-MM-yyyy";
		[FormerlySerializedAs("MonthFormat")] [SerializeField] private string m_monthFormat = "MMMMM";
		[FormerlySerializedAs("DayToggleTemplate")] [SerializeField] private DayToggle m_dayToggleTemplate;
		[FormerlySerializedAs("DayNameLabelTemplate")] [SerializeField] private Text m_dayNameLabelTemplate;
		[FormerlySerializedAs("DayContainer")] [SerializeField] private GridLayoutGroup m_dayContainer;
		[FormerlySerializedAs("SelectedDateText")] [SerializeField] private Text m_selectedDateText;
		[FormerlySerializedAs("CurrentMonth")] [SerializeField] private Text m_currentMonth;
		[FormerlySerializedAs("CurrentYear")] [SerializeField] private Text m_currentYear;
		[FormerlySerializedAs("ForwardPickOnly")] [SerializeField] private bool m_forwardPickOnly = false;
		[FormerlySerializedAs("startDayOfWeek")] [SerializeField] private DayOfWeek m_startDayOfWeek;

		private DayToggle[] m_dayToggles = new DayToggle[7 * 6];
		private bool m_dayTogglesGenerated = false;
		DateTime m_referenceDate = DateTime.Now.AddYears(-100);
		DateTime m_displayDate = DateTime.Now.AddYears(-101);


		// Null so that it can be deselected(Yet to be implemented)
		private DateTime? m_SelectedDate;

		public DateTime? SelectedDate
		{
			get => m_SelectedDate;
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
			get => m_referenceDate;
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

		public string Truncate(string _value, int _maxLength)
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
				var DayNameLabel = Instantiate(m_dayNameLabelTemplate);
				DayNameLabel.name = String.Format("Day Name Label ({0})", day_name);
				DayNameLabel.transform.SetParent(m_dayContainer.transform);
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
				var DayToggle = Instantiate(m_dayToggleTemplate);
				DayToggle.transform.SetParent(m_dayContainer.transform);
				DayToggle.GetComponentInChildren<Text>().text = string.Empty;
				DayToggle.onDateSelected.AddListener(OnDaySelected);
				m_dayToggles[i] = DayToggle;
			}

			m_dayTogglesGenerated = true;
		}

		private void DisplayMonthDays(bool _refresh = false)
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

		void SetDayToggle(DayToggle _dayToggle, DateTime _toggleDate)
		{
			_dayToggle.interactable = ((!m_forwardPickOnly || (m_forwardPickOnly && !_toggleDate.IsPast())) &&
			                          _toggleDate.IsSameYearMonth(m_displayDate));
			_dayToggle.name = String.Format("Day Toggle ({0} {1})", _toggleDate.ToString("MMM"), _toggleDate.Day);
			_dayToggle.SetText(_toggleDate.Day.ToString());
			_dayToggle.DateTime = _toggleDate;

			_dayToggle.isOn = (SelectedDate != null) && ((DateTime)SelectedDate).IsSameDate(_toggleDate);
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

		public void SetSelectedDate(DateTime _date)
		{
			SelectedDate = _date;
			SwitchToSelectedDate();
		}

		void OnDaySelected(DateTime? _date)
		{
			SetSelectedDate((DateTime)_date);
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