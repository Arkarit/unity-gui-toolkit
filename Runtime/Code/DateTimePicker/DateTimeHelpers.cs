using UnityEngine;

namespace GuiToolkit
{
	using System;

	public static class DateTimeHelpers
	{
		public enum EDateTimeType
		{
			Hour,
			Minute,
			Second,
			Year,
			Month,
			Day,
		}

		public static string GetDateTimePartAsString( int _val, EDateTimeType _type )
		{
			int year = _type == EDateTimeType.Year ? _val : 2000;
			int month = _type == EDateTimeType.Month ? _val : 1;
			int day = _type == EDateTimeType.Day ? _val : 1;
			int hour = _type == EDateTimeType.Hour ? _val : 0;
			int minute = _type == EDateTimeType.Minute ? _val : 0;
			int second = _type == EDateTimeType.Second ? _val : 0;

			DateTime time;
			try
			{
				time = new DateTime(year, month, day, hour, minute, second);
			}
			catch (Exception e)
			{
				Debug.LogError($"Exception in time conversion:{e.Message}\nValues: {year}:{month}:{day}:{hour}:{minute}:{second}");
				return string.Empty;
			}

			Debug.Log($"---::: {time.ToShortTimeString()}\n{time.ToLongTimeString()}");

			switch (_type)
			{
				case EDateTimeType.Hour:
					return time.ToShortTimeString().Replace(":00", "");
				case EDateTimeType.Minute:
					{
						var spl = time.ToShortTimeString().Split(new char[] { ':', ' ' });
						return spl[1];
					}
				case EDateTimeType.Second:
					{
						var spl = time.ToLongTimeString().Split(new char[] { ':', ' ' });
						return spl[2];
					}
				case EDateTimeType.Year:
					break;
				case EDateTimeType.Month:
					break;
				case EDateTimeType.Day:
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			return string.Empty;
		}

		public static DateTime DuplicateDate( this DateTime value, DateTime from )
		{
			return new DateTime(from.Year, from.Month, from.Day);
		}

		public static DateTime GetYearMonthStart( this DateTime value )
		{
			return GetYearMonthStart(value.Year, value.Month);
		}
		public static DateTime GetYearMonthStart( int Year, int Month )
		{
			return new DateTime(Year, Month, 1);
		}
		public static bool IsCurrentYear( this DateTime value )
		{
			return (value.Year == DateTime.Today.Year);
		}
		public static bool IsCurrentYearMonth( this DateTime value )
		{
			return (IsCurrentYear(value) && value.Month == DateTime.Today.Month);
		}

		public static bool IsPastYearMonth( this DateTime value )
		{
			return (value.Year < DateTime.Today.Year)
				|| (value.Year == DateTime.Today.Year && value.Month < DateTime.Today.Month);
		}

		public static bool IsPast( this DateTime value )
		{
			return (value.IsPastYearMonth()
					|| (value.Year == DateTime.Today.Year && value.Month == DateTime.Today.Month
						&& value.Day < DateTime.Today.Day));
		}

		public static bool IsFutureYearMonth( this DateTime value )
		{
			return (value.Year > DateTime.Today.Year)
				|| (value.Year == DateTime.Today.Year && value.Month > DateTime.Today.Month);
		}

		public static int DaysInMonth( this DateTime value )
		{
			return DateTime.DaysInMonth(value.Year, value.Month);
		}

		public static bool IsSameYearMonth( this DateTime value, int Year, int Month )
		{
			return value.IsSameYearMonth(new DateTime(Year, Month, 1));
		}
		public static bool IsSameYearMonth( this DateTime value, DateTime compareTo )
		{
			return value.Year == compareTo.Year && value.Month == compareTo.Month;
		}
		public static bool IsSameDate( this DateTime value, DateTime compareTo )
		{
			return value.Day == compareTo.Day && value.IsSameYearMonth(compareTo);
		}
	}
}