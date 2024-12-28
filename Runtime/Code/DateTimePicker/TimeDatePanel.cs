using System;
using UnityEngine;

namespace GuiToolkit
{
	public class TimeDatePanel : TimeDatePanelBase
	{
		public enum Type
		{
			Hour,
			Minute,
			Second,
			Year,
			Month,
			Day,
		}

		[SerializeField] private Type m_type;

		protected override void OnCellClicked(UiGridPicker _gridPicker, int _x, int _y, UiGridPickerCell _cell)
		{
			base.OnCellClicked(_gridPicker, _x, _y, _cell);

Debug.Log($"---::: Cell clicked: {_x}, {_y} : {SelectedValue} {m_text.text}");
		}

		protected override string GetContentString( int val )
		{
			int year = m_type == Type.Year ? val : 2000;
			int month = m_type == Type.Month ? val : 1;
			int day = m_type == Type.Day ? val : 1;
			int hour = m_type == Type.Hour ? val : 0;
			int minute = m_type == Type.Minute ? val : 0;
			int second = m_type == Type.Second ? val : 0;

			DateTime time = new DateTime(year, month, day, hour, minute, second);

			switch (m_type)
			{
				case Type.Hour:
					return time.ToShortTimeString().Replace(":00", "");
				case Type.Minute:
					break;
				case Type.Second:
					break;
				case Type.Year:
					break;
				case Type.Month:
					break;
				case Type.Day:
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			return string.Empty;
		}
	}
}